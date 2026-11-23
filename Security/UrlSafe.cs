using System;
using System.Web;

namespace CyberApp_FIA
{
    /// <summary>
    /// Safe handling of links that come from stored data (room links, resource links).
    /// A value like  javascript:alert(1)  put into an href runs script when clicked, so a link
    /// is only ever used if it is a real http or https address.
    /// </summary>
    public static class UrlSafe
    {
        /// <summary>True if the text is an absolute http or https address.</summary>
        public static bool TryGetHttpUri(string value, out Uri uri)
        {
            uri = null;
            if (string.IsNullOrWhiteSpace(value)) return false;

            Uri parsed;
            if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out parsed)) return false;
            if (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps) return false;

            uri = parsed;
            return true;
        }

        /// <summary>
        /// For use inside an aspx attribute:  href='&lt;%# UrlSafe.Href(Eval("room")) %&gt;'
        /// Returns the address (HTML-attribute encoded) when it is a real http/https link, otherwise "#".
        /// </summary>
        public static string Href(object value)
        {
            Uri uri;
            if (!TryGetHttpUri(Convert.ToString(value), out uri)) return "#";
            return HttpUtility.HtmlAttributeEncode(uri.AbsoluteUri);
        }

        /// <summary>
        /// Checks a "room / link" field before it is saved. Plain text such as "MU 201" is fine.
        /// Anything containing a colon has to be a full http/https link, which blocks javascript:, data: and similar.
        /// Returns null when the value is acceptable, otherwise a message for the user.
        /// </summary>
        public static string RoomProblem(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            var text = value.Trim();
            if (text.Length > 300) return "Room / link is too long (300 characters maximum).";

            foreach (var ch in text)
            {
                if (char.IsControl(ch)) return "Room / link can't contain control characters.";
            }

            Uri uri;
            if (text.IndexOf(':') >= 0 && !TryGetHttpUri(text, out uri))
                return "If you enter a link, it must start with http:// or https://";

            return null;
        }
    }
}
