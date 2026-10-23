using System;
using System.IO;
using System.Web;
using System.Web.UI;
using System.Xml;
using CyberApp_FIA.Services;   // AuthSession (Epic #7)

namespace CyberApp_FIA
{
    /// <summary>
    /// Base class for every page that requires a logged-in user.
    /// - Checks the session on EVERY request (including postbacks).
    /// - Signs the user out if their password was changed after they logged in
    ///   (or if their account no longer exists).
    /// - Stops the browser from caching logged-in pages.
    /// - Adds a client-side timer that leaves the page when the session times out.
    /// </summary>
    public class SecurePage : System.Web.UI.Page
    {
        protected override void OnInit(EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            Response.Cache.SetExpires(DateTime.UtcNow.AddMinutes(-1));

            var userId = Session["UserId"] as string;
            if (string.IsNullOrWhiteSpace(userId))
            {
                Response.Redirect("~/Account/Login.aspx?expired=1");
                return;
            }

            // Password changed since this session logged in? End this session.
            if (!PasswordStampMatches(userId))
            {
                AuthSession.SignOut(Context);   // ends session + deletes session and AuthToken cookies
                Response.Redirect("~/Account/Login.aspx?expired=1");
                return;
            }

            var timeoutMs = Session.Timeout * 60 * 1000;
            var loginUrl = ResolveUrl("~/Account/Login.aspx?expired=1");

            var script =
                "(function(){" +
                "  var t;" +
                "  function reset(){ clearTimeout(t); t = setTimeout(function(){ window.location = '" + loginUrl + "'; }, " + timeoutMs + "); }" +
                "  reset();" +
                "  window.addEventListener('pageshow', function(e){ if (e.persisted) { window.location.reload(); } });" +
                "  if (window.Sys && Sys.WebForms && Sys.WebForms.PageRequestManager) {" +
                "    Sys.WebForms.PageRequestManager.getInstance().add_endRequest(reset);" +
                "  }" +
                "})();";

            ClientScript.RegisterStartupScript(typeof(SecurePage), "idleTimeout", script, true);

            base.OnInit(e);
        }

        /// <summary>
        /// Compares the user's passwordChangedUtc in users.xml with the value saved in the
        /// session at login. If they're different, the password was changed after this login.
        /// </summary>
        private bool PasswordStampMatches(string userId)
        {
            try
            {
                var cuurent = UserRepository.GetPasswordStamp(userId);
                if (cuurent == null) { return false; } // account no longer exists

                var atLogin = Session["PasswordStamp"] as string ?? "";
                return string.Equals(cuurent, atLogin, StringComparison.Ordinal);
            }
            catch
            {
                // If users.xml can't be read for a moment (e.g. it's being saved),
                // don't kick everyone out. The next request checks again.
                return true;
            }
        }
    }
}