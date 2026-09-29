using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml;
using CyberApp_FIA.Services;


namespace CyberApp_FIA.Participant
{
    public partial class Home : SecurePage
    {
        // ---------- XML file paths ----------
        private string EventsXmlPath => Server.MapPath("~/App_Data/events.xml");
        private string MicrocoursesXmlPath => Server.MapPath("~/App_Data/microcourses.xml");
        private string EventCoursesXmlPath => Server.MapPath("~/App_Data/eventCourses.xml");
        private string EventSessionsXmlPath => Server.MapPath("~/App_Data/eventSessions.xml");
        private string EnrollmentsXmlPath => Server.MapPath("~/App_Data/enrollments.xml");
        private string CompletionsXmlPath => Server.MapPath("~/App_Data/completions.xml");

        private string MissingParticipantSessionsXmlPath => Server.MapPath("~/App_Data/missingParticipantSessions.xml");
        private static readonly object MissingLock = new object();

        // NEW: users file for one-time session deletion notices
        private string UsersXmlPath => Server.MapPath("~/App_Data/users.xml");

        // NEW: helper one-on-one conversations
        private string HelperMessagesXmlPath => Server.MapPath("~/App_Data/helperMessages.xml");

        private static readonly object CompletionsLock = new object();

        private sealed class FilterState
        {
            public DateTime? From { get; set; }
            public DateTime? To { get; set; }
            public HashSet<string> Tags { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public string Query { get; set; } = "";
        }

        private sealed class Interval
        {
            public DateTime StartUtc { get; set; }
            public DateTime EndUtc { get; set; }
            public string SessionId { get; set; }
            public string Title { get; set; }
        }

        private static bool Overlaps(Interval a, Interval b) => a.StartUtc < b.EndUtc && b.StartUtc < a.EndUtc;

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            SessionsRepeater.ItemCommand += SessionsRepeater_ItemCommand;
            MySessionsRepeater.ItemCommand += SessionsRepeater_ItemCommand;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                var role = (string)Session["Role"];
                if (!string.Equals(role, "Participant", StringComparison.OrdinalIgnoreCase))
                {
                    Response.Redirect("~/Account/Login.aspx");
                    return;
                }

                try
                {
                    var userId = CurrentUserId();
                    if (string.IsNullOrEmpty(userId))
                    {
                        Response.Redirect("~/Account/Login.aspx");
                        return;
                    }

                    var qs = new CyberApp_FIA.Services.QuizService(Server);
                    var rules = qs.CurrentRulesetVersion();
                    if (!qs.IsCompleted(userId, rules))
                    {
                        Response.Redirect("~/Account/Participant/Quiz.aspx");
                        return;
                    }
                }
                catch { }

                var eventId = (string)Session["EventId"];
                if (string.IsNullOrWhiteSpace(eventId))
                {
                    Response.Redirect("~/Account/Participant/SelectEvent.aspx");
                    return;
                }

                LoadEventHeader(eventId);

                // NEW: show one-time notice if an admin deleted a session this participant was in
                LoadSessionDeletionNotice(CurrentUserId());

                // NEW: show no-show attendance notice (if any unacknowledged)
                LoadNoShowNotice(CurrentUserId(), eventId);

                // NEW: load assigned Helper one-on-one support section (if any)
                LoadAssignedHelperSupport(CurrentUserId());

                EnsureXmlDoc(EnrollmentsXmlPath, "enrollments");
                EnsureXmlDoc(CompletionsXmlPath, "completions");

                LoadRecommendedMicrocourses(CurrentUserId(), eventId);

                var allCourseTags = LoadCourseTags();
                var allTagValues = allCourseTags.Values.SelectMany(v => v).Distinct(StringComparer.OrdinalIgnoreCase);
                LoadTagOptionsIntoUi(allTagValues);

                var filters = ReadFiltersFromQuery();

                if (filters.From.HasValue) FilterFrom.Text = filters.From.Value.ToString("yyyy-MM-ddTHH:mm");
                if (filters.To.HasValue) FilterTo.Text = filters.To.Value.ToString("yyyy-MM-ddTHH:mm");
                foreach (ListItem li in FilterTags.Items) li.Selected = filters.Tags.Contains(li.Value);
                FilterQuery.Text = filters.Query;

                BindSessions(eventId, filters);
                BindMySessions(eventId, CurrentUserId());
            }
        }

        private string CurrentUserId() => Session["UserId"] as string;

        private void LoadEventHeader(string eventId)
        {
            var uni = (string)Session["University"] ?? "";
            University.Text = string.IsNullOrWhiteSpace(uni) ? "(not set)" : uni;

            if (!File.Exists(EventsXmlPath)) { EventName.Text = "(unknown)"; return; }
            var doc = new XmlDocument(); doc.Load(EventsXmlPath);
            var ev = (XmlElement)doc.SelectSingleNode($"/events/event[@id='{eventId}']");
            EventName.Text = ev?["name"]?.InnerText ?? "(unknown)";
        }

        /// <summary>
        /// Looks up the participant's assigned Helper in users.xml and, if found,
        /// shows the one-on-one support section and binds any existing conversations.
        /// Also stores helper id/name/email in Session for other pages.
        /// </summary>
        private void LoadAssignedHelperSupport(string userId)
        {
            // Default hidden/empty.
            HelperSupportPanel.Visible = false;
            HelperName.Text = string.Empty;

            ConversationsEmpty.Visible = false;
            ConversationsRepeater.DataSource = null;
            ConversationsRepeater.DataBind();

            StartHelperMessageBtn.Visible = false;

            // Clear any previous helper session values
            Session["AssignedHelperId"] = null;
            Session["AssignedHelperName"] = null;
            Session["AssignedHelperEmail"] = null;

            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return;

                var usersPath = Server.MapPath("~/App_Data/users.xml");
                if (!File.Exists(usersPath))
                    return;

                var doc = new XmlDocument();
                doc.Load(usersPath);

                // Find the current participant by id.
                var me = (XmlElement)doc.SelectSingleNode($"/users/user[@id='{userId}']");
                if (me == null)
                    return;

                // NEW: read assignedHelperId from the participant node
                var helperId = me.GetAttribute("assignedHelperId");
                if (string.IsNullOrWhiteSpace(helperId))
                    return;

                // Find the Helper row using that id
                var helper = (XmlElement)doc.SelectSingleNode($"/users/user[@id='{helperId}' and @role='Helper']");
                if (helper == null)
                    return;

                var firstName = helper["firstName"]?.InnerText ?? string.Empty;
                if (string.IsNullOrWhiteSpace(firstName))
                {
                    // Fallbacks if firstName is missing for some reason.
                    firstName = helper["name"]?.InnerText
                                ?? helper["email"]?.InnerText
                                ?? string.Empty;
                }

                if (string.IsNullOrWhiteSpace(firstName))
                    return;

                // Helper email (for message XML, etc.)
                var helperEmail = helper["email"]?.InnerText ?? string.Empty;

                // Store in Session so HelperMessage / HelperConversation can use them
                Session["AssignedHelperId"] = helperId;
                Session["AssignedHelperName"] = firstName;
                Session["AssignedHelperEmail"] = helperEmail;

                // Update UI
                HelperName.Text = Server.HtmlEncode(firstName);
                StartHelperMessageBtn.Text = "Send message to " + firstName;
                StartHelperMessageBtn.Visible = true;

                HelperSupportPanel.Visible = true;

                // Bind any existing conversations for this participant + helper pair.
                BindHelperConversations(userId, helperId);
            }
            catch
            {
                // Soft-fail: helper support is non-critical.
                HelperSupportPanel.Visible = false;
                HelperName.Text = string.Empty;
                Session["AssignedHelperId"] = null;
                Session["AssignedHelperName"] = null;
                Session["AssignedHelperEmail"] = null;
            }
        }

        private void EnsureXmlDoc(string path, string rootName)
        {
            if (File.Exists(path)) return;
            var doc = new XmlDocument();
            doc.AppendChild(doc.CreateElement(rootName));
            doc.Save(path);
        }

        /// <summary>
        /// Shows an attendance/no-show banner if this participant has an unacknowledged
        /// missing record in missingParticipantSessions.xml. Includes a simple strike-style message.
        /// </summary>
        private void LoadNoShowNotice(string userId, string eventId)
        {
            NoShowNoticePH.Visible = false;
            NoShowNoticeText.Text = string.Empty;
            NoShowAckKey.Value = string.Empty;

            try
            {
                if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(eventId))
                    return;

                EnsureXmlDoc(MissingParticipantSessionsXmlPath, "missingParticipantSessions");

                if (!File.Exists(MissingParticipantSessionsXmlPath))
                    return;

                var missDoc = new XmlDocument();
                missDoc.Load(MissingParticipantSessionsXmlPath);

                // Find all missing entries for this participant (scoped to this event),
                // and locate the most recent one that is NOT acknowledged.
                XmlElement latestUnacked = null;
                DateTime latestTs = DateTime.MinValue;

                foreach (XmlElement m in missDoc.SelectNodes(
                    $"/missingParticipantSessions/missing[@participantId='{userId}' and @eventId='{eventId}']"))
                {
                    // Skip acknowledged items
                    var ack = (m.GetAttribute("ack") ?? "").Trim();
                    if (string.Equals(ack, "true", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var tsRaw = m.GetAttribute("tsUtc");
                    if (!DateTime.TryParse(tsRaw, CultureInfo.InvariantCulture,
                        DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var tsUtc))
                    {
                        // If ts missing or malformed, treat as older than any valid ts.
                        tsUtc = DateTime.MinValue;
                    }

                    if (latestUnacked == null || tsUtc > latestTs)
                    {
                        latestUnacked = m;
                        latestTs = tsUtc;
                    }
                }

                if (latestUnacked == null)
                    return;

                var sessionId = latestUnacked.GetAttribute("sessionId") ?? "";
                var helperId = latestUnacked.GetAttribute("helperId") ?? "";

                // Compute strike count (simple): total missing records for this participant in this event (all time).
                int strikeCount = 0;
                foreach (XmlElement m in missDoc.SelectNodes(
                    $"/missingParticipantSessions/missing[@participantId='{userId}' and @eventId='{eventId}']"))
                {
                    strikeCount++;
                }

                // Resolve session details for a nicer message: course title, helper name, start time.
                string courseTitle = "a session";
                string helperName = "your Helper";
                DateTime? startLocal = null;

                if (File.Exists(EventSessionsXmlPath))
                {
                    var sesDoc = new XmlDocument();
                    sesDoc.Load(EventSessionsXmlPath);

                    var ses = sesDoc.SelectSingleNode(
                        $"/eventSessions/session[@eventId='{eventId}' and @id='{sessionId}']") as XmlElement;

                    if (ses != null)
                    {
                        // helper string (may be name/email)
                        var h = (ses["helper"]?.InnerText ?? "").Trim();
                        if (!string.IsNullOrWhiteSpace(h))
                            helperName = h;

                        // start time
                        var startIso = (ses["start"]?.InnerText ?? "").Trim();
                        if (DateTime.TryParse(startIso, CultureInfo.InvariantCulture,
                            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var startUtc))
                        {
                            startLocal = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc).ToLocalTime();
                        }

                        // microcourse title via courseId
                        var courseId = (ses["courseId"]?.InnerText ?? "").Trim();
                        if (!string.IsNullOrWhiteSpace(courseId) && File.Exists(MicrocoursesXmlPath))
                        {
                            var microDoc = new XmlDocument();
                            microDoc.Load(MicrocoursesXmlPath);
                            var courseEl = microDoc.SelectSingleNode($"/microcourses/course[@id='{courseId}']") as XmlElement;
                            var t = (courseEl?["title"]?.InnerText ?? "").Trim();
                            if (!string.IsNullOrWhiteSpace(t))
                                courseTitle = t;
                        }
                    }
                }

                // Build policy-like “strike system” wording (gentle, not threatening).
                // You can tweak thresholds later; the message works even if counts go beyond 3.
                var strikeLabel = strikeCount == 1 ? "1 notice" : $"{strikeCount} notices";

                var when = startLocal.HasValue
                    ? startLocal.Value.ToString("ddd, MMM d • h:mm tt")
                    : "(time unavailable)";

                // Message: what happened + policy-like warning.
                var msg =
                    $"You were marked missing for “{courseTitle}” with {helperName} on {when}. " +
                    $"This is {strikeLabel} for this event. " +
                    "Repeated no-shows may temporarily restrict your ability to enroll. " +
                    "If you can’t attend a session, please unenroll early so someone else can take the spot.";

                NoShowNoticeText.Text = Server.HtmlEncode(msg);
                NoShowNoticePH.Visible = true;

                // Store a unique key so “Acknowledge” can mark THIS record as acknowledged.
                // (eventId|sessionId|tsUtc)
                var tsKey = latestUnacked.GetAttribute("tsUtc") ?? "";
                NoShowAckKey.Value = $"{eventId}|{sessionId}|{tsKey}";
            }
            catch
            {
                // Non-critical banner: fail silently.
                NoShowNoticePH.Visible = false;
                NoShowNoticeText.Text = string.Empty;
                NoShowAckKey.Value = string.Empty;
            }
        }

        /// <summary>
        /// Participant clicks Acknowledge: mark the latest shown missing record as ack=true so it stops appearing.
        /// </summary>
        protected void AckNoShowBtn_Click(object sender, EventArgs e)
        {
            try
            {
                var userId = CurrentUserId();
                if (string.IsNullOrWhiteSpace(userId))
                    return;

                var key = (NoShowAckKey.Value ?? "").Trim();
                if (string.IsNullOrWhiteSpace(key))
                    return;

                var parts = key.Split('|');
                if (parts.Length != 3)
                    return;

                var eventId = parts[0];
                var sessionId = parts[1];
                var tsUtc = parts[2];

                lock (MissingLock)
                {
                    EnsureXmlDoc(MissingParticipantSessionsXmlPath, "missingParticipantSessions");

                    var doc = new XmlDocument();
                    doc.Load(MissingParticipantSessionsXmlPath);

                    // Find the exact missing record by participantId + eventId + sessionId + tsUtc
                    var node = doc.SelectSingleNode(
                        $"/missingParticipantSessions/missing[@participantId='{userId}' and @eventId='{eventId}' and @sessionId='{sessionId}' and @tsUtc='{tsUtc}']"
                    ) as XmlElement;

                    if (node != null)
                    {
                        node.SetAttribute("ack", "true");
                        node.SetAttribute("ackTsUtc", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
                        doc.Save(MissingParticipantSessionsXmlPath);
                    }
                }
            }
            catch
            {
                // ignore
            }
            finally
            {
                // Hide banner immediately after acknowledging (and clear key)
                NoShowNoticePH.Visible = false;
                NoShowNoticeText.Text = string.Empty;
                NoShowAckKey.Value = string.Empty;
            }
        }

        /// <summary>
        /// If users.xml has a <sessionDeletionNotice> for this participant,
        /// show a top-of-page indicator and clear the notice so it only appears once.
        /// </summary>
        private void LoadSessionDeletionNotice(string userId)
        {
            SessionDeletionNoticePH.Visible = false;
            SessionDeletionNoticeText.Text = string.Empty;

            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return;

                if (!File.Exists(UsersXmlPath))
                    return;

                var doc = new XmlDocument();
                doc.Load(UsersXmlPath);

                var me = doc.SelectSingleNode($"/users/user[@id='{userId}']") as XmlElement;
                if (me == null)
                    return;

                var notice = me["sessionDeletionNotice"] as XmlElement;
                if (notice == null)
                    return;

                var microTitle = notice.GetAttribute("microcourseTitle");
                var startIso = notice.GetAttribute("startUtc");

                if (string.IsNullOrWhiteSpace(microTitle))
                    microTitle = "a session";

                string message;
                if (!string.IsNullOrWhiteSpace(startIso) &&
                    DateTime.TryParse(
                        startIso,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                        out var startUtc))
                {
                    var startLocal = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc).ToLocalTime();
                    var when = startLocal.ToString("ddd, MMM d • h:mm tt");
                    message =
                        $"A session for \"{microTitle}\" on {when} was deleted by your university admin. " +
                        "Please choose another time that works for you.";
                }
                else
                {
                    message =
                        $"A session for \"{microTitle}\" was deleted by your university admin. " +
                        "Please choose another time that works for you.";
                }

                SessionDeletionNoticeText.Text = Server.HtmlEncode(message);
                SessionDeletionNoticePH.Visible = true;

                // Clear the notice so it does not keep showing forever.
                me.RemoveChild(notice);
                doc.Save(UsersXmlPath);
            }
            catch
            {
                // If anything goes wrong, just fail silently; this banner is non-critical.
                SessionDeletionNoticePH.Visible = false;
                SessionDeletionNoticeText.Text = string.Empty;
            }
        }



        /// <summary>
        /// Reads helperMessages.xml and binds conversation cards for the participant/helper pair.
        /// Each card shows the topic and sent date and links to the conversation page.
        /// </summary>
        private void BindHelperConversations(string participantId, string helperId)
        {
            ConversationsEmpty.Visible = false;
            ConversationsRepeater.DataSource = null;
            ConversationsRepeater.DataBind();

            if (string.IsNullOrWhiteSpace(participantId) || string.IsNullOrWhiteSpace(helperId))
            {
                ConversationsEmpty.Visible = true;
                return;
            }

            if (!File.Exists(HelperMessagesXmlPath))
            {
                ConversationsEmpty.Visible = true;
                return;
            }

            var rows = new List<object>();
            var doc = new XmlDocument();
            doc.Load(HelperMessagesXmlPath);

            foreach (XmlElement conv in doc.SelectNodes($"/helperMessages/conversation[@participantId='{participantId}' and @helperId='{helperId}']"))
            {
                var topic = conv.GetAttribute("topic") ?? "";
                var createdOnStr = conv.GetAttribute("createdOn");
                var lastUpdatedStr = conv.GetAttribute("lastUpdated");

                DateTime createdOnUtc;
                DateTime lastUpdatedUtc;

                if (!DateTime.TryParse(createdOnStr, CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out createdOnUtc))
                {
                    DateTime.TryParse(createdOnStr, out createdOnUtc);
                }

                if (!DateTime.TryParse(lastUpdatedStr, CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out lastUpdatedUtc))
                {
                    lastUpdatedUtc = createdOnUtc;
                }

                var createdLocal = DateTime.SpecifyKind(createdOnUtc, DateTimeKind.Utc).ToLocalTime();
                var lastLocal = DateTime.SpecifyKind(lastUpdatedUtc, DateTimeKind.Utc).ToLocalTime();
                var id = conv.GetAttribute("id") ?? "";

                var url = ResolveUrl("~/Account/Participant/HelperConversation.aspx?id=" + HttpUtility.UrlEncode(id));

                rows.Add(new
                {
                    Topic = topic,
                    CreatedOnLocal = createdLocal,
                    LastUpdatedLocal = lastLocal,
                    ConversationUrl = url
                });
            }

            if (rows.Count == 0)
            {
                ConversationsEmpty.Visible = true;
                return;
            }

            ConversationsEmpty.Visible = false;
            ConversationsRepeater.DataSource = rows
                .OrderByDescending(r => (DateTime)r.GetType().GetProperty("LastUpdatedLocal").GetValue(r, null))
                .ToList();
            ConversationsRepeater.DataBind();
        }

        private Dictionary<string, HashSet<string>> LoadCourseTags()
        {
            var map = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(MicrocoursesXmlPath)) return map;

            var doc = new XmlDocument(); doc.Load(MicrocoursesXmlPath);
            foreach (XmlElement c in doc.SelectNodes("/microcourses/course[@status='Published']"))
            {
                var id = c.GetAttribute("id");
                if (string.IsNullOrWhiteSpace(id)) continue;

                var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (XmlElement t in c.SelectNodes("tags/tag"))
                {
                    var v = (t.InnerText ?? "").Trim();
                    if (!string.IsNullOrWhiteSpace(v)) set.Add(v);
                }
                map[id] = set;
            }
            return map;
        }

        private void LoadRecommendedMicrocourses(string userId, string eventId)
        {
            RecommendedCoursesPanel.Visible = false;
            RecommendedCoursesEmpty.Visible = false;
            RecommendedCoursesRepeater.DataSource = null;
            RecommendedCoursesRepeater.DataBind();

            if (string.IsNullOrWhiteSpace(userId))
                return;

            var quiz = new QuizService(Server);
            var latest = quiz.LoadLatestResult(userId);
            if (latest == null || latest.DomainScores == null || latest.DomainScores.Count == 0)
                return;

            var catalog = LoadRecommendationCatalog();
            if (catalog.Count == 0)
                return;

            var aliases = BuildRecommendationAliases(catalog);
            var completedSet = LoadUserCompletedSet(userId);
            var visibleCourseIds = LoadEventVisibleCourseIds(eventId);
            var upcomingCourseIds = LoadUpcomingCourseIdsForEvent(eventId);

            var rows = BuildRecommendationRows(
                latest,
                catalog,
                aliases,
                completedSet,
                visibleCourseIds,
                upcomingCourseIds);

            RecommendedCoursesPanel.Visible = true;
            RecommendedCoursesEmpty.Visible = rows.Count == 0;
            RecommendedCoursesRepeater.DataSource = rows;
            RecommendedCoursesRepeater.DataBind();
        }

        private Dictionary<string, RecommendationCourse> LoadRecommendationCatalog()
        {
            var catalog = new Dictionary<string, RecommendationCourse>(StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(MicrocoursesXmlPath))
                return catalog;

            var doc = new XmlDocument();
            doc.Load(MicrocoursesXmlPath);

            foreach (XmlElement c in doc.SelectNodes("/microcourses/course"))
            {
                var id = c.GetAttribute("id");
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                catalog[id] = new RecommendationCourse
                {
                    Id = id,
                    Title = (c["title"]?.InnerText ?? "").Trim(),
                    Summary = (c["summary"]?.InnerText ?? "").Trim(),
                    Status = (c.GetAttribute("status") ?? "").Trim()
                };
            }

            var allIdToTitle = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var allTitleToId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var course in catalog.Values)
            {
                allIdToTitle[course.Id] = course.Title;
                if (!string.IsNullOrWhiteSpace(course.Title) && !allTitleToId.ContainsKey(course.Title))
                    allTitleToId[course.Title] = course.Id;
            }

            foreach (XmlElement c in doc.SelectNodes("/microcourses/course"))
            {
                var id = c.GetAttribute("id");
                if (string.IsNullOrWhiteSpace(id) || !catalog.ContainsKey(id))
                    continue;

                var prereqIds = new List<string>();

                foreach (XmlElement p in c.SelectNodes("prerequisites/course"))
                {
                    var resolved = ResolveRecommendationCourseIdentifier(
                        p.GetAttribute("id"),
                        allIdToTitle,
                        allTitleToId);

                    if (!string.IsNullOrWhiteSpace(resolved) &&
                        !prereqIds.Any(x => string.Equals(x, resolved, StringComparison.OrdinalIgnoreCase)))
                    {
                        prereqIds.Add(resolved);
                    }
                }

                var singlePrereq = c.SelectSingleNode("prerequisite") as XmlElement;
                if (singlePrereq != null)
                {
                    var resolved = ResolveRecommendationCourseIdentifier(
                        singlePrereq.GetAttribute("id"),
                        allIdToTitle,
                        allTitleToId);

                    if (!string.IsNullOrWhiteSpace(resolved) &&
                        !prereqIds.Any(x => string.Equals(x, resolved, StringComparison.OrdinalIgnoreCase)))
                    {
                        prereqIds.Add(resolved);
                    }
                }

                var prereqAttr = c.GetAttribute("prerequisiteId");
                if (!string.IsNullOrWhiteSpace(prereqAttr))
                {
                    var resolved = ResolveRecommendationCourseIdentifier(
                        prereqAttr,
                        allIdToTitle,
                        allTitleToId);

                    if (!string.IsNullOrWhiteSpace(resolved) &&
                        !prereqIds.Any(x => string.Equals(x, resolved, StringComparison.OrdinalIgnoreCase)))
                    {
                        prereqIds.Add(resolved);
                    }
                }

                catalog[id].PrereqIds = prereqIds;
            }

            return catalog;
        }

        private string ResolveRecommendationCourseIdentifier(
            string ident,
            Dictionary<string, string> allIdToTitle,
            Dictionary<string, string> allTitleToId)
        {
            if (string.IsNullOrWhiteSpace(ident))
                return null;

            if (allIdToTitle.ContainsKey(ident))
                return ident;

            if (allTitleToId.TryGetValue(ident, out var idFromTitle))
                return idFromTitle;

            return null;
        }

        private Dictionary<string, string> BuildRecommendationAliases(Dictionary<string, RecommendationCourse> catalog)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var course in catalog.Values)
            {
                var normalized = NormalizeRecommendationKey(course.Title);
                if (!string.IsNullOrWhiteSpace(normalized) && !map.ContainsKey(normalized))
                    map[normalized] = course.Id;
            }

            TryAddRecommendationAlias(map, catalog, "Two-Factor Authentication Setup and Management", "2FA Setup And Management");
            TryAddRecommendationAlias(map, catalog, "Detecting Spyware Infections on Devices", "Detecting Spyware Infection on Devices");
            TryAddRecommendationAlias(map, catalog, "Managing Your Digital Footprint", "Managing Digital Footprint");
            TryAddRecommendationAlias(map, catalog, "Identifying Hidden Surveillance Devices (Electronic Scanning)", "Identifying Hidden-Surveilance Devices");

            return map;
        }

        private void TryAddRecommendationAlias(
            Dictionary<string, string> map,
            Dictionary<string, RecommendationCourse> catalog,
            string quizTitle,
            string courseTitle)
        {
            var match = catalog.Values.FirstOrDefault(c =>
                string.Equals(c.Title, courseTitle, StringComparison.OrdinalIgnoreCase));

            if (match == null)
                return;

            var normalizedQuizTitle = NormalizeRecommendationKey(quizTitle);
            if (!string.IsNullOrWhiteSpace(normalizedQuizTitle))
                map[normalizedQuizTitle] = match.Id;
        }

        private string NormalizeRecommendationKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            value = value.Replace("&", " and ")
                         .Replace("-", " ")
                         .Replace("/", " ")
                         .Replace("(", " ")
                         .Replace(")", " ");

            var chars = value.ToLowerInvariant()
                             .Select(ch => char.IsLetterOrDigit(ch) ? ch : ' ')
                             .ToArray();

            return string.Join(
                " ",
                new string(chars).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        }

        private string ResolveRecommendationCourseId(
            string domainTitle,
            Dictionary<string, string> aliases)
        {
            if (string.IsNullOrWhiteSpace(domainTitle))
                return null;

            aliases.TryGetValue(NormalizeRecommendationKey(domainTitle), out var courseId);
            return courseId;
        }

        private HashSet<string> LoadUpcomingCourseIdsForEvent(string eventId)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(eventId) || !File.Exists(EventSessionsXmlPath))
                return set;

            var doc = new XmlDocument();
            doc.Load(EventSessionsXmlPath);

            foreach (XmlElement s in doc.SelectNodes($"/eventSessions/session[@eventId='{eventId}']"))
            {
                var courseId = (s["courseId"]?.InnerText ?? "").Trim();
                var endIso = (s["end"]?.InnerText ?? "").Trim();

                if (string.IsNullOrWhiteSpace(courseId))
                    continue;

                if (DateTime.TryParse(
                        endIso,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                        out var endUtc) &&
                    endUtc >= DateTime.UtcNow)
                {
                    set.Add(courseId);
                }
            }

            return set;
        }

        private List<RecommendationRow> BuildRecommendationRows(
    QuizService.ScoreResult latest,
    Dictionary<string, RecommendationCourse> catalog,
    Dictionary<string, string> aliases,
    HashSet<string> completedSet,
    HashSet<string> visibleCourseIds,
    HashSet<string> upcomingCourseIds)
        {
            var rowsByCourseId = new Dictionary<string, RecommendationRow>(StringComparer.OrdinalIgnoreCase);

            var orderedDomains = latest.DomainScores
                .Where(kv => kv.Value > 0)
                .OrderByDescending(kv => kv.Value)
                .ThenBy(kv => kv.Key)
                .ToList();

            int sortOrder = 0;

            foreach (var domain in orderedDomains)
            {
                var targetCourseId = ResolveRecommendationCourseId(domain.Key, aliases);
                if (string.IsNullOrWhiteSpace(targetCourseId))
                    continue;

                if (!catalog.TryGetValue(targetCourseId, out var targetCourse))
                    continue;

                if (completedSet.Contains(targetCourseId))
                    continue;

                var chain = GetRecursiveRecommendationChain(targetCourseId, catalog, completedSet);

                if (chain.Count == 0)
                    continue;

                // No unmet prereq chain, so recommend the target directly.
                if (chain.Count == 1 && string.Equals(chain[0], targetCourseId, StringComparison.OrdinalIgnoreCase))
                {
                    UpsertRecommendationRow(
                        rowsByCourseId,
                        targetCourse,
                        sortOrder,
                        domain.Value,
                        $"Recommended because your {domain.Key} gap was {domain.Value:0.##}/10, which suggests a bigger need in this area right now.",
                        isPrereq: false,
                        isLockedByPrereq: false,
                        visibleCourseIds: visibleCourseIds,
                        upcomingCourseIds: upcomingCourseIds);

                    sortOrder++;
                    continue;
                }

                // Recursive chain example: [C, B, A]
                // C = take first, B = needed next, A = target and still locked
                for (int i = 0; i < chain.Count; i++)
                {
                    var courseId = chain[i];
                    if (!catalog.TryGetValue(courseId, out var course))
                        continue;

                    if (completedSet.Contains(courseId))
                        continue;

                    bool isFirstAction = (i == 0);
                    bool isTarget = string.Equals(courseId, targetCourseId, StringComparison.OrdinalIgnoreCase);
                    bool isPrereq = !isTarget;
                    bool isLockedByPrereq = !isFirstAction;

                    string previousTitle = null;
                    string nextTitle = null;

                    if (i > 0 && catalog.TryGetValue(chain[i - 1], out var previousCourse))
                        previousTitle = previousCourse.Title;

                    if (i + 1 < chain.Count && catalog.TryGetValue(chain[i + 1], out var nextCourse))
                        nextTitle = nextCourse.Title;

                    string reason;
                    if (isFirstAction)
                    {
                        reason =
                            $"Start here first. This microcourse is being recommended because it is a prerequisite for \"{nextTitle}\" and needs to be completed before you can move forward in this learning path.";
                    }
                    else if (isTarget)
                    {
                        reason =
                            $"This directly matches your {domain.Key} gap ({domain.Value:0.##}/10), but it stays locked until you complete \"{previousTitle}\".";
                    }
                    else
                    {
                        reason =
                            $"Complete this after \"{previousTitle}\". It is still required before you can unlock \"{nextTitle}\".";
                    }

                    UpsertRecommendationRow(
                        rowsByCourseId,
                        course,
                        sortOrder,
                        domain.Value,
                        reason,
                        isPrereq: isPrereq,
                        isLockedByPrereq: isLockedByPrereq,
                        visibleCourseIds: visibleCourseIds,
                        upcomingCourseIds: upcomingCourseIds);

                    sortOrder++;
                }
            }

            return rowsByCourseId.Values
                .OrderBy(r => r.SortOrder)
                .ThenByDescending(r => r.GapScore)
                .Take(4)
                .ToList();
        }

        private List<string> GetRecursiveRecommendationChain(
    string targetCourseId,
    Dictionary<string, RecommendationCourse> catalog,
    HashSet<string> completedSet)
        {
            var ordered = new List<string>();
            var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            CollectRecursiveRecommendationChain(
                targetCourseId,
                catalog,
                completedSet,
                ordered,
                visiting);

            return ordered;
        }

        private void CollectRecursiveRecommendationChain(
            string courseId,
            Dictionary<string, RecommendationCourse> catalog,
            HashSet<string> completedSet,
            List<string> ordered,
            HashSet<string> visiting)
        {
            if (string.IsNullOrWhiteSpace(courseId))
                return;

            if (completedSet.Contains(courseId))
                return;

            if (!catalog.TryGetValue(courseId, out var course))
                return;

            // Cycle protection: prevents infinite loops on bad XML like A -> B -> A
            if (!visiting.Add(courseId))
                return;

            var unmetPrereqs = (course.PrereqIds ?? new List<string>())
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Where(id => !completedSet.Contains(id))
                .ToList();

            foreach (var prereqId in unmetPrereqs)
            {
                CollectRecursiveRecommendationChain(
                    prereqId,
                    catalog,
                    completedSet,
                    ordered,
                    visiting);
            }

            if (!ordered.Any(x => string.Equals(x, courseId, StringComparison.OrdinalIgnoreCase)))
            {
                ordered.Add(courseId);
            }

            visiting.Remove(courseId);
        }

        private void UpsertRecommendationRow(
            Dictionary<string, RecommendationRow> rowsByCourseId,
            RecommendationCourse course,
            int sortOrder,
            double gapScore,
            string reason,
            bool isPrereq,
            bool isLockedByPrereq,
            HashSet<string> visibleCourseIds,
            HashSet<string> upcomingCourseIds)
        {
            var isPublished = string.Equals(course.Status, "Published", StringComparison.OrdinalIgnoreCase);
            var enforceVisibility = visibleCourseIds != null && visibleCourseIds.Count > 0;
            var inThisEvent = !enforceVisibility || visibleCourseIds.Contains(course.Id);
            var hasUpcomingSession = upcomingCourseIds != null && upcomingCourseIds.Contains(course.Id);

            var hasMatchingSessions = inThisEvent && hasUpcomingSession;
            var canSignUp = isPublished && !isLockedByPrereq && hasMatchingSessions;

            var availabilityText =
                !isPublished ? "Coming soon" :
                isLockedByPrereq ? "Finish the prerequisite first" :
                !inThisEvent ? "Not offered in this event" :
                !hasUpcomingSession ? "No session posted yet" :
                "Ready to sign up";

            var badgeText =
    isPrereq && !isLockedByPrereq ? "Take this first" :
    isPrereq && isLockedByPrereq ? "Needed next" :
    isLockedByPrereq ? "Locked until prereq" :
    "Recommended";

            var badgeCss =
                isPrereq && !isLockedByPrereq ? "pill pill-pink" :
                "pill pill-link";

            if (rowsByCourseId.TryGetValue(course.Id, out var existing))
            {
                if (sortOrder < existing.SortOrder)
                    existing.SortOrder = sortOrder;

                if (gapScore > existing.GapScore)
                {
                    existing.GapScore = gapScore;
                    existing.GapText = $"Gap {gapScore:0.##}/10";
                }

                if (isPrereq && !isLockedByPrereq)
                {
                    existing.BadgeText = "Take this first";
                    existing.BadgeCss = "pill pill-pink";
                    existing.Reason = reason;
                    existing.AvailabilityText = availabilityText;
                    existing.CanSignUp = canSignUp;
                }
                else if (isPrereq && isLockedByPrereq)
                {
                    if (!string.Equals(existing.BadgeText, "Take this first", StringComparison.OrdinalIgnoreCase))
                    {
                        existing.BadgeText = "Needed next";
                        existing.BadgeCss = "pill pill-link";
                    }

                    existing.CanSignUp = false;
                    existing.AvailabilityText = "Finish the earlier prerequisite first";
                    existing.Reason = reason;
                }
                else if (isLockedByPrereq)
                {
                    if (!string.Equals(existing.BadgeText, "Take this first", StringComparison.OrdinalIgnoreCase))
                    {
                        existing.BadgeText = "Locked until prereq";
                        existing.BadgeCss = "pill pill-link";
                    }

                    existing.CanSignUp = false;
                    existing.AvailabilityText = "Finish the prerequisite first";
                    existing.Reason = reason;
                }

                else
                {
                    var keepPrereqMessaging =
                        string.Equals(existing.BadgeText, "Take this first", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(existing.BadgeText, "Needed next", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(existing.BadgeText, "Locked until prereq", StringComparison.OrdinalIgnoreCase);

                    if (!keepPrereqMessaging)
                    {
                        existing.BadgeText = "Recommended";
                        existing.BadgeCss = "pill pill-link";
                        existing.Reason = reason;
                        existing.AvailabilityText = availabilityText;
                        existing.CanSignUp = canSignUp;
                    }
                }

                if (hasMatchingSessions)
                    existing.HasMatchingSessions = true;

                return;
            }

            rowsByCourseId[course.Id] = new RecommendationRow
            {
                SortOrder = sortOrder,
                GapScore = gapScore,
                CourseId = course.Id,
                Title = course.Title,
                Summary = course.Summary,
                Reason = reason,
                AvailabilityText = availabilityText,
                BadgeText = badgeText,
                BadgeCss = badgeCss,
                GapText = $"Gap {gapScore:0.##}/10",
                CanSignUp = canSignUp,
                HasMatchingSessions = hasMatchingSessions
            };
        }

        private void LoadTagOptionsIntoUi(IEnumerable<string> allTags)
        {
            FilterTags.Items.Clear();
            foreach (var t in allTags.OrderBy(x => x))
                FilterTags.Items.Add(new ListItem(t, t));
        }

        private static DateTime? ParseLocal(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            if (DateTime.TryParse(s, out var dt)) return dt;
            return null;
        }

        private FilterState ReadFiltersFromQuery()
        {
            var f = new FilterState();
            f.From = ParseLocal(Request.QueryString["from"]);
            f.To = ParseLocal(Request.QueryString["to"]);
            var tags = (Request.QueryString["tags"] ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var t in tags) f.Tags.Add(t);
            f.Query = (Request.QueryString["q"] ?? "").Trim();
            return f;
        }

        private string BuildFilterQueryString(FilterState f)
        {
            var qs = HttpUtility.ParseQueryString(string.Empty);
            if (f.From.HasValue) qs["from"] = f.From.Value.ToString("yyyy-MM-ddTHH:mm");
            if (f.To.HasValue) qs["to"] = f.To.Value.ToString("yyyy-MM-ddTHH:mm");
            if (f.Tags.Count > 0) qs["tags"] = string.Join(",", f.Tags);
            if (!string.IsNullOrWhiteSpace(f.Query)) qs["q"] = f.Query;
            return qs.ToString();
        }

        private sealed class IntervalList : List<Interval> { }

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

                var courseId = sn["courseId"]?.InnerText ?? "";
                var title = LoadCourseTitles().TryGetValue(courseId, out var t) ? t : "(untitled)";

                list.Add(new Interval { StartUtc = startUtc, EndUtc = endUtc, SessionId = sid, Title = title });
            }

            return list;
        }

        private sealed class RecommendationCourse
        {
            public string Id { get; set; }
            public string Title { get; set; }
            public string Summary { get; set; }
            public string Status { get; set; }
            public List<string> PrereqIds { get; set; } = new List<string>();
        }

        private sealed class RecommendationRow
        {
            public int SortOrder { get; set; }
            public double GapScore { get; set; }
            public string CourseId { get; set; }
            public string Title { get; set; }
            public string Summary { get; set; }
            public string Reason { get; set; }
            public string AvailabilityText { get; set; }
            public string BadgeText { get; set; }
            public string BadgeCss { get; set; }
            public string GapText { get; set; }
            public bool CanSignUp { get; set; }
            public bool HasMatchingSessions { get; set; }
        }

        private bool SessionOverlapsUser(string eventId, string userId, string sessionId, out Interval conflictWith)
        {
            conflictWith = null;
            if (!File.Exists(EventSessionsXmlPath)) return false;

            var ses = new XmlDocument(); ses.Load(EventSessionsXmlPath);
            var s = (XmlElement)ses.SelectSingleNode($"/eventSessions/session[@eventId='{eventId}' and @id='{sessionId}']");
            if (s == null) return false;

            var startIso = s["start"]?.InnerText ?? "";
            var endIso = s["end"]?.InnerText ?? "";
            if (!DateTime.TryParse(startIso, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var startUtc)) return false;
            if (!DateTime.TryParse(endIso, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var endUtc)) return false;

            var target = new Interval { StartUtc = startUtc, EndUtc = endUtc, SessionId = sessionId };
            var mine = GetUserEnrolledIntervals(eventId, CurrentUserId());
            foreach (var m in mine)
            {
                if (Overlaps(target, m)) { conflictWith = m; return true; }
            }
            return false;
        }

        private (DateTime startUtc, DateTime endUtc)? GetSessionTimes(string eventId, string sessionId)
        {
            if (!File.Exists(EventSessionsXmlPath)) return null;
            var ses = new XmlDocument(); ses.Load(EventSessionsXmlPath);
            var s = (XmlElement)ses.SelectSingleNode($"/eventSessions/session[@eventId='{eventId}' and @id='{sessionId}']");
            if (s == null) return null;
            if (!DateTime.TryParse(s["start"]?.InnerText ?? "", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var startUtc)) return null;
            if (!DateTime.TryParse(s["end"]?.InnerText ?? "", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var endUtc)) return null;
            return (startUtc, endUtc);
        }

        // NEW: 1-based position of the current user in a session's waitlist (0 if not present)
        private int GetWaitlistPosition(string eventId, string sessionId, string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return 0;
            var doc = LoadEnrollmentsDoc();
            var ses = (XmlElement)doc.SelectSingleNode($"/enrollments/session[@eventId='{eventId}' and @id='{sessionId}']");
            if (ses == null) return 0;

            var list = ses.SelectNodes("waitlist/user");
            if (list == null) return 0;

            int idx = 1;
            foreach (XmlElement u in list)
            {
                if (string.Equals(u.GetAttribute("id"), userId, StringComparison.OrdinalIgnoreCase))
                    return idx;
                idx++;
            }
            return 0;
        }

        // NEW: Gets the participant's 1-based position inside the enrolled queue.
        // This uses the order of <enrolled><user> nodes in enrollments.xml.
        private int GetEnrolledQueuePosition(XmlElement enrollmentSessionNode, string userId)
        {
            if (enrollmentSessionNode == null || string.IsNullOrWhiteSpace(userId))
            {
                return 0;
            }

            var enrolledUsers = enrollmentSessionNode.SelectNodes("enrolled/user");

            if (enrolledUsers == null)
            {
                return 0;
            }

            var position = 1;

            foreach (XmlElement user in enrolledUsers)
            {
                if (string.Equals(user.GetAttribute("id"), userId, StringComparison.OrdinalIgnoreCase))
                {
                    return position;
                }

                position++;
            }

            return 0;
        }

        private void BindSessions(string eventId, FilterState filters)
        {
            var visibleCourseIds = LoadEventVisibleCourseIds(eventId);
            var idToTitle = LoadCourseTitles();
            var titleToId = LoadTitleToIdMap();
            var prereqsByCourseId = LoadPrereqMapMultiple(idToTitle, titleToId);
            var completedSet = LoadUserCompletedSet(CurrentUserId());
            var courseTags = LoadCourseTags();

            var rows = LoadUpcomingSessionRows(
                eventId, visibleCourseIds, idToTitle,
                prereqsByCourseId, completedSet, CurrentUserId(),
                filters, courseTags);

            EmptySessionsPH.Visible = rows.Count == 0;
            SessionsRepeater.DataSource = rows;
            SessionsRepeater.DataBind();
        }

        private HashSet<string> LoadEventVisibleCourseIds(string eventId)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(EventCoursesXmlPath)) return set;

            var doc = new XmlDocument(); doc.Load(EventCoursesXmlPath);
            foreach (XmlElement c in doc.SelectNodes($"/eventCourses/event[@id='{eventId}']/course[@enabled='true']"))
            {
                var id = c.GetAttribute("id");
                if (!string.IsNullOrEmpty(id)) set.Add(id);
            }
            return set;
        }

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

        private Dictionary<string, string> LoadTitleToIdMap()
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(MicrocoursesXmlPath)) return map;

            var doc = new XmlDocument(); doc.Load(MicrocoursesXmlPath);
            foreach (XmlElement c in doc.SelectNodes("/microcourses/course[@status='Published']"))
            {
                var id = c.GetAttribute("id");
                var title = c["title"]?.InnerText;
                if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(title))
                {
                    map[title] = id;
                }
            }
            return map;
        }

        private string ResolveCourseIdentifier(string ident, Dictionary<string, string> idToTitle, Dictionary<string, string> titleToId)
        {
            if (string.IsNullOrWhiteSpace(ident)) return null;
            if (idToTitle.ContainsKey(ident)) return ident;
            if (titleToId.TryGetValue(ident, out var idFromTitle)) return idFromTitle;
            return null;
        }

        private Dictionary<string, List<string>> LoadPrereqMapMultiple(Dictionary<string, string> idToTitle, Dictionary<string, string> titleToId)
        {
            var map = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(MicrocoursesXmlPath)) return map;

            var doc = new XmlDocument(); doc.Load(MicrocoursesXmlPath);
            foreach (XmlElement c in doc.SelectNodes("/microcourses/course"))
            {
                var courseId = c.GetAttribute("id");
                if (string.IsNullOrWhiteSpace(courseId)) continue;

                var list = new List<string>();
                foreach (XmlElement p in c.SelectNodes("prerequisites/course"))
                {
                    var ident = p.GetAttribute("id");
                    var resolved = ResolveCourseIdentifier(ident, idToTitle, titleToId);
                    if (!string.IsNullOrWhiteSpace(resolved)) list.Add(resolved);
                }
                var singleChild = (XmlElement)c.SelectSingleNode("prerequisite");
                if (singleChild != null)
                {
                    var ident = singleChild.GetAttribute("id");
                    var resolved = ResolveCourseIdentifier(ident, idToTitle, titleToId);
                    if (!string.IsNullOrWhiteSpace(resolved) && !list.Contains(resolved, StringComparer.OrdinalIgnoreCase))
                        list.Add(resolved);
                }
                var attrIdent = c.GetAttribute("prerequisiteId");
                if (!string.IsNullOrWhiteSpace(attrIdent))
                {
                    var resolved = ResolveCourseIdentifier(attrIdent, idToTitle, titleToId);
                    if (!string.IsNullOrWhiteSpace(resolved) && !list.Contains(resolved, StringComparer.OrdinalIgnoreCase))
                        list.Add(resolved);
                }
                if (list.Count > 0) map[courseId] = list;
            }
            return map;
        }



        private HashSet<string> LoadUserCompletedSet(string userId)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(userId)) return set;
            if (!File.Exists(CompletionsXmlPath)) return set;

            var doc = new XmlDocument(); doc.Load(CompletionsXmlPath);
            foreach (XmlElement c in doc.SelectNodes($"/completions/user[@id='{userId}']/course"))
            {
                var id = c.GetAttribute("id");
                if (!string.IsNullOrWhiteSpace(id)) set.Add(id);
            }
            return set;
        }

        private void MarkCourseCompleted(string userId, string courseId)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(courseId)) return;

            lock (CompletionsLock)
            {
                EnsureXmlDoc(CompletionsXmlPath, "completions");
                var doc = new XmlDocument(); doc.Load(CompletionsXmlPath);

                var user = (XmlElement)doc.SelectSingleNode($"/completions/user[@id='{userId}']");
                if (user == null)
                {
                    user = doc.CreateElement("user");
                    user.SetAttribute("id", userId);
                    doc.DocumentElement.AppendChild(user);
                }

                var existing = (XmlElement)user.SelectSingleNode($"course[@id='{courseId}']");
                if (existing == null)
                {
                    var c = doc.CreateElement("course");
                    c.SetAttribute("id", courseId);
                    c.SetAttribute("completedOn", DateTime.UtcNow.ToString("yyyy-MM-dd"));
                    user.AppendChild(c);
                }
                else
                {
                    if (!existing.HasAttribute("completedOn"))
                        existing.SetAttribute("completedOn", DateTime.UtcNow.ToString("yyyy-MM-dd"));
                }

                doc.Save(CompletionsXmlPath);
            }
        }

        private List<object> LoadUpcomingSessionRows(
            string eventId,
            HashSet<string> visibleCourseIds,
            Dictionary<string, string> idToTitle,
            Dictionary<string, List<string>> prereqsByCourseId,
            HashSet<string> completedSet,
            string userId,
            FilterState filters,
            Dictionary<string, HashSet<string>> courseTags)
        {
            var rows = new List<object>();
            if (!File.Exists(EventSessionsXmlPath)) return rows;

            var doc = new XmlDocument(); doc.Load(EventSessionsXmlPath);
            var myIntervals = GetUserEnrolledIntervals(eventId, userId);

            foreach (XmlElement s in doc.SelectNodes($"/eventSessions/session[@eventId='{eventId}']"))
            {
                var courseId = s["courseId"]?.InnerText ?? "";
                if (string.IsNullOrEmpty(courseId)) continue;
                if (visibleCourseIds.Count > 0 && !visibleCourseIds.Contains(courseId)) continue;

                // Hide sessions for courses the user already completed
                if (completedSet != null && completedSet.Contains(courseId))
                    continue;

                var startIso = s["start"]?.InnerText ?? "";
                var endIso = s["end"]?.InnerText ?? "";
                if (!DateTime.TryParse(endIso, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var endUtc))
                {
                    if (!DateTime.TryParse(endIso, out endUtc)) continue;
                }
                if (endUtc < DateTime.UtcNow) continue;

                DateTime startUtc;
                if (!DateTime.TryParse(startIso, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out startUtc))
                {
                    DateTime.TryParse(startIso, out startUtc);
                }
                var startLocal = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc).ToLocalTime();

                if (filters.From.HasValue && startLocal < filters.From.Value) continue;
                if (filters.To.HasValue && startLocal > filters.To.Value) continue;

                var sessionId = s.GetAttribute("id");
                if (string.IsNullOrWhiteSpace(sessionId))
                    sessionId = $"{courseId}|{startUtc:o}";

                var room = s["room"]?.InnerText ?? "";
                var helperName = s["helper"]?.InnerText ?? "";
                var capStr = s["capacity"]?.InnerText ?? "0";
                var capacity = int.TryParse(capStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var capVal) ? capVal : 0;

                idToTitle.TryGetValue(courseId, out var title);
                var microcourseTitle = string.IsNullOrWhiteSpace(title) ? "(untitled)" : title;

                if (!string.IsNullOrWhiteSpace(filters.Query))
                {
                    var q = filters.Query.Trim();
                    var hay = $"{microcourseTitle}|{helperName}|{room}";
                    if (hay.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0) continue;
                }

                if (filters.Tags.Count > 0)
                {
                    courseTags.TryGetValue(courseId, out var tagsForCourse);
                    if (tagsForCourse == null || !tagsForCourse.Overlaps(filters.Tags))
                        continue;
                }

                var enrolledCount = GetEnrolledCount(eventId, sessionId);
                var remaining = Math.Max(0, capacity - enrolledCount);
                var isFull = remaining == 0;
                var isEnrolled = IsUserEnrolled(eventId, sessionId, userId);
                var isWaitlisted = IsUserWaitlisted(eventId, sessionId, userId);

                var waitlistPosition = isWaitlisted ? GetWaitlistPosition(eventId, sessionId, userId) : 0;

                var prereqsByCourse = prereqsByCourseId;
                prereqsByCourse.TryGetValue(courseId, out var prereqList);
                prereqList = prereqList ?? new List<string>();

                var completedSetLocal = completedSet;
                var unmet = prereqList.Where(p => !completedSetLocal.Contains(p)).ToList();

                var prereqMet = unmet.Count == 0;
                var missingPrereqId = prereqMet ? null : unmet[0];
                string missingPrereqTitle = null;
                if (!string.IsNullOrWhiteSpace(missingPrereqId))
                {
                    idToTitle.TryGetValue(missingPrereqId, out missingPrereqTitle);
                    if (string.IsNullOrWhiteSpace(missingPrereqTitle)) missingPrereqTitle = missingPrereqId;
                }

                var hasConflict = false;
                string conflictMessage = "";
                string replacementUrl = "";
                if (!isEnrolled && !isWaitlisted)
                {
                    var target = new Interval { StartUtc = startUtc, EndUtc = endUtc, SessionId = sessionId, Title = microcourseTitle };
                    foreach (var m in myIntervals)
                    {
                        if (Overlaps(target, m))
                        {
                            hasConflict = true;
                            var mLocalStart = DateTime.SpecifyKind(m.StartUtc, DateTimeKind.Utc).ToLocalTime();
                            var mLocalEnd = DateTime.SpecifyKind(m.EndUtc, DateTimeKind.Utc).ToLocalTime();
                            conflictMessage = $"It overlaps with your existing session “{m.Title}” ({mLocalStart:ddd, MMM d • h:mm tt}–{mLocalEnd:h:mm tt}).";
                            var returnQs = Request.Url.Query ?? "";
                            replacementUrl = ResolveUrl($"~/Account/Participant/Replacements.aspx?eventId={HttpUtility.UrlEncode(eventId)}&sessionId={HttpUtility.UrlEncode(sessionId)}&return={HttpUtility.UrlEncode(returnQs)}");
                            break;
                        }
                    }
                }

                rows.Add(new
                {
                    microcourseTitle,
                    helperName,
                    remainingSeats = remaining,
                    capacity,
                    room,
                    startLocal,
                    sessionId,
                    isFull,
                    isEnrolled,
                    isWaitlisted,
                    waitlistPosition,
                    courseId,
                    prereqMet,
                    missingPrereqId,
                    missingPrereqTitle,
                    hasConflict,
                    conflictMessage,
                    replacementUrl
                });
            }

            return rows.OrderBy(r => (DateTime)r.GetType().GetProperty("startLocal").GetValue(r, null)).ToList();
        }

        private void BindMySessions(string eventId, string userId)
        {
            var rows = new List<object>();
            if (string.IsNullOrWhiteSpace(userId)) { MySessionsWrap.Visible = false; return; }
            if (!File.Exists(EventSessionsXmlPath)) { MySessionsWrap.Visible = false; return; }

            var docSess = new XmlDocument(); docSess.Load(EventSessionsXmlPath);
            var docEnr = LoadEnrollmentsDoc();

            // NEW: capture all enrolled intervals to detect conflicts across "My Sessions".
            var myIntervals = GetUserEnrolledIntervals(eventId, userId);

            foreach (XmlElement sesNode in docEnr.SelectNodes($"/enrollments/session[@eventId='{eventId}']"))
            {
                var sid = sesNode.GetAttribute("id");

                // include waitlisted sessions, not only enrolled
                bool isEnrolled = sesNode.SelectSingleNode($"enrolled/user[@id='{userId}']") != null;
                bool isWaitlisted = !isEnrolled && sesNode.SelectSingleNode($"waitlist/user[@id='{userId}']") != null;
                if (!isEnrolled && !isWaitlisted) continue;

                var s = (XmlElement)docSess.SelectSingleNode($"/eventSessions/session[@eventId='{eventId}' and @id='{sid}']");
                if (s == null) continue;

                var courseId = s["courseId"]?.InnerText ?? "";

                // NEW: queue display values for multi-participant enrolled sessions
                var capStr = (s["capacity"]?.InnerText ?? "0").Trim();
                var capacity = int.TryParse(capStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var capVal)
                    ? capVal
                    : 0;

                var enrolledCount = sesNode.SelectNodes("enrolled/user")?.Count ?? 0;
                var queuePosition = isEnrolled ? GetEnrolledQueuePosition(sesNode, userId) : 0;

                // Parse current start/end for the session (UTC → local)
                var startIso = s["start"]?.InnerText ?? "";
                var endIso = s["end"]?.InnerText ?? "";

                DateTime.TryParse(
                    startIso,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                    out var startUtc);

                DateTime.TryParse(
                    endIso,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                    out var endUtc);

                var startLocal = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc).ToLocalTime();
                var endLocal = DateTime.SpecifyKind(endUtc, DateTimeKind.Utc).ToLocalTime();

                var room = s["room"]?.InnerText ?? "";
                var helperName = s["helper"]?.InnerText ?? "";

                var titles = LoadCourseTitles();
                titles.TryGetValue(courseId, out var title);
                var microcourseTitle = string.IsNullOrWhiteSpace(title) ? "(untitled)" : title;

                // --- NEW: detect & describe a time change for this session ---
                bool timeChanged = false;
                string timeChangedMessage = string.Empty;

                var timeChangeNode = s["timeChange"] as XmlElement;
                if (timeChangeNode != null)
                {
                    var oldStartIso = timeChangeNode["oldStart"]?.InnerText ?? "";
                    var oldEndIso = timeChangeNode["oldEnd"]?.InnerText ?? "";

                    if (!string.IsNullOrWhiteSpace(oldStartIso) && !string.IsNullOrWhiteSpace(oldEndIso))
                    {
                        if (DateTime.TryParse(
                                oldStartIso,
                                CultureInfo.InvariantCulture,
                                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                                out var oldStartUtc) &&
                            DateTime.TryParse(
                                oldEndIso,
                                CultureInfo.InvariantCulture,
                                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                                out var oldEndUtc))
                        {
                            var oldStartLocal = DateTime.SpecifyKind(oldStartUtc, DateTimeKind.Utc).ToLocalTime();
                            var oldEndLocal = DateTime.SpecifyKind(oldEndUtc, DateTimeKind.Utc).ToLocalTime();

                            timeChanged = true;
                            timeChangedMessage =
                                $"The time for “{microcourseTitle}” was updated: " +
                                $"was {oldStartLocal:ddd, MMM d • h:mm tt}–{oldEndLocal:h:mm tt}, " +
                                $"now {startLocal:ddd, MMM d • h:mm tt}–{endLocal:h:mm tt}.";
                        }
                    }
                }

                // compute waitlist position for "My Sessions" when applicable
                var waitlistPosition = isWaitlisted ? GetWaitlistPosition(eventId, sid, userId) : 0;

                // NEW: Total queue position includes enrolled participants first, then waitlisted users.
                // Example: capacity/enrolled count is 3 and this user is waitlist position 2,
                // so their overall queue position is 5.
                
                var overallQueuePosition = isWaitlisted && waitlistPosition > 0
                    ? enrolledCount + waitlistPosition
                    : 0;

                // Only participants the Helper has "admitted" see the room link.
                bool isAdmitted = false;
                if (isEnrolled)
                {
                    var thisUserNode = sesNode.SelectSingleNode($"enrolled/user[@id='{userId}']") as XmlElement;
                    if (thisUserNode != null &&
                        string.Equals(thisUserNode.GetAttribute("admitted"), "true", StringComparison.OrdinalIgnoreCase))
                    {
                        isAdmitted = true;
                    }
                }

                bool canSeeRoom = !string.IsNullOrWhiteSpace(room) && isEnrolled && isAdmitted;

                // --- NEW: detect conflicts vs other enrolled sessions ---
                bool hasConflict = false;
                string conflictMessage = string.Empty;
                string replacementUrl = string.Empty;

                var returnQuery = Request.Url.Query ?? "";
                var swapUrl = ResolveUrl(
                    $"~/Account/Participant/SwapSession.aspx?eventId={HttpUtility.UrlEncode(eventId)}" +
                    $"&sessionId={HttpUtility.UrlEncode(sid)}" +
                    $"&return={HttpUtility.UrlEncode(returnQuery)}"
                );

                var thisInterval = myIntervals
                    .FirstOrDefault(m => string.Equals(m.SessionId, sid, StringComparison.OrdinalIgnoreCase));

                if (thisInterval != null)
                {
                    foreach (var other in myIntervals)
                    {
                        // Skip comparing the session to itself
                        if (string.Equals(other.SessionId, sid, StringComparison.OrdinalIgnoreCase))
                            continue;

                        if (Overlaps(thisInterval, other))
                        {
                            hasConflict = true;

                            var mLocalStart = DateTime.SpecifyKind(other.StartUtc, DateTimeKind.Utc).ToLocalTime();
                            var mLocalEnd = DateTime.SpecifyKind(other.EndUtc, DateTimeKind.Utc).ToLocalTime();

                            conflictMessage =
                                $"It overlaps with your other session “{other.Title}” " +
                                $"({mLocalStart:ddd, MMM d • h:mm tt}–{mLocalEnd:h:mm tt}).";

                            var returnQs = Request.Url.Query ?? "";
                            replacementUrl = ResolveUrl(
                                $"~/Account/Participant/Replacements.aspx?eventId={HttpUtility.UrlEncode(eventId)}" +
                                $"&sessionId={HttpUtility.UrlEncode(sid)}" +
                                $"&return={HttpUtility.UrlEncode(returnQs)}"
                            );
                            break;
                        }
                    }
                }


                rows.Add(new
                {
                    microcourseTitle,
                    helperName,
                    room,
                    startLocal,
                    sessionId = sid,
                    isEnrolled,
                    isWaitlisted,
                    waitlistPosition,
                    canSeeRoom,
                    timeChanged,
                    timeChangedMessage,
                    hasConflict,
                    conflictMessage,
                    replacementUrl,
                    swapUrl,

                    // Enrolled queue card values
                    capacity,
                    enrolledCount,
                    queuePosition,
                    showQueuePosition = isEnrolled && capacity > 1 && queuePosition > 0,

                    // Waitlist queue card values
                    overallQueuePosition,
                    showWaitlistPosition = isWaitlisted && overallQueuePosition > 0
                });
            }

            MySessionsWrap.Visible = rows.Count > 0;
            MySessionsRepeater.DataSource = rows
                .OrderBy(r => (DateTime)r.GetType().GetProperty("startLocal").GetValue(r, null))
                .ToList();
            MySessionsRepeater.DataBind();
        }

        protected void SessionsRepeater_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            var userId = CurrentUserId();
            var eventId = (string)Session["EventId"] ?? "";
            var sessionId = Convert.ToString(e.CommandArgument);

            if (string.IsNullOrWhiteSpace(userId)) { ShowFormInfo(false, "Please sign in again."); return; }
            if (string.IsNullOrWhiteSpace(eventId) || string.IsNullOrWhiteSpace(sessionId)) { ShowFormInfo(false, "Missing event or session."); return; }

            try
            {
                string msg; bool ok = false;

                if (string.Equals(e.CommandName, "enroll", StringComparison.OrdinalIgnoreCase))
                {
                    if (SessionOverlapsUser(eventId, userId, sessionId, out var conflict))
                    {
                        var sLocal = DateTime.SpecifyKind(conflict.StartUtc, DateTimeKind.Utc).ToLocalTime();
                        var eLocal = DateTime.SpecifyKind(conflict.EndUtc, DateTimeKind.Utc).ToLocalTime();
                        var returnQs = Request.Url.Query ?? "";
                        var replUrl = ResolveUrl($"~/Account/Participant/Replacements.aspx?eventId={Uri.EscapeDataString(eventId)}&sessionId={Uri.EscapeDataString(sessionId)}&return={HttpUtility.UrlEncode(returnQs)}");
                        ShowFormInfo(false, $"Time conflict with “{conflict.Title}” ({sLocal:ddd, MMM d • h:mm tt}–{eLocal:h:mm tt}). You can look for alternate times here: {replUrl}");
                        return;
                    }

                    ok = TryEnroll(eventId, sessionId, userId, out msg);
                    if (ok)
                    {
                        var returnQs = Request.Url.Query;
                        var url = ResolveUrl($"~/Account/Participant/EnrollSuccess.aspx?eventId={Uri.EscapeDataString(eventId)}&sessionId={Uri.EscapeDataString(sessionId)}&return={HttpUtility.UrlEncode(returnQs)}");
                        Response.Redirect(url, endResponse: false);
                        HttpContext.Current.ApplicationInstance.CompleteRequest();
                        return;
                    }
                    ShowFormInfo(false, msg);
                    return;
                }
                else if (string.Equals(e.CommandName, "waitlist", StringComparison.OrdinalIgnoreCase))
                {
                    ok = TryWaitlist(eventId, sessionId, userId, out msg);
                }
                else if (string.Equals(e.CommandName, "complete", StringComparison.OrdinalIgnoreCase))
                {
                    ok = TryMarkComplete(eventId, sessionId, userId, out msg);
                }
                else if (string.Equals(e.CommandName, "unenroll", StringComparison.OrdinalIgnoreCase))
                {
                    ok = TryUnenroll(eventId, sessionId, userId, out msg);
                }
                else
                {
                    msg = "Unknown action.";
                }

                ShowFormInfo(ok, msg);
            }
            catch (Exception ex)
            {
                ShowFormInfo(false, "Action failed: " + Server.HtmlEncode(ex.Message));
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(eventId))
                {
                    var filters = ReadFiltersFromQuery();
                    BindSessions(eventId, filters);
                    BindMySessions(eventId, userId);
                    // helper conversations are bound in initial load only; no change here
                }
            }
        }

        protected void ApplyFilters_Click(object sender, EventArgs e)
        {
            var f = new FilterState
            {
                From = ParseLocal(FilterFrom.Text),
                To = ParseLocal(FilterTo.Text),
                Query = (FilterQuery.Text ?? "").Trim()
            };
            foreach (ListItem li in FilterTags.Items) if (li.Selected) f.Tags.Add(li.Value);

            var qs = BuildFilterQueryString(f);
            var url = ResolveUrl("~/Account/Participant/Home.aspx");
            Response.Redirect(string.IsNullOrEmpty(qs) ? url : $"{url}?{qs}", endResponse: true);
        }

        protected void ClearFilters_Click(object sender, EventArgs e)
        {
            var url = ResolveUrl("~/Account/Participant/Home.aspx");
            Response.Redirect(url, endResponse: true);
        }

        private void ShowFormInfo(bool ok, string msg)
        {
            FormMessage.Text = ok
                ? $"<span style='color:#0a6b4f'>{Server.HtmlEncode(msg)}</span>"
                : $"<span style='color:#b00020'>{Server.HtmlEncode(msg)}</span>";
        }

        private XmlDocument LoadEnrollmentsDoc()
        {
            EnsureXmlDoc(EnrollmentsXmlPath, "enrollments");
            var doc = new XmlDocument();
            doc.Load(EnrollmentsXmlPath);
            return doc;
        }

        private void SaveEnrollmentsDoc(XmlDocument doc)
        {
            lock (EnrollmentSync.EnrollmentsLock)
            {
                doc.Save(EnrollmentsXmlPath);
            }
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

        protected void BtnLogout_Click(object sender, EventArgs e)
        {
            SessionHelper.SignOut(Context);
            Response.Redirect("~/Welcome_Page.aspx");
        }

        private int GetEnrolledCount(string eventId, string sessionId)
        {
            var doc = LoadEnrollmentsDoc();
            var node = doc.SelectNodes($"/enrollments/session[@eventId='{eventId}' and @id='{sessionId}']/enrolled/user");
            return node?.Count ?? 0;
        }

        private bool IsUserEnrolled(string eventId, string sessionId, string userId)
        {
            var doc = LoadEnrollmentsDoc();
            var node = doc.SelectSingleNode($"/enrollments/session[@eventId='{eventId}' and @id='{sessionId}']/enrolled/user[@id='{userId}']");
            return node != null;
        }

        private bool IsUserWaitlisted(string eventId, string sessionId, string userId)
        {
            var doc = LoadEnrollmentsDoc();
            var node = doc.SelectSingleNode($"/enrollments/session[@eventId='{eventId}' and @id='{sessionId}']/waitlist/user[@id='{userId}']");
            return node != null;
        }

        private int GetCapacity(string eventId, string sessionId)
        {
            if (!File.Exists(EventSessionsXmlPath)) return 0;
            var doc = new XmlDocument(); doc.Load(EventSessionsXmlPath);
            var s = (XmlElement)doc.SelectSingleNode($"/eventSessions/session[@eventId='{eventId}' and @id='{sessionId}']");
            if (s == null) return 0;
            return int.TryParse(s["capacity"]?.InnerText, out var v) ? v : 0;
        }

        private string GetCourseIdForSession(string eventId, string sessionId)
        {
            if (!File.Exists(EventSessionsXmlPath)) return null;
            var doc = new XmlDocument(); doc.Load(EventSessionsXmlPath);
            var s = (XmlElement)doc.SelectSingleNode($"/eventSessions/session[@eventId='{eventId}' and @id='{sessionId}']");
            return s?["courseId"]?.InnerText ?? null;
        }

        private bool TryEnroll(string eventId, string sessionId, string userId, out string message)
        {
            lock (EnrollmentSync.EnrollmentsLock)
            {
                var cap = GetCapacity(eventId, sessionId);
                var doc = LoadEnrollmentsDoc();
                var ses = EnsureSessionNode(doc, eventId, sessionId);

                if (ses.SelectSingleNode($"enrolled/user[@id='{userId}']") != null) { message = "You’re already enrolled in this session."; return true; }
                if (ses.SelectSingleNode($"waitlist/user[@id='{userId}']") != null) { message = "You’re already on the waitlist for this session."; return false; }

                if (SessionOverlapsUser(eventId, userId, sessionId, out var conflict))
                {
                    var sLocal = DateTime.SpecifyKind(conflict.StartUtc, DateTimeKind.Utc).ToLocalTime();
                    var eLocal = DateTime.SpecifyKind(conflict.EndUtc, DateTimeKind.Utc).ToLocalTime();
                    message = $"Time conflict with “{conflict.Title}” ({sLocal:ddd, MMM d • h:mm tt}–{eLocal:h:mm tt}).";
                    return false;
                }

                var currentEnrolled = ses.SelectNodes("enrolled/user")?.Count ?? 0;
                var remaining = Math.Max(0, cap - currentEnrolled);
                if (remaining <= 0) { message = "This session is full. You can join the waitlist instead."; return false; }

                var u = doc.CreateElement("user");
                u.SetAttribute("id", userId);
                u.SetAttribute("ts", DateTime.UtcNow.ToString("o"));
                ses.SelectSingleNode("enrolled").AppendChild(u);

                SaveEnrollmentsDoc(doc);

                // INSERT: audit log for Participant Enroll
                try
                {
                    var courseId = GetCourseIdForSession(eventId, sessionId);
                    var titles = LoadCourseTitles();
                    titles.TryGetValue(courseId ?? string.Empty, out var title);
                    var safeTitle = string.IsNullOrWhiteSpace(title) ? "(untitled microcourse)" : title;

                    UniversityAuditLogger.AppendForCurrentUser(
                        this,
                        "Participant Enroll",
                        $"Participant enrolled in session for \"{safeTitle}\" (eventId={eventId}, sessionId={sessionId})."
                    );
                }
                catch
                {
                    // Best-effort only
                }

                message = "Enrolled successfully!";
                return true;

            }
        }

        private bool TryWaitlist(string eventId, string sessionId, string userId, out string message)
        {
            lock (EnrollmentSync.EnrollmentsLock)
            {
                var cap = GetCapacity(eventId, sessionId);
                var doc = LoadEnrollmentsDoc();
                var ses = EnsureSessionNode(doc, eventId, sessionId);

                var currentEnrolled = ses.SelectNodes("enrolled/user")?.Count ?? 0;
                var remaining = Math.Max(0, cap - currentEnrolled);
                if (remaining > 0) { message = "Seats are still available; please enroll instead of joining the waitlist."; return false; }

                if (ses.SelectSingleNode($"enrolled/user[@id='{userId}']") != null) { message = "You’re already enrolled in this session."; return false; }
                if (ses.SelectSingleNode($"waitlist/user[@id='{userId}']") != null) { message = "You’re already on the waitlist for this session."; return false; }

                if (SessionOverlapsUser(eventId, userId, sessionId, out var conflict))
                {
                    var sLocal = DateTime.SpecifyKind(conflict.StartUtc, DateTimeKind.Utc).ToLocalTime();
                    var eLocal = DateTime.SpecifyKind(conflict.EndUtc, DateTimeKind.Utc).ToLocalTime();
                    message = $"Time conflict with “{conflict.Title}” ({sLocal:ddd, MMM d • h:mm tt}).";
                    return false;
                }

                // compute the position before appending
                var existingWaiters = ses.SelectNodes("waitlist/user")?.Count ?? 0;
                int newPosition = existingWaiters + 1;

                var u = doc.CreateElement("user");
                u.SetAttribute("id", userId);
                u.SetAttribute("ts", DateTime.UtcNow.ToString("o"));
                ses.SelectSingleNode("waitlist").AppendChild(u);

                SaveEnrollmentsDoc(doc);

                // INSERT: audit log for Participant Waitlist
                try
                {
                    var courseId = GetCourseIdForSession(eventId, sessionId);
                    var titles = LoadCourseTitles();
                    titles.TryGetValue(courseId ?? string.Empty, out var title);
                    var safeTitle = string.IsNullOrWhiteSpace(title) ? "(untitled microcourse)" : title;

                    UniversityAuditLogger.AppendForCurrentUser(
                        this,
                        "Participant Waitlist",
                        $"Participant joined the waitlist for \"{safeTitle}\" (eventId={eventId}, sessionId={sessionId}, position={newPosition})."
                    );
                }
                catch
                {
                }

                message = $"Added to waitlist. Your position is {newPosition}.";
                return true;

            }
        }

        private bool TryMarkComplete(string eventId, string sessionId, string userId, out string message)
        {
            lock (EnrollmentSync.EnrollmentsLock)
            {
                var doc = LoadEnrollmentsDoc();
                var ses = EnsureSessionNode(doc, eventId, sessionId);

                var enrolledNode = (XmlElement)ses.SelectSingleNode($"enrolled/user[@id='{userId}']");
                if (enrolledNode == null) { message = "You are not enrolled in this session."; return false; }

                var courseId = GetCourseIdForSession(eventId, sessionId);

                enrolledNode.ParentNode.RemoveChild(enrolledNode);

                var firstWait = (XmlElement)ses.SelectSingleNode("waitlist/user[1]");
                if (firstWait != null)
                {
                    var moved = doc.CreateElement("user");
                    moved.SetAttribute("id", firstWait.GetAttribute("id"));
                    moved.SetAttribute("ts", DateTime.UtcNow.ToString("o"));
                    ses.SelectSingleNode("enrolled").AppendChild(moved);
                    firstWait.ParentNode.RemoveChild(firstWait);
                }

                SaveEnrollmentsDoc(doc);

                if (!string.IsNullOrWhiteSpace(courseId))
                {
                    MarkCourseCompleted(userId, courseId);
                }

                // INSERT: audit log for Participant Completion
                try
                {
                    var titles = LoadCourseTitles();
                    titles.TryGetValue(courseId ?? string.Empty, out var title);
                    var safeTitle = string.IsNullOrWhiteSpace(title) ? "(untitled microcourse)" : title;

                    UniversityAuditLogger.AppendForCurrentUser(
                        this,
                        "Participant Completion",
                        $"Participant marked session complete for \"{safeTitle}\" (eventId={eventId}, sessionId={sessionId})."
                    );
                }
                catch
                {
                }

                message = "Marked complete. Your seat was freed (and the next waitlisted participant was enrolled). Prerequisite checks updated.";
                return true;

            }
        }

        // NEW: Unenroll handler for both enrolled and waitlisted users
        private bool TryUnenroll(string eventId, string sessionId, string userId, out string message)
        {
            lock (EnrollmentSync.EnrollmentsLock)
            {
                var doc = LoadEnrollmentsDoc();
                var ses = EnsureSessionNode(doc, eventId, sessionId);

                var enrolledNode = (XmlElement)ses.SelectSingleNode($"enrolled/user[@id='{userId}']");
                var waitNode = (XmlElement)ses.SelectSingleNode($"waitlist/user[@id='{userId}']");

                if (enrolledNode == null && waitNode == null)
                {
                    message = "You are not on this session.";
                    return false;
                }

                if (enrolledNode != null)
                {
                    // Remove from enrolled and promote next from waitlist (FIFO)
                    enrolledNode.ParentNode.RemoveChild(enrolledNode);

                    var firstWait = (XmlElement)ses.SelectSingleNode("waitlist/user[1]");
                    if (firstWait != null)
                    {
                        var moved = doc.CreateElement("user");
                        moved.SetAttribute("id", firstWait.GetAttribute("id"));
                        moved.SetAttribute("ts", DateTime.UtcNow.ToString("o"));
                        ses.SelectSingleNode("enrolled").AppendChild(moved);
                        firstWait.ParentNode.RemoveChild(firstWait);
                        SaveEnrollmentsDoc(doc);
                        message = "You have been unenrolled. The next person on the waitlist was enrolled.";
                        return true;
                    }

                    SaveEnrollmentsDoc(doc);

                    // INSERT: audit log for Participant Unenroll (enrolled branch)
                    try
                    {
                        var courseId = GetCourseIdForSession(eventId, sessionId);
                        var titles = LoadCourseTitles();
                        titles.TryGetValue(courseId ?? string.Empty, out var title);
                        var safeTitle = string.IsNullOrWhiteSpace(title) ? "(untitled microcourse)" : title;

                        UniversityAuditLogger.AppendForCurrentUser(
                            this,
                            "Participant Unenroll",
                            $"Participant unenrolled from session for \"{safeTitle}\" (eventId={eventId}, sessionId={sessionId})."
                        );
                    }
                    catch
                    {
                    }

                    message = "You have been unenrolled.";
                    return true;

                }
                else
                {
                    waitNode.ParentNode.RemoveChild(waitNode);
                    SaveEnrollmentsDoc(doc);

                    // INSERT: audit log for Participant Unenroll (waitlist branch)
                    try
                    {
                        var courseId = GetCourseIdForSession(eventId, sessionId);
                        var titles = LoadCourseTitles();
                        titles.TryGetValue(courseId ?? string.Empty, out var title);
                        var safeTitle = string.IsNullOrWhiteSpace(title) ? "(untitled microcourse)" : title;

                        UniversityAuditLogger.AppendForCurrentUser(
                            this,
                            "Participant Unenroll",
                            $"Participant removed themselves from the waitlist for \"{safeTitle}\" (eventId={eventId}, sessionId={sessionId})."
                        );
                    }
                    catch
                    {
                    }

                    message = "Removed from the waitlist.";
                    return true;

                }
            }
        }

        // NEW: navigate to helper message page
        protected void StartHelperMessageBtn_Click(object sender, EventArgs e)
        {
            var url = ResolveUrl("~/Account/Participant/HelperMessage.aspx");
            Response.Redirect(url, endResponse: true);
        }
    }
}






