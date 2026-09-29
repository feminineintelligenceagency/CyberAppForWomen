using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml;

namespace CyberApp_FIA.Participant
{
    public partial class Replacements : SecurePage
    {
        // XML paths
        private string EventSessionsXmlPath => Server.MapPath("~/App_Data/eventSessions.xml");
        private string MicrocoursesXmlPath => Server.MapPath("~/App_Data/microcourses.xml");
        private string EnrollmentsXmlPath => Server.MapPath("~/App_Data/enrollments.xml");

        

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                var userId = Session["UserId"] as string;
                var role = Session["Role"] as string;
                if (string.IsNullOrWhiteSpace(userId) || !string.Equals(role, "Participant", StringComparison.OrdinalIgnoreCase))
                {
                    Response.Redirect("~/Account/Login.aspx");
                    return;
                }

                var eventId = Request.QueryString["eventId"] ?? "";
                var sessionId = Request.QueryString["sessionId"] ?? "";
                var returnQs = Request.QueryString["return"] ?? "";

                // Set cancel link (back to Home with preserved filters)
                var cancelHref = ResolveUrl("~/Account/Participant/Home.aspx" + (string.IsNullOrEmpty(returnQs) ? "" : returnQs));
                CancelLink.HRef = cancelHref;

                if (string.IsNullOrWhiteSpace(eventId) || string.IsNullOrWhiteSpace(sessionId) || !File.Exists(EventSessionsXmlPath))
                {
                    EmptyPH.Visible = true;
                    return;
                }

                var doc = new XmlDocument(); doc.Load(EventSessionsXmlPath);
                var original = (XmlElement)doc.SelectSingleNode($"/eventSessions/session[@eventId='{eventId}' and @id='{sessionId}']");
                if (original == null) { EmptyPH.Visible = true; return; }

                var courseId = original["courseId"]?.InnerText ?? "";
                var helper = original["helper"]?.InnerText ?? "";
                var startIso = original["start"]?.InnerText ?? "";
                var endIso = original["end"]?.InnerText ?? "";
                if (!DateTime.TryParse(startIso, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var startUtc)) { EmptyPH.Visible = true; return; }
                if (!DateTime.TryParse(endIso, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var endUtc)) { EmptyPH.Visible = true; return; }

                var startLocal = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc).ToLocalTime();
                var endLocal = DateTime.SpecifyKind(endUtc, DateTimeKind.Utc).ToLocalTime();

                // Titles
                var titles = LoadCourseTitles();
                titles.TryGetValue(courseId, out var title);
                CourseTitle.Text = string.IsNullOrWhiteSpace(title) ? "(untitled)" : title;
                OriginalTime.Text = $"{startLocal:ddd, MMM d • h:mm tt} – {endLocal:h:mm tt}";

                // Build user's current intervals to filter out overlaps
                var myIntervals = GetUserEnrolledIntervals(eventId, userId);

                // Find same-course sessions with no overlap
                var rows = new List<object>();
                foreach (XmlElement s in doc.SelectNodes($"/eventSessions/session[@eventId='{eventId}']"))
                {
                    var cid = s["courseId"]?.InnerText ?? "";
                    if (!string.Equals(cid, courseId, StringComparison.OrdinalIgnoreCase)) continue;

                    var sid = s.GetAttribute("id");
                    if (string.Equals(sid, sessionId, StringComparison.OrdinalIgnoreCase)) continue; // skip the conflicting one

                    var sStartIso = s["start"]?.InnerText ?? "";
                    var sEndIso = s["end"]?.InnerText ?? "";
                    if (!DateTime.TryParse(sStartIso, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var sStartUtc)) continue;
                    if (!DateTime.TryParse(sEndIso, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var sEndUtc)) continue;
                    if (sEndUtc < DateTime.UtcNow) continue; // in the past

                    // overlap test
                    var overlaps = myIntervals.Any(iv => sStartUtc < iv.EndUtc && iv.StartUtc < sEndUtc);
                    if (overlaps) continue;

                    // capacity check
                    var cap = SafeParseInt(s["capacity"]?.InnerText, 0);
                    var remaining = Math.Max(0, cap - GetEnrolledCount(eventId, sid));
                    if (remaining <= 0) continue;

                    var sStartLocal = DateTime.SpecifyKind(sStartUtc, DateTimeKind.Utc).ToLocalTime();
                    var sEndLocal = DateTime.SpecifyKind(sEndUtc, DateTimeKind.Utc).ToLocalTime();
                    var hlp = s["helper"]?.InnerText ?? helper;

                    rows.Add(new
                    {
                        title = CourseTitle.Text,
                        startLocal = sStartLocal,
                        endLocal = sEndLocal,
                        helper = hlp,
                        capacity = cap,
                        remaining = remaining,
                        sessionId = sid
                    });
                }

                EmptyPH.Visible = rows.Count == 0;
                AltRepeater.DataSource = rows.OrderBy(r => (DateTime)r.GetType().GetProperty("startLocal").GetValue(r, null)).ToList();
                AltRepeater.DataBind();
            }
        }

        protected void AltRepeater_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (!string.Equals(e.CommandName, "enroll_alt", StringComparison.OrdinalIgnoreCase)) return;

            var userId = Session["UserId"] as string ?? "";
            var eventId = Request.QueryString["eventId"] ?? "";
            var altSessionId = Convert.ToString(e.CommandArgument);
            var returnQs = Request.QueryString["return"] ?? "";

            try
            {
                string message;
                if (TryEnroll(eventId, altSessionId, userId, out message))
                {
                    // On success, send to the same success page used in Home
                    var url = ResolveUrl(
                        $"~/Account/Participant/EnrollSuccess.aspx?eventId={Uri.EscapeDataString(eventId)}&sessionId={Uri.EscapeDataString(altSessionId)}&return={HttpUtility.UrlEncode(returnQs)}");
                    Response.Redirect(url, endResponse: false);
                    HttpContext.Current.ApplicationInstance.CompleteRequest();
                    return;
                }
                else
                {
                    FormMessage.Text = $"<span style='color:#b00020'>{Server.HtmlEncode(message)}</span>";
                }
            }
            catch (Exception ex)
            {
                FormMessage.Text = $"<span style='color:#b00020'>Failed: {Server.HtmlEncode(ex.Message)}</span>";
            }
        }

        // ======== helpers (local copy) ========
        private Dictionary<string, string> LoadCourseTitles()
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(MicrocoursesXmlPath)) return map;

            var doc = new XmlDocument(); doc.Load(MicrocoursesXmlPath);
            foreach (XmlElement c in doc.SelectNodes("/microcourses/course[@status='Published']"))
            {
                var id = c.GetAttribute("id");
                if (string.IsNullOrWhiteSpace(id)) continue;
                var title = c["title"]?.InnerText ?? "(untitled)";
                map[id] = title;
            }
            return map;
        }

        private sealed class Interval
        {
            public DateTime StartUtc { get; set; }
            public DateTime EndUtc { get; set; }
        }

        private List<Interval> GetUserEnrolledIntervals(string eventId, string userId)
        {
            var list = new List<Interval>();
            if (string.IsNullOrWhiteSpace(userId)) return list;
            if (!File.Exists(EnrollmentsXmlPath) || !File.Exists(EventSessionsXmlPath)) return list;

            var enr = new XmlDocument(); enr.Load(EnrollmentsXmlPath);
            var ses = new XmlDocument(); ses.Load(EventSessionsXmlPath);

            foreach (XmlElement s in enr.SelectNodes($"/enrollments/session[@eventId='{eventId}']"))
            {
                var sid = s.GetAttribute("id");
                if (s.SelectSingleNode($"enrolled/user[@id='{userId}']") == null) continue;

                var sn = (XmlElement)ses.SelectSingleNode($"/eventSessions/session[@eventId='{eventId}' and @id='{sid}']");
                if (sn == null) continue;

                var startIso = sn["start"]?.InnerText ?? "";
                var endIso = sn["end"]?.InnerText ?? "";
                if (!DateTime.TryParse(startIso, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var startUtc)) continue;
                if (!DateTime.TryParse(endIso, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var endUtc)) continue;

                list.Add(new Interval { StartUtc = startUtc, EndUtc = endUtc });
            }

            return list;
        }

        private int GetEnrolledCount(string eventId, string sessionId)
        {
            EnsureXmlDoc(EnrollmentsXmlPath, "enrollments");
            var doc = new XmlDocument(); doc.Load(EnrollmentsXmlPath);
            var node = doc.SelectNodes($"/enrollments/session[@eventId='{eventId}' and @id='{sessionId}']/enrolled/user");
            return node?.Count ?? 0;
        }

        private static int SafeParseInt(string s, int fallback)
        {
            if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v)) return v;
            return fallback;
        }

        private void EnsureXmlDoc(string path, string rootName)
        {
            if (File.Exists(path)) return;
            var doc = new XmlDocument();
            doc.AppendChild(doc.CreateElement(rootName));
            doc.Save(path);
        }

        private bool TryEnroll(string eventId, string sessionId, string userId, out string message)
        {
            lock (EnrollmentSync.EnrollmentsLock)
            {
                // conflict guard
                if (SessionOverlapsUser(eventId, userId, sessionId))
                {
                    message = "This time now overlaps with your schedule.";
                    return false;
                }

                // capacity & add
                var cap = GetCapacity(eventId, sessionId);
                var doc = new XmlDocument(); doc.Load(EnrollmentsXmlPath);
                var ses = EnsureSessionNode(doc, eventId, sessionId);

                if (ses.SelectSingleNode($"enrolled/user[@id='{userId}']") != null)
                {
                    message = "You’re already enrolled in this session.";
                    return true;
                }
                if (ses.SelectSingleNode($"waitlist/user[@id='{userId}']") != null)
                {
                    message = "You’re already on the waitlist for this session.";
                    return false;
                }

                var currentEnrolled = ses.SelectNodes("enrolled/user")?.Count ?? 0;
                var remaining = Math.Max(0, cap - currentEnrolled);
                if (remaining <= 0)
                {
                    message = "This session is full.";
                    return false;
                }

                var u = doc.CreateElement("user");
                u.SetAttribute("id", userId);
                u.SetAttribute("ts", DateTime.UtcNow.ToString("o"));
                ses.SelectSingleNode("enrolled").AppendChild(u);

                doc.Save(EnrollmentsXmlPath);
                message = "Enrolled successfully!";
                return true;
            }
        }

        private bool SessionOverlapsUser(string eventId, string userId, string sessionId)
        {
            if (!File.Exists(EventSessionsXmlPath)) return false;

            var ses = new XmlDocument(); ses.Load(EventSessionsXmlPath);
            var s = (XmlElement)ses.SelectSingleNode($"/eventSessions/session[@eventId='{eventId}' and @id='{sessionId}']");
            if (s == null) return false;

            if (!DateTime.TryParse(s["start"]?.InnerText ?? "", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var startUtc)) return false;
            if (!DateTime.TryParse(s["end"]?.InnerText ?? "", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var endUtc)) return false;

            var mine = GetUserEnrolledIntervals(eventId, userId);
            return mine.Any(iv => startUtc < iv.EndUtc && iv.StartUtc < endUtc);
        }

        private int GetCapacity(string eventId, string sessionId)
        {
            if (!File.Exists(EventSessionsXmlPath)) return 0;
            var doc = new XmlDocument(); doc.Load(EventSessionsXmlPath);
            var s = (XmlElement)doc.SelectSingleNode($"/eventSessions/session[@eventId='{eventId}' and @id='{sessionId}']");
            if (s == null) return 0;
            return SafeParseInt(s["capacity"]?.InnerText, 0);
        }

        private XmlElement EnsureSessionNode(XmlDocument doc, string eventId, string sessionId)
        {
            var ses = (XmlElement)doc.SelectSingleNode($"/enrollments/session[@eventId='{eventId}' and @id='{sessionId}']");
            if (ses == null)
            {
                ses = doc.CreateElement("session");
                ses.SetAttribute("eventId", eventId);
                ses.SetAttribute("id", sessionId);
                var enrolled = doc.CreateElement("enrolled");
                var waitlist = doc.CreateElement("waitlist");
                ses.AppendChild(enrolled);
                ses.AppendChild(waitlist);
                doc.DocumentElement.AppendChild(ses);
            }
            return ses;
        }
    }
}
