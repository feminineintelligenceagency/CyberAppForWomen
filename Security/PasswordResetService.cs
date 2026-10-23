using CyberApp_FIA.Account;    // LoginRateLimiting (dmundra)
using CyberApp_FIA.Services;   // passHasher (Alex's Argon2)
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Xml;

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

        //private static string ResetsPath => HttpContext.Current.Server.MapPath("~/App_Data/passwordResets.xml");
        //private static string UsersPath => HttpContext.Current.Server.MapPath("~/App_Data/users.xml");

        // ===================== Public methods =====================

        /// <summary>
        /// Finds a user by email (case-insensitive). Loops instead of building an XPath
        /// string from user input, so a typed email can't break the query.
        /// </summary>
        public static UserRepository.LoginRecord FindUserByEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) { return null; }
            return UserRepository.GetLoginByEmail(email);
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

            Database.InTransaction((conn, tx) =>
            {
                // Clean up tokens that expired more than a day ago
                Database.ExecuteNonQuery(conn, tx,
                    "DELETE FROM dbo.PasswordResetTokens WHERE ExpiresAtUtc < DATEADD(DAY, -1, SYSUTCDATETIME());");

                // Cancel this user's older unused links
                Database.ExecuteNonQuery(conn, tx, @"
                        UPDATE dbo.PasswordResetTokens SET UsedAtUtc = SYSUTCDATETIME()
                         WHERE UserId = @U AND UsedAtUtc IS NULL;",
                    UserIdParam(userId));

                Database.ExecuteNonQuery(conn, tx, @"
                        INSERT INTO dbo.PasswordResetTokens (TokenHash, UserId, CreatedAtUtc, ExpiresAtUtc)
                        VALUES (@H, @U, SYSUTCDATETIME(), DATEADD(SECOND, @Life, SYSUTCDATETIME()));",
                    Database.Param("@H", SqlDbType.Binary, 32, HashToken(token)),
                    UserIdParam(userId),
                    Database.Param("@Life", SqlDbType.Int, (int)TokenLifetime.TotalSeconds));
            });


            return token;
        }

        /// <summary>
        /// Returns the user id if the token is real, not expired, and not used. Otherwise null.
        /// </summary>
        public static string GetUserIdForToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;

            return Database.ExecuteScalar(@"
                SELECT UserId FROM dbo.PasswordResetTokens
                 WHERE TokenHash = @H AND UsedAtUtc IS NULL AND ExpiresAtUtc > SYSUTCDATETIME();",
                Database.Param("@H", SqlDbType.Binary, 32, HashToken(token))) as string;

        }

        /// <summary>
        /// Checks the token again, marks it used, and saves the new password.
        /// Also stamps passwordChangedUtc so SecurePage signs out every other session.
        /// Returns false if the token is invalid, expired, or already used.
        /// </summary>
        public static bool ResetPassword(string token, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(token)) return false;

            // Hash outside the transaction: Argon2 is deliberately slow, and holding row
            // locks while it runs would block other requests.
            var hashed = passHasher.HashNewPassword(newPassword);
            var tokenHash = HashToken(token);

            return Database.InTransaction((conn, tx) =>
            {
                // Mark the token used and get its user in ONE atomic statement, so two
                // simultaneous submits of the same link can't both succeed.
                var userId = Database.ExecuteScalar(conn, tx, @"
                    UPDATE dbo.PasswordResetTokens
                       SET UsedAtUtc = SYSUTCDATETIME()
                    OUTPUT inserted.UserId
                     WHERE TokenHash = @H AND UsedAtUtc IS NULL AND ExpiresAtUtc > SYSUTCDATETIME();",
                    Database.Param("@H", SqlDbType.Binary, 32, tokenHash)) as string;

                if (userId == null) return false;

                // New password (Argon2), unlock (dmundra's lockout), new password stamp.
                var rows = Database.ExecuteNonQuery(conn, tx, @"
                    UPDATE dbo.UserCredentials
                       SET PasswordHash = @Hash, PasswordSalt = @Salt, Algorithm = @Algo,
                           Argon2MemoryKiB = @M, Argon2Iterations = @T, Argon2Parallelism = @P,
                           PasswordUpdatedAtUtc = SYSUTCDATETIME(),
                           FailedLoginCount = 0, LockoutUntilUtc = NULL
                     WHERE UserId = @U;",
                    Database.Param("@Hash", SqlDbType.VarBinary, 64, hashed.Hash),
                    Database.Param("@Salt", SqlDbType.VarBinary, 64, hashed.Salt),
                    Database.Param("@Algo", SqlDbType.VarChar, 20, hashed.Algorithm),
                    Database.Param("@M", SqlDbType.Int, hashed.MemoryKiB),
                    Database.Param("@T", SqlDbType.Int, hashed.Iterations),
                    Database.Param("@P", SqlDbType.Int, hashed.Parallelism),
                    UserIdParam(userId));

                if (rows != 1)
                {
                    // Account no longer exists. The token stays used (harmless), nothing else changes.
                    return false;
                }

                // Any other outstanding links for this user stop working too.
                Database.ExecuteNonQuery(conn, tx, @"
                    UPDATE dbo.PasswordResetTokens SET UsedAtUtc = SYSUTCDATETIME()
                     WHERE UserId = @U AND UsedAtUtc IS NULL;",
                    UserIdParam(userId));

                return true;
            });

        }

        // ===================== Helpers =====================

        private static bool IsExpired(XmlElement reset, DateTime now)
        {
            if (!DateTime.TryParse(reset.GetAttribute("expiresUtc"), CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out var expires))
            {
                return true; // unreadable date = treat as expired
            }
            return now > expires.ToUniversalTime();
        }

        private static byte[] HashToken(string token)
        {
            using (var sha = SHA256.Create())
            {
                return sha.ComputeHash(Encoding.UTF8.GetBytes(token ?? ""));
            }
        }

        private static SqlParameter UserIdParam(string userId)
        {
            return Database.Param("@U", SqlDbType.Char, 32, (userId ?? "").Trim().ToLowerInvariant());
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