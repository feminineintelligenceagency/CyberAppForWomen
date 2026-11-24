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

            // Role gate on EVERY request (including postbacks), based on the folder the page lives in.
            // Pages used to check the role only on first load, so a button click could skip the check.
            var requiredRole = RequiredRoleForPath(Request.AppRelativeCurrentExecutionFilePath);
            if (requiredRole != null &&
                !string.Equals((Session["Role"] as string ?? "").Trim(), requiredRole, StringComparison.OrdinalIgnoreCase))
            {
                Response.Redirect("~/Account/Login.aspx");
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
        /// Which role may open pages in each role folder. Pages outside these folders
        /// (for example Account/Security) only need a signed-in user.
        /// </summary>
        private static string RequiredRoleForPath(string appRelativePath)
        {
            var path = appRelativePath ?? "";
            if (path.StartsWith("~/Account/SuperAdmin/", StringComparison.OrdinalIgnoreCase)) return "SuperAdmin";
            if (path.StartsWith("~/Account/UniversityAdmin/", StringComparison.OrdinalIgnoreCase)) return "UniversityAdmin";
            if (path.StartsWith("~/Account/Helper/", StringComparison.OrdinalIgnoreCase)) return "Helper";
            if (path.StartsWith("~/Account/Participant/", StringComparison.OrdinalIgnoreCase)) return "Participant";
            return null;
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
                // If the account store can't be read for a moment, don't kick regular users out
                // (the next request checks again). Admin sessions fail closed instead: they can
                // change other people's data, so they must not continue on an unchecked session.
                var role = (Session["Role"] as string ?? "").Trim();
                var isAdmin = string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(role, "UniversityAdmin", StringComparison.OrdinalIgnoreCase);
                return !isAdmin;
            }
        }
    }
}