using System;
using System.Web;
using CyberApp_FIA.Services;   

namespace CyberApp_FIA
{
    /// Application-wide events that run on every request, before any page loads.
    /// Epic #7:
    public class Global : HttpApplication
    {
        // IIS Express HTTPS port from CyberApp_FIA.csproj (<IISExpressSSLPort>).
        // Only used when testing on your own machine.
        private const int LocalHttpsPort = 44347;

        // =====================Enforce HTTPS =====================

        // Runs at the very start of every request (pages, images, files).
        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            // Was this request made over plain HTTP instead of HTTPS?
            if (!Request.IsSecureConnection)
            {
                // Copy the exact URL the user asked for, but switch it to HTTPS.
                var secureUrl = new UriBuilder(Request.Url)
                {
                    Scheme = Uri.UriSchemeHttps,                    // http:// -> https://
                    Port = Request.IsLocal ? LocalHttpsPort : -1    // local: 44347, server: default 443
                };

                // Send the browser to the HTTPS version (301 = moved permanently).
                Response.RedirectPermanent(secureUrl.ToString(), false);

                // Stop here so no page content is ever sent over the insecure connection.
                CompleteRequest();
                return;
            }

            // On the real server (not localhost), tell browsers to use only HTTPS
            // for this site for the next year, even if someone types http://.
            if (!Request.IsLocal)
            {
                Response.AppendHeader("Strict-Transport-Security", "max-age=31536000");
            }
        }

        // ==================== Verify login token =====================

        // Runs on every request, right after ASP.NET loads the user's session.
        // This is the earliest point where we can check session data.
        protected void Application_PostAcquireRequestState(object sender, EventArgs e)
        {
            // Some requests have no session (images), and visitors who aren't
            // logged in have nothing to verify, so skip those.
            var session = Context.Session;
            if (session == null || session["UserId"] == null) return;

            // Logged in, but the browser's AuthToken cookie doesn't match the one
            // saved at login? Then this session may have been hijacked or planted
            // by someone else, so end it. The page will see no UserId and send the
            // user back to the login page.
            if (!AuthSession.IsValid(Context))
            {
                AuthSession.SignOut(Context);
            }
        }
    }
}