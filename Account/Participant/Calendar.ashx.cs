using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Web;
using System.Web.SessionState;
using System.Xml;
using CyberApp_FIA.Services;

namespace CyberApp_FIA.Participant
{
    // If your .ashx directive says Class="CyberApp_FIA.Participant.CalendarIcs",
    // rename this class to CalendarIcs. Otherwise leave it as Calendar.
    // IRequiresSessionState: lets the handler read the login session (without it Session is null).
    public class Calendar : IHttpHandler, IRequiresSessionState
    {
        public bool IsReusable => true;

        private static string EventSessionsXmlPath(HttpContext ctx) => ctx.Server.MapPath("~/App_Data/eventSessions.xml");
        private static string MicrocoursesXmlPath(HttpContext ctx) => ctx.Server.MapPath("~/App_Data/microcourses.xml");

        public void ProcessRequest(HttpContext context)
        {
            // Only signed-in users may download a calendar file. Without this check anyone on the
            // internet could read session details (helper, room, times, course title).
            var signedInUser = context.Session?["UserId"] as string;
            if (string.IsNullOrWhiteSpace(signedInUser) || !AuthSession.IsValid(context))
            {
                context.Response.StatusCode = 401;
                context.Response.ContentType = "text/plain; charset=utf-8";
                context.Response.Write("Please sign in to download this calendar file.");
                return;
            }

            var eventId = context.Request.QueryString["eventId"] ?? string.Empty;
            var sessionId = context.Request.QueryString["sessionId"] ?? string.Empty;

            if (string.IsNullOrWhiteSpace(eventId) || string.IsNullOrWhiteSpace(sessionId))
            {
                context.Response.StatusCode = 400;
                context.Response.ContentType = "text/plain; charset=utf-8";
                context.Response.Write("Missing eventId or sessionId.");
                return;
            }

            // --- Load session details ---
            string courseId = null, helper = "", location = "Zoom";
            DateTime startUtc = DateTime.UtcNow, endUtc = DateTime.UtcNow.AddMinutes(30);

            var sessionsPath = EventSessionsXmlPath(context);
            if (File.Exists(sessionsPath))
            {
                var doc = new XmlDocument();
                doc.Load(sessionsPath);

                var s = (XmlElement)doc.SelectSingleNode($"/eventSessions/session[@eventId={XPathSafe.Lit(eventId)} and @id={XPathSafe.Lit(sessionId)}]");
                if (s != null)
                {
                    courseId = s["courseId"]?.InnerText ?? courseId;
                    helper = s["helper"]?.InnerText ?? helper;

                    var roomRaw = s["room"]?.InnerText;
                    location = string.IsNullOrWhiteSpace(roomRaw) ? location : roomRaw;

                    if (DateTime.TryParse(
                            s["start"]?.InnerText,
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                            out var st))
                    {
                        startUtc = st;
                    }
                    if (DateTime.TryParse(
                            s["end"]?.InnerText,
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                            out var en))
                    {
                        endUtc = en;
                    }
                }
            }

            // --- Resolve course title ---
            var title = "FIA Session";
            var coursesPath = MicrocoursesXmlPath(context);
            if (!string.IsNullOrWhiteSpace(courseId) && File.Exists(coursesPath))
            {
                var doc = new XmlDocument();
                doc.Load(coursesPath);
                var c = (XmlElement)doc.SelectSingleNode($"/microcourses/course[@id={XPathSafe.Lit(courseId)}]");
                if (c != null)
                {
                    var t = c["title"]?.InnerText;
                    if (!string.IsNullOrWhiteSpace(t)) title = t;
                }
            }

            // --- Build ICS (UTC) ---
            string Escape(string s) => (s ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace(",", "\\,")
                .Replace(";", "\\;")
                .Replace("\r\n", "\\n")
                .Replace("\n", "\\n");

            string DtUtc(DateTime dt) => dt.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'");
            var desc = string.IsNullOrWhiteSpace(helper) ? "FIA session" : $"Helper: {helper}";

            var sb = new StringBuilder();
            sb.AppendLine("BEGIN:VCALENDAR");
            sb.AppendLine("VERSION:2.0");
            sb.AppendLine("PRODID:-//FIA//Cyberfair//EN");
            sb.AppendLine("CALSCALE:GREGORIAN");
            sb.AppendLine("METHOD:PUBLISH");
            sb.AppendLine("BEGIN:VEVENT");
            sb.AppendLine($"UID:{Escape(sessionId)}@fia");
            sb.AppendLine($"DTSTAMP:{DtUtc(DateTime.UtcNow)}");
            sb.AppendLine($"DTSTART:{DtUtc(startUtc)}");
            sb.AppendLine($"DTEND:{DtUtc(endUtc)}");
            sb.AppendLine($"SUMMARY:{Escape(title)}");
            sb.AppendLine($"DESCRIPTION:{Escape(desc)}");
            sb.AppendLine($"LOCATION:{Escape(string.IsNullOrWhiteSpace(location) ? "Zoom" : location)}");
            sb.AppendLine("END:VEVENT");
            sb.AppendLine("END:VCALENDAR");

            // --- Download response ---
            var fileName = SafeFileName($"FIA-{title}-{startUtc:yyyyMMdd-HHmm}.ics");
            context.Response.Clear();
            context.Response.ContentType = "text/calendar; charset=utf-8";
            context.Response.AddHeader("Content-Disposition", $"attachment; filename=\"{fileName}\"");
            context.Response.Write(sb.ToString());
        }

        private static string SafeFileName(string s)
        {
            foreach (var ch in Path.GetInvalidFileNameChars())
                s = s.Replace(ch, '_');
            return s;
        }
    }
}


