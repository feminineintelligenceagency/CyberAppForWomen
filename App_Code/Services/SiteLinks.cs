using System;
using System.Configuration;
using System.Diagnostics;

namespace CyberApp_FIA.Services
{
    /// <summary>
    /// Builds absolute links for emails (password reset, device alerts) from the configured
    /// site address (Notifications:BaseUrl), NOT from the incoming request.
    ///
    /// Why: the request's Host header is controlled by whoever sends the request. If an email
    /// link were built from it, an attacker could ask for a reset for someone else's account
    /// with a forged Host header and the victim would receive a real email whose link points
    /// at the attacker's server.
    /// </summary>
    public static class SiteLinks
    {
        /// <summary>
        /// Combines the configured base address with an app-relative path such as
        /// "~/Account/ResetPassword.aspx?token=abc". Returns false (and logs) if the base
        /// address is missing or unsafe, in which case the email must not be sent.
        /// </summary>
        public static bool TryBuild(string appRelativePath, out string absoluteUrl)
        {
            absoluteUrl = null;

            if (string.IsNullOrEmpty(appRelativePath) || !appRelativePath.StartsWith("~/", StringComparison.Ordinal))
                throw new ArgumentException("Path must start with ~/", nameof(appRelativePath));

            Uri baseUri;
            var baseUrl = ConfigurationManager.AppSettings["Notifications:BaseUrl"];

            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out baseUri) ||
                baseUri.Scheme != Uri.UriSchemeHttps ||
                !string.IsNullOrEmpty(baseUri.UserInfo) ||
                !string.IsNullOrEmpty(baseUri.Query) ||
                !string.IsNullOrEmpty(baseUri.Fragment))
            {
                Trace.TraceError("Email link not built: Notifications:BaseUrl is missing or not a plain https address.");
                return false;
            }

            baseUri = new Uri(baseUri.AbsoluteUri.TrimEnd('/') + "/");
            var destination = new Uri(baseUri, appRelativePath.Substring(2));

            // The link must stay inside the configured site.
            if (!baseUri.IsBaseOf(destination))
            {
                Trace.TraceError("Email link not built: path leaves the configured site.");
                return false;
            }

            absoluteUrl = destination.AbsoluteUri;
            return true;
        }
    }
}
