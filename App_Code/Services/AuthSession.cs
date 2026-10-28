using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace CyberApp_FIA.Services
{
    public static class AuthSession
    {
        public const string CookieName = "AuthToken";
        private const string TokenSessionKey = "AuthToken";
        private const string SessionIdKey = "AuthSessionId";
        private const string SessionCookieName = "ASP.NET_SessionId";

        /// <summary>Creates a fresh, trackable authentication session after credentials are verified.</summary>
        public static void SignIn(HttpContext context)
        {
            if (context == null) throw new ArgumentNullException("context");
            var userId = context.Session?["UserId"] as string;
            if (string.IsNullOrWhiteSpace(userId))
                throw new InvalidOperationException("A user must be authenticated before an auth session is created.");

            var bytes = new byte[32];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(bytes);
            }
            var token = Convert.ToBase64String(bytes);
            var sessionId = Guid.NewGuid().ToString("N");
            var now = DateTime.UtcNow;

            // Replace an existing registry entry if this ASP.NET session is signing in again.
            var previousSessionId = context.Session[SessionIdKey] as string;
            if (!string.IsNullOrEmpty(previousSessionId))
                AuthSessionStore.RevokeSessionById(context, previousSessionId);

            // Store only a hash server-side; the raw token is kept in the user's protected cookie/session.
            AuthSessionStore.Register(
                context,
                sessionId,
                userId,
                HashToken(token),
                GetDeviceType(context.Request.UserAgent),
                now,
                context.Session.Timeout);

            context.Session[TokenSessionKey] = token;
            context.Session[SessionIdKey] = sessionId;

            // Flag sign-ins from an unrecognized browser/device; a monitoring failure must never block login.
            try
            {
                DeviceMonitor.CheckSignIn(context, userId, sessionId, GetDeviceType(context.Request.UserAgent), now);
            }
            catch { }

            context.Response.Cookies.Add(new HttpCookie(CookieName, token)
            {
                HttpOnly = true,
                Secure = true,
                Path = "/"
            });
        }

        /// <summary>Checks both the browser token and the server-side registry, updating last-active time.</summary>
        public static bool IsValid(HttpContext context)
        {
            if (context == null || context.Session == null) return false;

            var expected = context.Session[TokenSessionKey] as string;
            var actual = context.Request.Cookies[CookieName]?.Value;
            var sessionId = context.Session[SessionIdKey] as string;
            var userId = context.Session["UserId"] as string;

            if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(actual)
                || string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(userId))
                return false;

            // Removing a registry entry makes this check fail on that browser's next request.
            return SecureEquals(expected, actual)
                && AuthSessionStore.ValidateAndTouch(context, sessionId, userId, HashToken(actual), DateTime.UtcNow);
        }

        /// <summary>Returns the current user's unexpired sessions, including the current browser session.</summary>
        public static IReadOnlyList<AuthSessionInfo> GetActiveSessions(HttpContext context)
        {
            EnsureAuthenticated(context);
            var currentSessionId = context.Session[SessionIdKey] as string;
            var userId = context.Session["UserId"] as string;
            return AuthSessionStore.GetActiveSessions(context, userId, currentSessionId, DateTime.UtcNow);
        }

        /// <summary>Revokes one of the current user's sessions without allowing cross-account revocation.</summary>
        public static bool SignOutSession(HttpContext context, string sessionId)
        {
            EnsureAuthenticated(context);
            if (string.IsNullOrWhiteSpace(sessionId)) throw new ArgumentException("A session ID is required.", "sessionId");

            var userId = context.Session["UserId"] as string;
            var removed = AuthSessionStore.RevokeSession(context, userId, sessionId);
            if (removed && string.Equals(sessionId, context.Session[SessionIdKey] as string, StringComparison.Ordinal))
                ClearCurrentSession(context);

            return removed;
        }

        /// <summary>Revokes every session belonging to the current user except this browser session.</summary>
        public static int SignOutOtherSessions(HttpContext context)
        {
            EnsureAuthenticated(context);
            return AuthSessionStore.RevokeOtherSessions(
                context,
                context.Session["UserId"] as string,
                context.Session[SessionIdKey] as string,
                DateTime.UtcNow);
        }

        /// <summary>Revokes the current registry entry and expires both authentication cookies.</summary>
        public static void SignOut(HttpContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            if (context.Session != null)
            {
                var sessionId = context.Session[SessionIdKey] as string;
                var userId = context.Session["UserId"] as string;
                if (!string.IsNullOrEmpty(sessionId) && !string.IsNullOrEmpty(userId))
                    AuthSessionStore.RevokeSession(context, userId, sessionId);
            }

            ClearCurrentSession(context);
        }

        private static void ClearCurrentSession(HttpContext context)
        {
            if (context.Session != null)
            {
                context.Session.Clear();
                context.Session.Abandon();
            }
           
            ExpireCookie(context, SessionCookieName);
            ExpireCookie(context, CookieName);
        }

        private static void ExpireCookie(HttpContext context, string name)
        {
            context.Response.Cookies.Add(new HttpCookie(name, "")
            {
                Expires = DateTime.UtcNow.AddYears(-1),
                HttpOnly = true,
                Secure = true,
                Path = "/"
            });
        }

        private static void EnsureAuthenticated(HttpContext context)
        {
            if (!IsValid(context))
                throw new UnauthorizedAccessException("A valid authenticated session is required.");
        }

        private static string HashToken(string token)
        {
            using (var sha256 = SHA256.Create())
                return Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(token)));
        }

        // Keep the stored label coarse; detailed user-agent strings are neither needed nor retained.
        private static string GetDeviceType(string userAgent)
        {
            if (string.IsNullOrWhiteSpace(userAgent)) return "Unknown";
            var normalized = userAgent.ToLowerInvariant();

            if (normalized.Contains("ipad") || normalized.Contains("tablet"))
                return "Tablet";
            if (normalized.Contains("android") && !normalized.Contains("mobile"))
                return "Tablet";
            if (normalized.Contains("mobile") || normalized.Contains("iphone")
                || normalized.Contains("ipod") || normalized.Contains("android")
                || normalized.Contains("windows phone"))
                return "Mobile";
            return "Desktop";
        }

        private static bool SecureEquals(string a, string b)
        {
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}