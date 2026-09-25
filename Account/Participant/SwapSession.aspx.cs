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
    public partial class SwapSession : SecurePage
    {
        private string EventSessionsXmlPath => Server.MapPath("~/App_Data/eventSessions.xml");
        private string MicrocoursesXmlPath => Server.MapPath("~/App_Data/microcourses.xml");
        private string EnrollmentsXmlPath => Server.MapPath("~/App_Data/enrollments.xml");

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                var userId = Session["UserId"] as string;
                var role = Session["Role"] as string;

                if (string.IsNullOrWhiteSpace(userId) ||
                    !string.Equals(role, "Participant", StringComparison.OrdinalIgnoreCase))
                {
                    Response.Redirect("~/Account/Login.aspx");
                    return;
                }

                var eventId = Request.QueryString["eventId"] ?? "";
                var sessionId = Request.QueryString["sessionId"] ?? "";
                var returnQs = Request.QueryString["return"] ?? "";

                CancelLink.HRef = ResolveUrl("~/Account/Participant/Home.aspx" + (string.IsNullOrEmpty(returnQs) ? "" : returnQs));

                if (string.IsNullOrWhiteSpace(eventId) ||
                    string.IsNullOrWhiteSpace(sessionId) ||
                    !File.Exists(EventSessionsXmlPath))
                {
                    EmptyPH.Visible = true;
                    return;
                }

                var sesDoc = new XmlDocument();
                sesDoc.Load(EventSessionsXmlPath);

                var original = sesDoc.SelectSingleNode($"/eventSessions/session[@eventId='{eventId}' and @id='{sessionId}']") as XmlElement;
                if (original == null)
                {
                    EmptyPH.Visible = true;
                    return;
                }

                if (!IsUserCurrentlyEnrolled(eventId, sessionId, userId))
                {
                    EmptyPH.Visible = true;
                    FormMessage.Text = "<span style='color:#b00020'>You are no longer enrolled in that session.</span>";
                    return;
                }

                var courseId = (original["courseId"]?.InnerText ?? "").Trim();
                var helper = (original["helper"]?.InnerText ?? "").Trim();
                var startIso = (original["start"]?.InnerText ?? "").Trim();
                var endIso = (original["end"]?.InnerText ?? "").Trim();

                if (!DateTime.TryParse(startIso, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var startUtc) ||
                    !DateTime.TryParse(endIso, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var endUtc))
                {
                    EmptyPH.Visible = true;
                    return;
                }

                var startLocal = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc).ToLocalTime();
                var endLocal = DateTime.SpecifyKind(endUtc, DateTimeKind.Utc).ToLocalTime();

                var titles = LoadCourseTitles();
                titles.TryGetValue(courseId, out var title);

                CourseTitle.Text = string.IsNullOrWhiteSpace(title) ? "(untitled)" : title;
                OriginalTime.Text = $"{startLocal:ddd, MMM d • h:mm tt} – {endLocal:h:mm tt}";

                var myIntervals = GetUserEnrolledIntervals(eventId, userId, sessionId);

                var rows = new List<object>();

                foreach (XmlElement s in sesDoc.SelectNodes($"/eventSessions/session[@eventId='{eventId}']"))
                {
                    var cid = (s["courseId"]?.InnerText ?? "").Trim();
                    if (!string.Equals(cid, courseId, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var altSessionId = s.GetAttribute("id");
                    if (string.Equals(altSessionId, sessionId, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var sStartIso = (s["start"]?.InnerText ?? "").Trim();
                    var sEndIso = (s["end"]?.InnerText ?? "").Trim();

                    if (!DateTime.TryParse(sStartIso, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var sStartUtc) ||
                        !DateTime.TryParse(sEndIso, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var sEndUtc))
                        continue;

                    if (sEndUtc < DateTime.UtcNow)
                        continue;

                    var overlaps = myIntervals.Any(iv => sStartUtc < iv.EndUtc && iv.StartUtc < sEndUtc);
                    if (overlaps)
                        continue;

                    var cap = SafeParseInt(s["capacity"]?.InnerText, 0);
                    var remaining = Math.Max(0, cap - GetEnrolledCount(eventId, altSessionId));
                    if (remaining <= 0)
                        continue;

                    var sStartLocal = DateTime.SpecifyKind(sStartUtc, DateTimeKind.Utc).ToLocalTime();
                    var sEndLocal = DateTime.SpecifyKind(sEndUtc, DateTimeKind.Utc).ToLocalTime();
                    var helperName = (s["helper"]?.InnerText ?? helper).Trim();

                    rows.Add(new
                    {
                        title = CourseTitle.Text,
                        startLocal = sStartLocal,
                        endLocal = sEndLocal,
                        helper = helperName,
                        capacity = cap,
                        remaining = remaining,
                        sessionId = altSessionId
                    });
                }

                EmptyPH.Visible = rows.Count == 0;
                SwapRepeater.DataSource = rows
                    .OrderBy(r => (DateTime)r.GetType().GetProperty("startLocal").GetValue(r, null))
                    .ToList();
                SwapRepeater.DataBind();
            }
        }

        protected void SwapRepeater_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (!string.Equals(e.CommandName, "swap_to", StringComparison.OrdinalIgnoreCase))
                return;

            var userId = Session["UserId"] as string ?? "";
            var eventId = Request.QueryString["eventId"] ?? "";
            var oldSessionId = Request.QueryString["sessionId"] ?? "";
            var newSessionId = Convert.ToString(e.CommandArgument);
            var returnQs = Request.QueryString["return"] ?? "";

            try
            {
                if (TryAtomicSwap(eventId, oldSessionId, newSessionId, userId, out var message))
                {
                    var homeUrl = ResolveUrl("~/Account/Participant/Home.aspx" + (string.IsNullOrEmpty(returnQs) ? "" : returnQs));
                    Response.Redirect(homeUrl, endResponse: false);
                    HttpContext.Current.ApplicationInstance.CompleteRequest();
                    return;
                }

                FormMessage.Text = $"<span style='color:#b00020'>{Server.HtmlEncode(message)}</span>";
            }
            catch (Exception ex)
            {
                FormMessage.Text = $"<span style='color:#b00020'>Failed: {Server.HtmlEncode(ex.Message)}</span>";
            }
        }

        private bool TryAtomicSwap(string eventId, string oldSessionId, string newSessionId, string userId, out string message)
        {
            lock (CyberApp_FIA.EnrollmentSync.EnrollmentsLock)
            {
                if (string.IsNullOrWhiteSpace(eventId) ||
                    string.IsNullOrWhiteSpace(oldSessionId) ||
                    string.IsNullOrWhiteSpace(newSessionId) ||
                    string.IsNullOrWhiteSpace(userId))
                {
                    message = "Missing event, session, or user information.";
                    return false;
                }

                if (string.Equals(oldSessionId, newSessionId, StringComparison.OrdinalIgnoreCase))
                {
                    message = "Please choose a different session time.";
                    return false;
                }

                EnsureXmlDoc(EnrollmentsXmlPath, "enrollments");

                var sesDoc = new XmlDocument();
                sesDoc.Load(EventSessionsXmlPath);

                var enrDoc = new XmlDocument();
                enrDoc.Load(EnrollmentsXmlPath);

                var oldSession = sesDoc.SelectSingleNode($"/eventSessions/session[@eventId='{eventId}' and @id='{oldSessionId}']") as XmlElement;
                var newSession = sesDoc.SelectSingleNode($"/eventSessions/session[@eventId='{eventId}' and @id='{newSessionId}']") as XmlElement;

                if (oldSession == null || newSession == null)
                {
                    message = "One of those sessions is no longer available.";
                    return false;
                }

                var oldCourseId = (oldSession["courseId"]?.InnerText ?? "").Trim();
                var newCourseId = (newSession["courseId"]?.InnerText ?? "").Trim();

                if (!string.Equals(oldCourseId, newCourseId, StringComparison.OrdinalIgnoreCase))
                {
                    message = "You can only swap into another time for the same microcourse.";
                    return false;
                }

                var oldEnrollSession = EnsureSessionNode(enrDoc, eventId, oldSessionId);
                var newEnrollSession = EnsureSessionNode(enrDoc, eventId, newSessionId);

                var oldEnrollNode = oldEnrollSession.SelectSingleNode($"enrolled/user[@id='{userId}']") as XmlElement;
                if (oldEnrollNode == null)
                {
                    message = "You are no longer enrolled in the original session.";
                    return false;
                }

                if (newEnrollSession.SelectSingleNode($"enrolled/user[@id='{userId}']") != null)
                {
                    message = "You are already enrolled in that new session.";
                    return false;
                }

                if (newEnrollSession.SelectSingleNode($"waitlist/user[@id='{userId}']") != null)
                {
                    message = "You are already on the waitlist for that new session.";
                    return false;
                }

                if (!DateTime.TryParse((newSession["start"]?.InnerText ?? "").Trim(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var newStartUtc) ||
                    !DateTime.TryParse((newSession["end"]?.InnerText ?? "").Trim(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var newEndUtc))
                {
                    message = "The selected session time could not be read.";
                    return false;
                }

                if (newEndUtc < DateTime.UtcNow)
                {
                    message = "That session has already passed.";
                    return false;
                }

                var cap = SafeParseInt(newSession["capacity"]?.InnerText, 0);
                var currentEnrolled = newEnrollSession.SelectNodes("enrolled/user")?.Count ?? 0;
                var remaining = Math.Max(0, cap - currentEnrolled);
                if (remaining <= 0)
                {
                    message = "That session is full now. Your current seat was not changed.";
                    return false;
                }

                var myOtherIntervals = GetUserEnrolledIntervalsFromDocs(eventId, userId, oldSessionId, enrDoc, sesDoc);
                var overlaps = myOtherIntervals.Any(iv => newStartUtc < iv.EndUtc && iv.StartUtc < newEndUtc);
                if (overlaps)
                {
                    message = "That session now overlaps with your other enrolled sessions.";
                    return false;
                }

                var newUserNode = enrDoc.CreateElement("user");
                newUserNode.SetAttribute("id", userId);
                newUserNode.SetAttribute("ts", DateTime.UtcNow.ToString("o"));
                newEnrollSession.SelectSingleNode("enrolled").AppendChild(newUserNode);

                oldEnrollNode.ParentNode.RemoveChild(oldEnrollNode);

                PromoteFirstWaitlistedUser(oldEnrollSession, enrDoc);

                enrDoc.Save(EnrollmentsXmlPath);

                message = "Session swapped successfully.";
                return true;
            }
        }

        private void PromoteFirstWaitlistedUser(XmlElement sessionNode, XmlDocument doc)
        {
            var firstWait = sessionNode.SelectSingleNode("waitlist/user[1]") as XmlElement;
            if (firstWait == null)
                return;

            var moved = doc.CreateElement("user");
            moved.SetAttribute("id", firstWait.GetAttribute("id"));
            moved.SetAttribute("ts", DateTime.UtcNow.ToString("o"));
            sessionNode.SelectSingleNode("enrolled").AppendChild(moved);
            firstWait.ParentNode.RemoveChild(firstWait);
        }

        private bool IsUserCurrentlyEnrolled(string eventId, string sessionId, string userId)
        {
            EnsureXmlDoc(EnrollmentsXmlPath, "enrollments");
            var doc = new XmlDocument();
            doc.Load(EnrollmentsXmlPath);
            return doc.SelectSingleNode($"/enrollments/session[@eventId='{eventId}' and @id='{sessionId}']/enrolled/user[@id='{userId}']") != null;
        }

        private Dictionary<string, string> LoadCourseTitles()
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(MicrocoursesXmlPath))
                return map;

            var doc = new XmlDocument();
            doc.Load(MicrocoursesXmlPath);

            foreach (XmlElement c in doc.SelectNodes("/microcourses/course[@status='Published']"))
            {
                var id = c.GetAttribute("id");
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                map[id] = c["title"]?.InnerText ?? "(untitled)";
            }

            return map;
        }

        private sealed class Interval
        {
            public DateTime StartUtc { get; set; }
            public DateTime EndUtc { get; set; }
        }

        private List<Interval> GetUserEnrolledIntervals(string eventId, string userId, string excludeSessionId)
        {
            EnsureXmlDoc(EnrollmentsXmlPath, "enrollments");

            var enrDoc = new XmlDocument();
            enrDoc.Load(EnrollmentsXmlPath);

            var sesDoc = new XmlDocument();
            sesDoc.Load(EventSessionsXmlPath);

            return GetUserEnrolledIntervalsFromDocs(eventId, userId, excludeSessionId, enrDoc, sesDoc);
        }

        private List<Interval> GetUserEnrolledIntervalsFromDocs(string eventId, string userId, string excludeSessionId, XmlDocument enrDoc, XmlDocument sesDoc)
        {
            var list = new List<Interval>();

            foreach (XmlElement s in enrDoc.SelectNodes($"/enrollments/session[@eventId='{eventId}']"))
            {
                var sid = s.GetAttribute("id");

                if (string.Equals(sid, excludeSessionId, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (s.SelectSingleNode($"enrolled/user[@id='{userId}']") == null)
                    continue;

                var sn = sesDoc.SelectSingleNode($"/eventSessions/session[@eventId='{eventId}' and @id='{sid}']") as XmlElement;
                if (sn == null)
                    continue;

                if (!DateTime.TryParse((sn["start"]?.InnerText ?? "").Trim(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var startUtc) ||
                    !DateTime.TryParse((sn["end"]?.InnerText ?? "").Trim(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var endUtc))
                    continue;

                list.Add(new Interval { StartUtc = startUtc, EndUtc = endUtc });
            }

            return list;
        }

        private int GetEnrolledCount(string eventId, string sessionId)
        {
            EnsureXmlDoc(EnrollmentsXmlPath, "enrollments");

            var doc = new XmlDocument();
            doc.Load(EnrollmentsXmlPath);

            var node = doc.SelectNodes($"/enrollments/session[@eventId='{eventId}' and @id='{sessionId}']/enrolled/user");
            return node?.Count ?? 0;
        }

        private static int SafeParseInt(string s, int fallback)
        {
            if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
                return v;

            return fallback;
        }

        private void EnsureXmlDoc(string path, string rootName)
        {
            if (File.Exists(path))
                return;

            var doc = new XmlDocument();
            doc.AppendChild(doc.CreateElement(rootName));
            doc.Save(path);
        }

        private XmlElement EnsureSessionNode(XmlDocument doc, string eventId, string sessionId)
        {
            var ses = doc.SelectSingleNode($"/enrollments/session[@eventId='{eventId}' and @id='{sessionId}']") as XmlElement;
            if (ses != null)
                return ses;

            ses = doc.CreateElement("session");
            ses.SetAttribute("eventId", eventId);
            ses.SetAttribute("id", sessionId);

            var enrolled = doc.CreateElement("enrolled");
            var waitlist = doc.CreateElement("waitlist");

            ses.AppendChild(enrolled);
            ses.AppendChild(waitlist);
            doc.DocumentElement.AppendChild(ses);

            return ses;
        }
    }
}