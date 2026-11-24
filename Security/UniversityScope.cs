using System;
using System.IO;
using System.Web.SessionState;
using System.Xml;

namespace CyberApp_FIA
{
    /// <summary>
    /// Makes sure a University Admin can only work with events that belong to their own university.
    /// The event pages take the event id from the URL, so without this check an admin from
    /// University A could open, edit or delete University B's event by changing the id.
    /// </summary>
    public static class UniversityScope
    {
        /// <summary>
        /// True only if the event exists and its university matches the signed-in admin's university
        /// (kept in Session["University"] since login). Anything unclear (no university on the
        /// session, missing file, unreadable file, unknown event) returns false.
        /// </summary>
        public static bool AdminOwnsEvent(HttpSessionState session, string eventsXmlPath, string eventId)
        {
            try
            {
                var adminUniversity = ((session == null ? null : session["University"]) as string ?? "").Trim();
                if (adminUniversity.Length == 0) return false;
                if (string.IsNullOrWhiteSpace(eventId)) return false;
                if (string.IsNullOrEmpty(eventsXmlPath) || !File.Exists(eventsXmlPath)) return false;

                var doc = new XmlDocument();
                doc.Load(eventsXmlPath);

                var ev = doc.SelectSingleNode("/events/event[@id=" + XPathSafe.Lit(eventId) + "]") as XmlElement;
                if (ev == null) return false;

                var eventUniversity = (ev["university"] == null ? "" : ev["university"].InnerText).Trim();
                return string.Equals(eventUniversity, adminUniversity, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}
