using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Xml;
using CyberApp_FIA.Account;    // LoginRateLimiting (dmundra)
using CyberApp_FIA.Services;   // passHasher (Alex's Argon2)

namespace CyberApp_FIA
{
    /// <summary>
    /// Handles self-service password reset tokens.
    /// - Tokens are random, and only a SHA-256 hash of each token is stored (in App_Data/passwordResets.xml),
    ///   so someone reading the file can't use the links.
    /// - Each token expires after 30 minutes and works only once.
    /// - Asking for a new link cancels any older unused links for that user.
    /// </summary>
    public static class PasswordResetService
    {
        public static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(30);

        private static readonly object ResetsLock = new object();
        private static readonly object UsersLock = new object();

        private static string ResetsPath => HttpContext.Current.Server.MapPath("~/App_Data/passwordResets.xml");
        private static string UsersPath => HttpContext.Current.Server.MapPath("~/App_Data/users.xml");

        // ===================== Public methods =====================

        /// <summary>
        /// Finds a user by email (case-insensitive). Loops instead of building an XPath
        /// string from user input, so a typed email can't break the query.
        /// </summary>
        public static XmlElement FindUserByEmail(XmlDocument doc, string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;

            foreach (XmlElement user in doc.SelectNodes("/users/user"))
            {
                var stored = user["email"]?.InnerText?.Trim();
                if (string.Equals(stored, email.Trim(), StringComparison.OrdinalIgnoreCase))
                    return user;
            }
            return null;
        }

        /// <summary>
        /// Creates a new reset token for the user and returns the raw token (goes in the email link).
        /// </summary>
        public static string CreateToken(string userId)
        {
            var bytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            var token = HttpServerUtility.UrlTokenEncode(bytes); // URL-safe

            lock (ResetsLock)
            {
                var doc = LoadResets();
                var root = doc.DocumentElement;
                var now = DateTime.UtcNow;

                // Copy to a list first so we can safely remove nodes while looping
                var existing = new List<XmlElement>();
                foreach (XmlElement r in root.SelectNodes("reset")) existing.Add(r);

                foreach (var r in existing)
                {
                    if (IsExpired(r, now))
                    {
                        root.RemoveChild(r); // clean up old entries
                    }
                    else if (r.GetAttribute("userId") == userId)
                    {
                        r.SetAttribute("used", "true"); // cancel older unused links for this user
                    }
                }

                var el = doc.CreateElement("reset");
                el.SetAttribute("tokenHash", HashToken(token));
                el.SetAttribute("userId", userId);
                el.SetAttribute("createdUtc", now.ToString("o"));
                el.SetAttribute("expiresUtc", now.Add(TokenLifetime).ToString("o"));
                el.SetAttribute("used", "false");
                root.AppendChild(el);

                doc.Save(ResetsPath);
            }

            return token;
        }

        /// <summary>
        /// Returns the user id if the token is real, not expired, and not used. Otherwise null.
        /// </summary>
        public static string GetUserIdForToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;

            lock (ResetsLock)
            {
                var doc = LoadResets();
                var reset = FindValidReset(doc, token, DateTime.UtcNow);
                return reset?.GetAttribute("userId");
            }
        }

        /// <summary>
        /// Checks the token again, marks it used, and saves the new password.
        /// Also stamps passwordChangedUtc so SecurePage signs out every other session.
        /// Returns false if the token is invalid, expired, or already used.
        /// </summary>
        public static bool ResetPassword(string token, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(token)) return false;

            lock (ResetsLock)
            {
                var resets = LoadResets();
                var now = DateTime.UtcNow;
                var reset = FindValidReset(resets, token, now);
                if (reset == null) return false;

                var userId = reset.GetAttribute("userId");

                // Mark used FIRST, so the link can never work twice even if something fails below
                reset.SetAttribute("used", "true");
                reset.SetAttribute("usedUtc", now.ToString("o"));
                resets.Save(ResetsPath);

                lock (UsersLock)
                {
                    if (!File.Exists(UsersPath)) return false;

                    var users = new XmlDocument { PreserveWhitespace = true };
                    users.Load(UsersPath);

                    var user = FindUserById(users, userId);
                    if (user == null) return false;

                    // Save the new password with Alex's Argon2 hasher (same one Login checks)
                    passHasher.SetPassword(user, newPassword);

                    // If the account was locked from too many wrong passwords, unlock it (dmundra's lockout)
                    LoginRateLimiting.ClearLockout(users, user);

                    // Stamp the change so SecurePage signs out every other session
                    SetOrCreateChild(users, user, "passwordChangedUtc", now.ToString("o"));

                    users.Save(UsersPath);
                }
            }

            return true;
        }

        // ===================== Helpers =====================

        private static XmlDocument LoadResets()
        {
            var doc = new XmlDocument();
            if (File.Exists(ResetsPath))
            {
                doc.Load(ResetsPath);
            }
            else
            {
                doc.LoadXml("<?xml version='1.0' encoding='utf-8'?><passwordResets version='1'></passwordResets>");
            }
            return doc;
        }

        private static XmlElement FindValidReset(XmlDocument doc, string token, DateTime now)
        {
            var tokenHash = HashToken(token);

            foreach (XmlElement r in doc.DocumentElement.SelectNodes("reset"))
            {
                if (r.GetAttribute("tokenHash") != tokenHash) continue;
                if (r.GetAttribute("used") == "true") return null;
                if (IsExpired(r, now)) return null;
                return r;
            }
            return null;
        }

        private static bool IsExpired(XmlElement reset, DateTime now)
        {
            if (!DateTime.TryParse(reset.GetAttribute("expiresUtc"), CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out var expires))
            {
                return true; // unreadable date = treat as expired
            }
            return now > expires.ToUniversalTime();
        }

        private static string HashToken(string token)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(token));
                return BitConverter.ToString(bytes).Replace("-", "");
            }
        }

        private static XmlElement FindUserById(XmlDocument doc, string userId)
        {
            foreach (XmlElement user in doc.SelectNodes("/users/user"))
            {
                if (user.GetAttribute("id") == userId) return user;
            }
            return null;
        }

        private static void SetOrCreateChild(XmlDocument doc, XmlElement parent, string name, string value)
        {
            var child = parent[name];
            if (child == null)
            {
                child = doc.CreateElement(name);
                parent.AppendChild(child);
            }
            child.InnerText = value ?? "";
        }
    }
}