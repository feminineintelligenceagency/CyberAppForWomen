using System;
using System.Security.Cryptography;
using System.Web;

namespace CyberApp_FIA.Services
{
    public static class AuthSession
    {
        public const string CookieName = "AuthToken";
        private const string SessionKey = "AuthToken";
        private const string SessionCookieName = "ASP.NET_SessionId";

        public static void SignIn(HttpContext context)
        {
            var bytes = new byte[32];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(bytes);
            }
            var token = Convert.ToBase64String(bytes);

            context.Session[SessionKey] = token;

            context.Response.Cookies.Add(new HttpCookie(CookieName, token)
            {
                HttpOnly = true,
                Secure = true,
                Path = "/"
            });
        }

        public static bool IsValid(HttpContext context)
        {
            var expected = context.Session?[SessionKey] as string;
            var actual = context.Request.Cookies[CookieName]?.Value;

            if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(actual)) return false;
            return SecureEquals(expected, actual);
        }

        public static void SignOut(HttpContext context)
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

        private static bool SecureEquals(string a, string b)
        {
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}