using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml;
using CyberApp_FIA.Services;

namespace CyberApp_FIA.Account
{
    public partial class EventSessionEdit : SecurePage
    {
        private string EventsXmlPath => Server.MapPath("~/App_Data/events.xml");
        private string EventSessionsXmlPath => Server.MapPath("~/App_Data/eventSessions.xml");
        private string MicrocoursesXmlPath => Server.MapPath("~/App_Data/microcourses.xml");
        private string UsersXmlPath => Server.MapPath("~/App_Data/users.xml");
        private string HelperProgressXmlPath => Server.MapPath("~/App_Data/helperProgress.xml");
        private string EnrollmentsXmlPath => Server.MapPath("~/App_Data/enrollments.xml");

        private string _eventId;
        private string _sessionId;

        private sealed class HelperRow
        {
            public string HelperId { get; set; }
            public string Name { get; set; }
            public bool IsEligible { get; set; }
            public bool IsCertified { get; set; }
            public bool HasOverlap { get; set; }
            public bool IsCurrent { get; set; }   // current helper on initial view
            public string CertLabel { get; set; }
            public string CertCssClass { get; set; }

            public DateTime? LastDeliveredUtc { get; set; }
            public int DeliveredCount { get; set; }
        }

        private sealed class ImpactRow
        {
            public string Email { get; set; }
            public string Status { get; set; }       // Enrolled / Waitlisted
            public bool HasConflict { get; set; }    // true if overlap with another session
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            var role = (string)Session["Role"];
            if (!string.Equals(role, "UniversityAdmin", StringComparison.OrdinalIgnoreCase))
            {
                Response.Redirect("~/Account/Login.aspx");
                return;
            }

            _eventId = Request.QueryString["eventId"] ?? "";
            _sessionId = Request.QueryString["sessionId"] ?? "";

            if (string.IsNullOrEmpty(_eventId) || string.IsNullOrEmpty(_sessionId))
            {
                Response.Redirect("~/Account/UniversityAdmin/UniversityAdminHome.aspx");
                return;
            }

            if (!IsPostBack)
            {
                LoadHeader();

                // Load session fields and then bind helpers with "current helper" flag
                string currentHelperName;
                LoadSessionFields(out string courseId, out DateTime? start, out DateTime? end,
                                  out string room, out int cap, out currentHelperName);

                BindCourses(courseId);

                SessionDateTimeStart.Text = start.HasValue
                    ? start.Value.ToLocalTime().ToString("yyyy-MM-ddTHH:mm")
                    : string.Empty;
                SessionDateTimeEnd.Text = end.HasValue
                    ? end.Value.ToLocalTime().ToString("yyyy-MM-ddTHH:mm")
                    : string.Empty;
                Room.Text = room ?? string.Empty;
                Capacity.Text = cap > 0 ? cap.ToString() : string.Empty;

                // Initial helper list: mark current helper as "Current helper"
                BindHelpersList(initialCurrentHelperName: currentHelperName);

                // Initial impact summary
                BindImpactSummary();
            }
        }

        private void LoadHeader()
        {
            if (!File.Exists(EventsXmlPath)) return;

            var doc = new XmlDocument();
            doc.Load(EventsXmlPath);
            var ev = (XmlElement)doc.SelectSingleNode($"/events/event[@id='{_eventId}']");
            if (ev == null)
            {
                Response.Redirect("~/Account/UniversityAdmin/EventManage.aspx?id=" + _eventId);
                return;
            }

            EventName.Text = ev["name"]?.InnerText ?? "(unnamed)";
            University.Text = ev["university"]?.InnerText ?? "";
            var date = ev["date"]?.InnerText ?? "";
            if (DateTime.TryParse(date, out var dt))
                EventDate.Text = dt.ToLocalTime().ToString("yyyy-MM-dd");
            else
                EventDate.Text = "(unset)";
        }

        private void LoadSessionFields(out string courseId, out DateTime? startUtc,
                                       out DateTime? endUtc, out string room,
                                       out int capacity, out string helperName)
        {
            courseId = "";
            startUtc = null;
            endUtc = null;
            room = "";
            capacity = 0;
            helperName = "";

            if (!File.Exists(EventSessionsXmlPath)) return;

            var doc = new XmlDocument();
            doc.Load(EventSessionsXmlPath);
            var s = (XmlElement)doc.SelectSingleNode(
                $"/eventSessions/session[@eventId='{_eventId}' and @id='{_sessionId}']");
            if (s == null)
            {
                Response.Redirect("~/Account/UniversityAdmin/EventManage.aspx?id=" + _eventId);
                return;
            }

            courseId = s["courseId"]?.InnerText ?? "";
            var startIso = s["start"]?.InnerText ?? "";
            var endIso = s["end"]?.InnerText ?? "";
            room = s["room"]?.InnerText ?? "";
            helperName = (s["helper"]?.InnerText ?? "").Trim();
            var capText = s["capacity"]?.InnerText ?? "";
            int.TryParse(capText, out capacity);

            if (TryParseIsoUtc(startIso, out var start))
                startUtc = start;
            if (TryParseIsoUtc(endIso, out var end))
                endUtc = end;
        }

        private void BindCourses(string selectedCourseId)
        {
            CourseSelect.Items.Clear();
            if (!File.Exists(MicrocoursesXmlPath)) return;

            var doc = new XmlDocument();
            doc.Load(MicrocoursesXmlPath);
            var nodes = doc.SelectNodes("/microcourses/course[@status='Published']");
            foreach (XmlElement c in nodes)
            {
                var id = c.GetAttribute("id");
                var title = c["title"]?.InnerText ?? "(untitled)";
                var li = new ListItem(title, id);
                CourseSelect.Items.Add(li);

                if (!string.IsNullOrEmpty(selectedCourseId) &&
                    id.Equals(selectedCourseId, StringComparison.OrdinalIgnoreCase))
                {
                    CourseSelect.ClearSelection();
                    li.Selected = true;
                }
            }
        }

        // ===================== Save / Cancel =====================

        protected void BtnSave_Click(object sender, EventArgs e)
        {
            EditMessage.Text = "";

            var courseId = CourseSelect.SelectedValue;
            if (string.IsNullOrWhiteSpace(courseId))
            {
                EditMessage.Text = "<span class='err'>Pick a course.</span>";
                BindImpactSummary();
                return;
            }

            if (!TryParseLocalToUtc(SessionDateTimeStart.Text, out var startUtc) ||
                !TryParseLocalToUtc(SessionDateTimeEnd.Text, out var endUtc))
            {
                EditMessage.Text = "<span class='err'>Enter valid start and end times.</span>";
                BindHelpersList(); // refresh without "current helper" behavior
                BindImpactSummary();
                return;
            }
            if (endUtc <= startUtc)
            {
                EditMessage.Text = "<span class='err'>End time must be after start.</span>";
                BindHelpersList();
                BindImpactSummary();
                return;
            }

            var helper = (HelperSelect.SelectedValue ?? "").Trim();
            if (string.IsNullOrWhiteSpace(helper))
            {
                EditMessage.Text = "<span class='err'>Pick a certified or eligible helper from the list.</span>";
                BindHelpersList();
                BindImpactSummary();
                return;
            }

            var room = (Room.Text ?? "").Trim();
            int.TryParse(Capacity.Text, out var cap);
            if (cap < 0) cap = 0;

            if (!File.Exists(EventSessionsXmlPath))
            {
                EditMessage.Text = "<span class='err'>Session store not found.</span>";
                BindImpactSummary();
                return;
            }

            var doc = new XmlDocument();
            doc.Load(EventSessionsXmlPath);
            var s = (XmlElement)doc.SelectSingleNode(
                $"/eventSessions/session[@eventId='{_eventId}' and @id='{_sessionId}']");
            if (s == null)
            {
                EditMessage.Text = "<span class='err'>Session not found.</span>";
                BindImpactSummary();
                return;
            }

            // Overlap check: same helper, same event, excluding THIS session
            var existing = doc.SelectNodes(
                $"/eventSessions/session[@eventId='{_eventId}']");
            foreach (XmlElement other in existing)
            {
                var otherId = other.GetAttribute("id");
                if (otherId == _sessionId) continue; // ignore self

                var sHelper = (other["helper"]?.InnerText ?? "").Trim();
                if (!string.Equals(helper, sHelper, StringComparison.OrdinalIgnoreCase)) continue;

                var sStartIso = other["start"]?.InnerText ?? "";
                var sEndIso = other["end"]?.InnerText ?? "";
                if (!TryParseIsoUtc(sStartIso, out var sStartUtc) ||
                    !TryParseIsoUtc(sEndIso, out var sEndUtc))
                    continue;

                if (IntervalsOverlap(startUtc, endUtc, sStartUtc, sEndUtc))
                {
                    EditMessage.Text =
                        $"<span class='err'>Helper <strong>{Server.HtmlEncode(helper)}</strong> is already booked " +
                        $"from {sStartUtc.ToLocalTime():yyyy-MM-dd HH:mm} to {sEndUtc.ToLocalTime():HH:mm}.</span>";
                    BindHelpersList(); // standard view (no "current helper" override now)
                    BindImpactSummary();
                    return;
                }
            }

            // Capture old values for audit
            var oldCourseId = s["courseId"]?.InnerText ?? "";
            var oldStartIso = s["start"]?.InnerText ?? "";
            var oldEndIso = s["end"]?.InnerText ?? "";
            var oldRoom = s["room"]?.InnerText ?? "";
            var oldHelper = (s["helper"]?.InnerText ?? "").Trim();
            var oldCapacity = s["capacity"]?.InnerText ?? "";

            // Update XML
            SetOrCreateElement(s, "courseId", courseId);
            SetOrCreateElement(s, "start", startUtc.ToString("o"));
            SetOrCreateElement(s, "end", endUtc.ToString("o"));
            SetOrCreateElement(s, "room", room);
            SetOrCreateElement(s, "helper", helper);
            SetOrCreateElement(s, "capacity", cap > 0 ? cap.ToString() : "");

            // --- NEW: record a time-change payload for participants if start/end changed ---
            bool timesChanged =
                !string.IsNullOrWhiteSpace(oldStartIso) &&
                !string.IsNullOrWhiteSpace(oldEndIso) &&
                (
                    !string.Equals(oldStartIso, startUtc.ToString("o"), StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(oldEndIso, endUtc.ToString("o"), StringComparison.OrdinalIgnoreCase)
                );

            if (timesChanged)
            {
                var timeChange = s["timeChange"] as XmlElement;
                if (timeChange == null)
                {
                    timeChange = doc.CreateElement("timeChange");
                    s.AppendChild(timeChange);
                }

                SetOrCreateElement(timeChange, "changedOn", DateTime.UtcNow.ToString("o"));
                SetOrCreateElement(timeChange, "oldStart", oldStartIso);
                SetOrCreateElement(timeChange, "oldEnd", oldEndIso);
                SetOrCreateElement(timeChange, "newStart", startUtc.ToString("o"));
                SetOrCreateElement(timeChange, "newEnd", endUtc.ToString("o"));
            }

            // Mark as edited
            s.SetAttribute("edited", "true");
            SetOrCreateElement(s, "editedAt", DateTime.UtcNow.ToString("o"));

            doc.Save(EventSessionsXmlPath);


            // AUDIT: log edit
            try
            {
                var details =
                    $"Session edited for eventId={_eventId}, sessionId={_sessionId}. " +
                    $"Course: {oldCourseId} → {courseId}; " +
                    $"Start: {oldStartIso} → {startUtc:o}; " +
                    $"End: {oldEndIso} → {endUtc:o}; " +
                    $"Room: {oldRoom} → {room}; " +
                    $"Helper: {oldHelper} → {helper}; " +
                    $"Capacity: {oldCapacity} → {(cap > 0 ? cap.ToString() : "")}.";
                UniversityAuditLogger.AppendForCurrentUser(
                    this,
                    "Session Updated",
                    details);
            }
            catch
            {
                // best-effort
            }

            EditMessage.Text = "<span class='ok'>Session updated.</span>";
            Response.Redirect("~/Account/UniversityAdmin/EventManage.aspx?id=" + _eventId);
        }

        protected void BtnCancel_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/Account/UniversityAdmin/EventManage.aspx?id=" + _eventId);
        }

        // ===================== Impact summary =====================

        private void BindImpactSummary()
        {
            var rows = new List<ImpactRow>();
            NoParticipantsPH.Visible = false;

            if (!File.Exists(EnrollmentsXmlPath) || !File.Exists(UsersXmlPath))
            {
                ImpactRepeater.DataSource = rows;
                ImpactRepeater.DataBind();
                NoParticipantsPH.Visible = true;
                return;
            }

            var enrollDoc = new XmlDocument();
            enrollDoc.Load(EnrollmentsXmlPath);

            var sessNode = enrollDoc.SelectSingleNode(
                $"/enrollments/session[@eventId='{_eventId}' and @id='{_sessionId}']") as XmlElement;
            if (sessNode == null)
            {
                ImpactRepeater.DataSource = rows;
                ImpactRepeater.DataBind();
                NoParticipantsPH.Visible = true;
                return;
            }

            var participantsDict = new Dictionary<string, ImpactRow>();

            void AddUsers(XmlElement container, string statusLabel)
            {
                if (container == null) return;
                foreach (XmlElement u in container.SelectNodes("user"))
                {
                    var uid = u.GetAttribute("id");
                    if (string.IsNullOrEmpty(uid)) continue;

                    if (!participantsDict.TryGetValue(uid, out var row))
                    {
                        var email = LookupEmailById(uid);
                        if (string.IsNullOrWhiteSpace(email))
                            email = uid;

                        row = new ImpactRow
                        {
                            Email = email,
                            Status = statusLabel,
                            HasConflict = false
                        };
                        participantsDict[uid] = row;
                    }
                    else
                    {
                        // Prefer "Enrolled" if user is both enrolled and waitlisted.
                        if (statusLabel == "Enrolled" && row.Status != "Enrolled")
                            row.Status = "Enrolled";
                    }
                }
            }

            AddUsers(sessNode["enrolled"] as XmlElement, "Enrolled");
            AddUsers(sessNode["waitlist"] as XmlElement, "Waitlisted");

            if (participantsDict.Count == 0)
            {
                ImpactRepeater.DataSource = rows;
                ImpactRepeater.DataBind();
                NoParticipantsPH.Visible = true;
                return;
            }

            // Default: no conflict
            foreach (var kv in participantsDict)
                rows.Add(kv.Value);

            // FIX: initialize to avoid CS0165
            DateTime newStartUtc = default, newEndUtc = default;

            bool haveNewWindow =
                TryParseLocalToUtc(SessionDateTimeStart.Text, out newStartUtc) &&
                TryParseLocalToUtc(SessionDateTimeEnd.Text, out newEndUtc) &&
                newEndUtc > newStartUtc;

            if (haveNewWindow && File.Exists(EventSessionsXmlPath))
            {
                var sessionsDoc = new XmlDocument();
                sessionsDoc.Load(EventSessionsXmlPath);

                foreach (var kv in participantsDict)
                {
                    var userId = kv.Key;
                    var row = kv.Value;

                    var otherSessions = enrollDoc.SelectNodes(
                        $"/enrollments/session[enrolled/user[@id='{userId}'] or waitlist/user[@id='{userId}']]");
                    if (otherSessions == null) continue;

                    foreach (XmlElement otherSess in otherSessions)
                    {
                        var otherEventId = otherSess.GetAttribute("eventId");
                        var otherSessionId = otherSess.GetAttribute("id");

                        // Skip the session we're currently editing
                        if (otherEventId == _eventId && otherSessionId == _sessionId)
                            continue;

                        var sess = sessionsDoc.SelectSingleNode(
                            $"/eventSessions/session[@eventId='{otherEventId}' and @id='{otherSessionId}']") as XmlElement;
                        if (sess == null) continue;

                        var sStartIso = sess["start"]?.InnerText ?? "";
                        var sEndIso = sess["end"]?.InnerText ?? "";
                        if (!TryParseIsoUtc(sStartIso, out var sStartUtc) ||
                            !TryParseIsoUtc(sEndIso, out var sEndUtc))
                            continue;

                        if (IntervalsOverlap(newStartUtc, newEndUtc, sStartUtc, sEndUtc))
                        {
                            row.HasConflict = true;
                            break;
                        }
                    }
                }
            }

            ImpactRepeater.DataSource = rows;
            ImpactRepeater.DataBind();
            NoParticipantsPH.Visible = rows.Count == 0;
        }

        protected void BtnDelete_Click(object sender, EventArgs e)
        {
            EditMessage.Text = "";

            // Ensure we have ids (Page_Load already sets these from query string)
            if (string.IsNullOrEmpty(_eventId) || string.IsNullOrEmpty(_sessionId))
            {
                EditMessage.Text = "<span class='err'>Missing event or session id.</span>";
                return;
            }

            // Load the session from eventSessions.xml
            if (!File.Exists(EventSessionsXmlPath))
            {
                EditMessage.Text = "<span class='err'>Session store not found.</span>";
                return;
            }

            var sessionsDoc = new XmlDocument();
            sessionsDoc.Load(EventSessionsXmlPath);

            var sessionNode = sessionsDoc.SelectSingleNode(
                $"/eventSessions/session[@eventId='{_eventId}' and @id='{_sessionId}']") as XmlElement;
            if (sessionNode == null)
            {
                EditMessage.Text = "<span class='err'>Session not found.</span>";
                return;
            }

            // Capture course + start time for participant notices
            var courseId = sessionNode["courseId"]?.InnerText ?? string.Empty;
            var startIso = sessionNode["start"]?.InnerText ?? string.Empty;

            string microTitle = GetCourseTitleById(courseId) ?? "(untitled microcourse)";
            string startUtcIso = null;
            if (TryParseIsoUtc(startIso, out var startUtc))
            {
                startUtcIso = startUtc.ToString("o");
            }

            // Collect impacted participant ids from enrollments.xml
            var impactedIds = new List<string>();

            if (File.Exists(EnrollmentsXmlPath))
            {
                var enrollDoc = new XmlDocument();
                enrollDoc.Load(EnrollmentsXmlPath);

                var sessEnroll = enrollDoc.SelectSingleNode(
                    $"/enrollments/session[@eventId='{_eventId}' and @id='{_sessionId}']") as XmlElement;

                if (sessEnroll != null)
                {
                    foreach (XmlElement u in sessEnroll.SelectNodes("enrolled/user"))
                    {
                        var id = u.GetAttribute("id");
                        if (!string.IsNullOrEmpty(id) && !impactedIds.Contains(id))
                            impactedIds.Add(id);
                    }

                    foreach (XmlElement u in sessEnroll.SelectNodes("waitlist/user"))
                    {
                        var id = u.GetAttribute("id");
                        if (!string.IsNullOrEmpty(id) && !impactedIds.Contains(id))
                            impactedIds.Add(id);
                    }

                    // Remove the entire enrollments session node
                    sessEnroll.ParentNode.RemoveChild(sessEnroll);
                    enrollDoc.Save(EnrollmentsXmlPath);
                }
            }

            // For each impacted participant, write a sessionDeletionNotice into users.xml
            if (impactedIds.Count > 0 && File.Exists(UsersXmlPath) && !string.IsNullOrEmpty(startUtcIso))
            {
                var usersDoc = new XmlDocument();
                usersDoc.Load(UsersXmlPath);

                foreach (var pid in impactedIds)
                {
                    var userNode = usersDoc.SelectSingleNode($"/users/user[@id='{pid}']") as XmlElement;
                    if (userNode == null) continue;

                    var notice = userNode["sessionDeletionNotice"] as XmlElement;
                    if (notice == null)
                    {
                        notice = usersDoc.CreateElement("sessionDeletionNotice");
                        userNode.AppendChild(notice);
                    }

                    notice.SetAttribute("microcourseTitle", microTitle);
                    notice.SetAttribute("startUtc", startUtcIso);
                    notice.SetAttribute("eventId", _eventId);
                    notice.SetAttribute("sessionId", _sessionId);
                    notice.SetAttribute("createdOn", DateTime.UtcNow.ToString("o"));
                }

                usersDoc.Save(UsersXmlPath);
            }

            // Remove the session itself from eventSessions.xml
            sessionNode.ParentNode.RemoveChild(sessionNode);
            sessionsDoc.Save(EventSessionsXmlPath);

            // Audit log: Session Deleted
            try
            {
                var details =
                    $"Session deleted for eventId={_eventId}, sessionId={_sessionId}, " +
                    $"course=\"{microTitle}\", start={startIso}.";
                UniversityAuditLogger.AppendForCurrentUser(
                    this,
                    "Session Deleted",
                    details);
            }
            catch
            {
                // best-effort only
            }

            // Redirect back to event manage page
            Response.Redirect("~/Account/UniversityAdmin/EventManage.aspx?id=" + _eventId);
        }


        // Helper: resolve a course title from microcourses.xml
        private string GetCourseTitleById(string courseId)
        {
            if (string.IsNullOrWhiteSpace(courseId) || !File.Exists(MicrocoursesXmlPath))
                return null;

            var doc = new XmlDocument();
            doc.Load(MicrocoursesXmlPath);

            var node = doc.SelectSingleNode($"/microcourses/course[@id='{courseId}']") as XmlElement;
            if (node == null) return null;

            return node["title"]?.InnerText ?? null;
        }

        private string LookupEmailById(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId) || !File.Exists(UsersXmlPath)) return "";
                var doc = new XmlDocument();
                doc.Load(UsersXmlPath);
                var node = doc.SelectSingleNode($"/users/user[@id='{userId}']") as XmlElement;
                if (node == null) return "";
                return node["email"]?.InnerText ?? "";
            }
            catch
            {
                return "";
            }
        }

        // ===================== Helpers list / filters =====================

        private void BindHelpersList(string initialCurrentHelperName = null)
        {
            var rows = new List<HelperRow>();

            var universityName = (University.Text ?? "").Trim();

            if (!File.Exists(UsersXmlPath))
            {
                NoHelpersPH.Visible = true;
                HelpersRepeater.DataSource = rows;
                HelpersRepeater.DataBind();
                HelperSelect.Items.Clear();
                HelperSelect.Items.Add(new ListItem("-- Select helper --", ""));
                return;
            }

            string courseId = CourseSelect.SelectedValue;
            bool hasCourse = !string.IsNullOrEmpty(courseId);

            DateTime startUtc = default, endUtc = default;
            bool hasTimeWindow =
                TryParseLocalToUtc(SessionDateTimeStart.Text, out startUtc) &&
                TryParseLocalToUtc(SessionDateTimeEnd.Text, out endUtc) &&
                endUtc > startUtc;

            XmlDocument progressDoc = null;
            if (File.Exists(HelperProgressXmlPath))
            {
                progressDoc = new XmlDocument();
                progressDoc.Load(HelperProgressXmlPath);
            }

            XmlDocument sessionsDoc = null;
            XmlNodeList sessionNodes = null;
            if (File.Exists(EventSessionsXmlPath))
            {
                sessionsDoc = new XmlDocument();
                sessionsDoc.Load(EventSessionsXmlPath);
                sessionNodes = sessionsDoc.SelectNodes("/eventSessions/session");
            }

            var usersDoc = new XmlDocument();
            usersDoc.Load(UsersXmlPath);

            var helperNodes = usersDoc.SelectNodes("/users/user | /accounts/user");
            foreach (XmlElement u in helperNodes)
            {
                var roleText = u["role"]?.InnerText ?? u.GetAttribute("role");
                if (!roleText.Equals("Helper", StringComparison.OrdinalIgnoreCase)) continue;

                var helperUniversity = u["university"]?.InnerText ?? u.GetAttribute("university") ?? "";
                if (!string.IsNullOrWhiteSpace(universityName) && !string.IsNullOrWhiteSpace(helperUniversity))
                {
                    if (!string.Equals(universityName.Trim(), helperUniversity.Trim(), StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                var id = u["id"]?.InnerText ?? u.GetAttribute("id");
                var name = u["displayName"]?.InnerText;
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = u["email"]?.InnerText ?? "(Helper)";
                }

                bool isCert = false;
                bool isElig = false;

                if (progressDoc != null && hasCourse && !string.IsNullOrEmpty(id))
                {
                    var courseNode = progressDoc.SelectSingleNode(
                        $"/helperProgress/helper[@id='{id}']/course[@id='{courseId}']"
                    ) as XmlElement;

                    if (courseNode != null)
                    {
                        var certText = courseNode["isCertified"]?.InnerText ?? "false";
                        var eligText = courseNode["isEligible"]?.InnerText ?? "";

                        isCert = certText.Equals("true", StringComparison.OrdinalIgnoreCase);

                        if (!string.IsNullOrEmpty(eligText))
                            isElig = eligText.Equals("true", StringComparison.OrdinalIgnoreCase);
                        else
                            isElig = isCert;
                    }
                }

                string certLabel;
                string certCss;

                if (isCert)
                {
                    certLabel = "Certified";
                    certCss = "status-cert";
                }
                else if (isElig)
                {
                    certLabel = "Eligible";
                    certCss = "status-eligible";
                }
                else
                {
                    certLabel = "Not certified";
                    certCss = "status-notcert";
                }

                DateTime? lastDelivered = null;
                int deliveredCount = 0;

                if (sessionNodes != null && hasCourse)
                {
                    foreach (XmlElement s in sessionNodes)
                    {
                        var sCourseId = s["courseId"]?.InnerText ?? "";
                        if (!string.Equals(sCourseId, courseId, StringComparison.OrdinalIgnoreCase))
                            continue;

                        var hName = (s["helper"]?.InnerText ?? "").Trim();
                        if (!string.Equals(hName, name.Trim(), StringComparison.OrdinalIgnoreCase))
                            continue;

                        var sStartIso = s["start"]?.InnerText ?? "";
                        if (!TryParseIsoUtc(sStartIso, out var sStartUtc))
                            continue;

                        deliveredCount++;
                        if (!lastDelivered.HasValue || sStartUtc > lastDelivered.Value)
                        {
                            lastDelivered = sStartUtc;
                        }
                    }
                }

                bool hasOverlap = false;
                if (hasTimeWindow && sessionNodes != null)
                {
                    foreach (XmlElement s in sessionNodes)
                    {
                        var sessEventId = s.GetAttribute("eventId");
                        var sessId = s.GetAttribute("id");
                        if (!string.Equals(sessEventId, _eventId, StringComparison.OrdinalIgnoreCase))
                            continue;

                        var hName = (s["helper"]?.InnerText ?? "").Trim();
                        if (!string.Equals(hName, name.Trim(), StringComparison.OrdinalIgnoreCase))
                            continue;

                        var sStartIso = s["start"]?.InnerText ?? "";
                        var sEndIso = s["end"]?.InnerText ?? "";
                        if (!TryParseIsoUtc(sStartIso, out var sStartUtc) ||
                            !TryParseIsoUtc(sEndIso, out var sEndUtc))
                            continue;

                        // For edit screen, respect overlaps with other sessions (skip current).
                        if (sessId == _sessionId) continue;

                        if (IntervalsOverlap(startUtc, endUtc, sStartUtc, sEndUtc))
                        {
                            hasOverlap = true;
                            break;
                        }
                    }
                }

                bool isCurrent = !string.IsNullOrWhiteSpace(initialCurrentHelperName) &&
                                 string.Equals(name.Trim(), initialCurrentHelperName.Trim(), StringComparison.OrdinalIgnoreCase);

                rows.Add(new HelperRow
                {
                    HelperId = id,
                    Name = name,
                    IsCertified = isCert,
                    IsEligible = isElig,
                    HasOverlap = hasOverlap,
                    IsCurrent = isCurrent,
                    CertLabel = certLabel,
                    CertCssClass = certCss,
                    LastDeliveredUtc = lastDelivered,
                    DeliveredCount = deliveredCount
                });
            }

            // Base: eligible + certified
            var tableRows = new List<HelperRow>();
            foreach (var r in rows)
            {
                if (r.IsCertified || r.IsEligible)
                    tableRows.Add(r);
            }

            bool sortLast = SortByLastDelivered.Checked;
            bool sortMost = SortByMostDelivered.Checked;
            bool filterEligible = FilterEligible.Checked;
            bool filterCertified = FilterCertified.Checked;

            if (sortLast || sortMost)
            {
                var sorted = new List<HelperRow>();
                foreach (var r in tableRows)
                {
                    if (r.IsCertified)
                        sorted.Add(r);
                }

                if (sortLast)
                {
                    sorted.Sort((a, b) =>
                    {
                        if (a.LastDeliveredUtc.HasValue && b.LastDeliveredUtc.HasValue)
                        {
                            return DateTime.Compare(b.LastDeliveredUtc.Value, a.LastDeliveredUtc.Value);
                        }
                        if (a.LastDeliveredUtc.HasValue) return -1;
                        if (b.LastDeliveredUtc.HasValue) return 1;
                        return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                    });
                }
                else if (sortMost)
                {
                    sorted.Sort((a, b) =>
                    {
                        int cmp = b.DeliveredCount.CompareTo(a.DeliveredCount);
                        if (cmp != 0) return cmp;
                        return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                    });
                }

                tableRows = sorted;
            }
            else
            {
                if (filterCertified && !filterEligible)
                {
                    var filtered = new List<HelperRow>();
                    foreach (var r in tableRows)
                    {
                        if (r.IsCertified) filtered.Add(r);
                    }
                    tableRows = filtered;
                }
                else if (filterEligible && !filterCertified)
                {
                    var filtered = new List<HelperRow>();
                    foreach (var r in tableRows)
                    {
                        if (r.IsEligible) filtered.Add(r);
                    }
                    tableRows = filtered;
                }

                tableRows.Sort((a, b) =>
                    string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            }

            NoHelpersPH.Visible = tableRows.Count == 0;
            HelpersRepeater.DataSource = tableRows;
            HelpersRepeater.DataBind();

            // Dropdown mirrors the currently displayed helpers.
            var previousSelection = HelperSelect.SelectedValue;

            HelperSelect.Items.Clear();
            HelperSelect.Items.Add(new ListItem("-- Select helper --", ""));

            foreach (var r in tableRows)
            {
                if (HelperSelect.Items.FindByValue(r.Name) == null)
                {
                    HelperSelect.Items.Add(new ListItem(r.Name, r.Name));
                }
            }

            // Initial selection: if nothing selected yet, try current helper
            if (string.IsNullOrEmpty(previousSelection) && !string.IsNullOrWhiteSpace(initialCurrentHelperName))
            {
                previousSelection = initialCurrentHelperName;
            }

            if (!string.IsNullOrEmpty(previousSelection))
            {
                var existing = HelperSelect.Items.FindByValue(previousSelection);
                if (existing != null)
                {
                    HelperSelect.ClearSelection();
                    existing.Selected = true;
                }
            }
        }

        protected void CourseSelect_SelectedIndexChanged(object sender, EventArgs e)
        {
            BindHelpersList(); // standard view (no current helper override)
            BindImpactSummary();
        }

        protected void SessionTime_TextChanged(object sender, EventArgs e)
        {
            BindHelpersList();
            BindImpactSummary();
        }

        protected void HelperFilterChanged(object sender, EventArgs e)
        {
            BindHelpersList();
        }

        protected void HelperSortChanged(object sender, EventArgs e)
        {
            var cb = sender as CheckBox;
            if (cb == SortByLastDelivered && SortByLastDelivered.Checked)
            {
                SortByMostDelivered.Checked = false;
            }
            else if (cb == SortByMostDelivered && SortByMostDelivered.Checked)
            {
                SortByLastDelivered.Checked = false;
            }

            BindHelpersList();
        }

        protected void BtnClearHelperFilters_Click(object sender, EventArgs e)
        {
            FilterEligible.Checked = false;
            FilterCertified.Checked = false;
            BindHelpersList();
        }

        // ===================== Helpers =====================

        private static void SetOrCreateElement(XmlElement parent, string name, string value)
        {
            var doc = parent.OwnerDocument;
            var node = parent[name];
            if (node == null)
            {
                node = doc.CreateElement(name);
                parent.AppendChild(node);
            }
            node.InnerText = value ?? string.Empty;
        }

        private static bool TryParseLocalToUtc(string input, out DateTime utc)
        {
            utc = default;
            if (DateTime.TryParse(input, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out var local) ||
                DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out local))
            {
                utc = local.ToUniversalTime();
                return true;
            }
            return false;
        }

        private static bool TryParseIsoUtc(string iso, out DateTime utc)
        {
            return DateTime.TryParseExact(
                iso,
                "o",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                out utc);
        }

        private static bool IntervalsOverlap(DateTime aStartUtc, DateTime aEndUtc, DateTime bStartUtc, DateTime bEndUtc)
            => aStartUtc < bEndUtc && bStartUtc < aEndUtc;
    }
}

