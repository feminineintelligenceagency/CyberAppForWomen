using System;
using System.Web;

namespace CyberApp_FIA
{
    public static class SessionHelper
    {
        public static void SignOut(HttpContext ctx)
        {
            ctx.Session.Clear();
            ctx.Session.Abandon();

            ctx.Response.Cookies.Add(new HttpCookie("ASP.NET_SessionId", string.Empty)
            {
                Expires = DateTime.UtcNow.AddYears(-1),
                HttpOnly = true,
                Secure = ctx.Request.IsSecureConnection
            });
        }
    }
}