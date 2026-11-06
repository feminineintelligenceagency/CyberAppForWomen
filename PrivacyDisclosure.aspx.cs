using System;
using System.Web.SessionState;
using System.Web.UI;

namespace CyberApp_FIA.Account
{
    /// <summary>
    /// Public privacy disclosure page:
    /// Anyone can open it 
    ///
    /// Opening it records the time in the session. The sign-up page checks this
    /// (HasViewed) and won't accept the consent checkbox until the disclosure was opened.
    /// The check is on the server, so it can't be skipped by editing the page in the browser.
    /// </summary>
    public partial class PrivacyDisclosure : Page
    {
        /// <summary>Session key holding the UTC time this disclosure was opened.</summary>
        public const string ViewedSessionKey = "PrivacyDisclosureViewedAtUtc";

        protected void Page_Load(object sender, EventArgs e)
        {
            // Record that the disclosure was opened in this browser session.
            Session[ViewedSessionKey] = DateTime.UtcNow;
        }

        /// <summary>True if the privacy disclosure was opened in this session.</summary>
        public static bool HasViewed(HttpSessionState session)
        {
            return session != null && session[ViewedSessionKey] is DateTime;
        }

        /// <summary>When the disclosure was opened in this session, or null.</summary>
        public static DateTime? ViewedAtUtc(HttpSessionState session)
        {
            return session?[ViewedSessionKey] as DateTime?;
        }
    }
}