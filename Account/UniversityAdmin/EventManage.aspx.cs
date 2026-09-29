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
    public partial class EventManage : SecurePage
    {
        private string EventsXmlPath => Server.MapPath("~/App_Data/events.xml");
        private string MicrocoursesXmlPath => Server.MapPath("~/App_Data/microcourses.xml");
        private string EventCoursesXmlPath => Server.MapPath("~/App_Data/eventCourses.xml");
        private string EventSessionsXmlPath => Server.MapPath("~/App_Data/eventSessions.xml");
        private string UsersXmlPath => Server.MapPath("~/App_Data/users.xml");
        private string HelperProgressXmlPath => Server.MapPath("~/App_Data/helperProgress.xml");

        private string _eventId;

        private sealed class HelperRow
        {
            public string HelperId { get; set; }
            public string Name { get; set; }
            public bool IsEligible { get; set; }
            public bool IsCertified { get; set; }
            public bool HasOverlap { get; set; }
            public string CertLabel { get; set; }
            public string CertCssClass { get; set; }

            public DateTime? LastDeliveredUtc { get; set; }
            public int DeliveredCount { get; set; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            var role = (string)Session["Role"];
            if (!string.Equals(role, "UniversityAdmin", StringComparison.OrdinalIgnoreCase))
            {
                Response.Redirect("~/Account/Login.aspx");
                return;
            }

            _eventId = Request.QueryString["id"] ?? "";
            if (string.IsNullOrEmpty(_eventId))
            {
                Response.Redirect("~/Account/UniversityAdmin/UniversityAdminHome.aspx");
                return;
            }

            if (!IsPostBack)
            {
                LoadEventHeader();
                BindMicrocourses();
                BindCourseSelect();
                BindHelpersList();
                BindSessions();
            }
        }

        private void LoadEventHeader()
        {
            if (!File.Exists(EventsXmlPath)) return;

            var doc = new XmlDocument(); doc.Load(EventsXmlPath);
            var ev = (XmlElement)doc.SelectSingleNode($"/events/event[@id='{_eventId}']");
            if (ev == null)
            {
                Response.Redirect("~/Account/UniversityAdmin/UniversityAdminHome.aspx");
                return;
            }

            var name = ev["name"]?.InnerText ?? "(unnamed)";
            EventName.Text = name;
            University.Text = ev["university"]?.InnerText ?? "";
            EventStatus.Text = "Published";

            var startStr = ev["startDate"]?.InnerText ?? ev["date"]?.InnerText ?? "";
            var endStr = ev["endDate"]?.InnerText ?? "";

            if (DateTime.TryParse(startStr, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var sDt) &&
                DateTime.TryParse(endStr, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var eDt))
            {
                EventDate.Text = sDt.ToLocalTime().ToString("MMM d, h:mm tt") +
                                 " — " + eDt.ToLocalTime().ToString("MMM d, h:mm tt");
            }
            else if (DateTime.TryParse(startStr, out var fallback))
            {
                EventDate.Text = fallback.ToLocalTime().ToString("yyyy-MM-dd");
            }
            else
            {
                EventDate.Text = "(unset)";
            }

            // Populate editable fields
            TxtEventName.Text = name == "(unnamed)" ? "" : name;
            TxtEventDescription.Text = ev["description"]?.InnerText ?? "";
        }

        // ===================== Event meta: save & delete =====================

        protected void BtnSaveEventMeta_Click(object sender, EventArgs e)
        {
            // Validate only the EventMeta group (name + description), not the scheduling form
            Page.Validate("EventMeta");
            if (!Page.IsValid) return;

            if (string.IsNullOrEmpty(_eventId))
            {
                EventMetaMessage.Text = "<span class='err'>Event id is missing.</span>";
                return;
            }

            if (!File.Exists(EventsXmlPath))
            {
                EventMetaMessage.Text = "<span class='err'>Events store not found.</span>";
                return;
            }

            var doc = new XmlDocument(); doc.Load(EventsXmlPath);
            var ev = doc.SelectSingleNode($"/events/event[@id='{_eventId}']") as XmlElement;
            if (ev == null)
            {
                EventMetaMessage.Text = "<span class='err'>Event not found.</span>";
                return;
            }

            var oldName = ev["name"]?.InnerText ?? "";
            var oldDescription = ev["description"]?.InnerText ?? "";

            var newName = (TxtEventName.Text ?? "").Trim();
            var newDescription = (TxtEventDescription.Text ?? "").Trim();

            SetOrCreateElement(ev, "name", newName);
            SetOrCreateElement(ev, "description", newDescription);

            doc.Save(EventsXmlPath);

            // Refresh header
            EventName.Text = newName;
            EventMetaMessage.Text = "<span class='ok'>Event details updated.</span>";

            // AUDIT: log changes (title + description)
            try
            {
                if (!string.Equals(oldName, newName, StringComparison.Ordinal))
                {
                    UniversityAuditLogger.AppendForCurrentUser(
                        this,
                        "Event Title Updated",
                        $"UniversityAdmin updated event title from '{oldName}' to '{newName}' (id={_eventId})."
                    );
                }

                if (!string.Equals(oldDescription, newDescription, StringComparison.Ordinal))
                {
                    UniversityAuditLogger.AppendForCurrentUser(
                        this,
                        "Event Description Updated",
                        $"UniversityAdmin updated description for event '{newName}' (id={_eventId})."
                    );
                }
            }
            catch
            {
                // Audit logging is best-effort; do not block the update.
            }
        }

        protected void BtnDeleteEvent_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_eventId))
            {
                EventMetaMessage.Text = "<span class='err'>Event id is missing.</span>";
                return;
            }

            if (!File.Exists(EventsXmlPath))
            {
                EventMetaMessage.Text = "<span class='err'>Events store not found.</span>";
                return;
            }

            var doc = new XmlDocument(); doc.Load(EventsXmlPath);
            var ev = doc.SelectSingleNode($"/events/event[@id='{_eventId}']") as XmlElement;
            if (ev == null)
            {
                EventMetaMessage.Text = "<span class='err'>Event not found.</span>";
                return;
            }

            var eventName = ev["name"]?.InnerText ?? "(unnamed)";
            var root = doc.DocumentElement;
            root.RemoveChild(ev);
            doc.Save(EventsXmlPath);

            // Optional cleanup: remove related eventCourses and eventSessions entries for this event
            try
            {
                if (File.Exists(EventCoursesXmlPath))
                {
                    var ecDoc = new XmlDocument(); ecDoc.Load(EventCoursesXmlPath);
                    var eNode = ecDoc.SelectSingleNode($"/eventCourses/event[@id='{_eventId}']") as XmlElement;
                    if (eNode != null && ecDoc.DocumentElement != null)
                    {
                        ecDoc.DocumentElement.RemoveChild(eNode);
                        ecDoc.Save(EventCoursesXmlPath);
                    }
                }

                if (File.Exists(EventSessionsXmlPath))
                {
                    var esDoc = new XmlDocument(); esDoc.Load(EventSessionsXmlPath);
                    var sesNodes = esDoc.SelectNodes($"/eventSessions/session[@eventId='{_eventId}']");
                    bool changed = false;
                    if (sesNodes != null && esDoc.DocumentElement != null)
                    {
                        foreach (XmlElement s in sesNodes)
                        {
                            esDoc.DocumentElement.RemoveChild(s);
                            changed = true;
                        }
                    }
                    if (changed)
                        esDoc.Save(EventSessionsXmlPath);
                }
            }
            catch
            {
                // Cleanup is best-effort.
            }

            // AUDIT: event deleted
            try
            {
                UniversityAuditLogger.AppendForCurrentUser(
                    this,
                    "Event Deleted",
                    $"UniversityAdmin deleted event '{eventName}' (id={_eventId})."
                );
            }
            catch
            {
                // Best-effort only.
            }

            // Redirect back to UA home after deletion
            Response.Redirect("~/Account/UniversityAdmin/UniversityAdminHome.aspx");
        }

        // ===================== Microcourse visibility =====================

        private void BindMicrocourses()
        {
            var rows = new List<object>();

            if (!File.Exists(MicrocoursesXmlPath))
            {
                NoCoursesPH.Visible = true;
                CoursesRepeater.DataSource = rows; CoursesRepeater.DataBind();
                return;
            }

            var enabledByCourse = LoadEventCourseSwitches();

            var doc = new XmlDocument(); doc.Load(MicrocoursesXmlPath);
            var nodes = doc.SelectNodes("/microcourses/course[@status='Published']");
            foreach (XmlElement c in nodes)
            {
                var id = c.GetAttribute("id");
                var title = c["title"]?.InnerText ?? "(untitled)";
                var duration = c["duration"]?.InnerText ?? "";

                var tagsNode = c["tags"];
                var tags = "";
                if (tagsNode != null && tagsNode.HasChildNodes)
                {
                    var list = new List<string>();
                    foreach (XmlElement t in tagsNode.SelectNodes("tag")) list.Add(t.InnerText);
                    tags = string.Join(", ", list);
                }

                var enabled = enabledByCourse.Contains(id);
                rows.Add(new { id, title, duration, tags, enabled });
            }

            NoCoursesPH.Visible = rows.Count == 0;
            CoursesRepeater.DataSource = rows;
            CoursesRepeater.DataBind();
        }

        private HashSet<string> LoadEventCourseSwitches()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(EventCoursesXmlPath)) return set;

            var doc = new XmlDocument(); doc.Load(EventCoursesXmlPath);
            var nodes = doc.SelectNodes($"/eventCourses/event[@id='{_eventId}']/course[@enabled='true']");
            foreach (XmlElement c in nodes) set.Add(c.GetAttribute("id"));
            return set;
        }

        protected void BtnSaveSwitches_Click(object sender, EventArgs e)
        {
            EnsureEventCoursesXml();

            var doc = new XmlDocument(); doc.Load(EventCoursesXmlPath);

            var evNode = doc.SelectSingleNode($"/eventCourses/event[@id='{_eventId}']") as XmlElement;
            if (evNode == null)
            {
                var root = doc.DocumentElement ?? doc.AppendChild(doc.CreateElement("eventCourses")) as XmlElement;
                evNode = doc.CreateElement("event");
                evNode.SetAttribute("id", _eventId);
                root.AppendChild(evNode);
            }
            else
            {
                while (evNode.HasChildNodes) evNode.RemoveChild(evNode.FirstChild);
            }

            foreach (RepeaterItem item in CoursesRepeater.Items)
            {
                var enabled = (item.FindControl("Enabled") as CheckBox)?.Checked ?? false;
                var id = (item.FindControl("CourseId") as HiddenField)?.Value;
                if (string.IsNullOrEmpty(id)) continue;

                var c = doc.CreateElement("course");
                c.SetAttribute("id", id);
                c.SetAttribute("enabled", enabled ? "true" : "false");
                evNode.AppendChild(c);
            }

            doc.Save(EventCoursesXmlPath);
        }

        private void EnsureEventCoursesXml()
        {
            if (File.Exists(EventCoursesXmlPath)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(EventCoursesXmlPath));
            var init = "<?xml version='1.0' encoding='utf-8'?><eventCourses></eventCourses>";
            File.WriteAllText(EventCoursesXmlPath, init);
        }

        private void BindCourseSelect()
        {
            CourseSelect.Items.Clear();
            if (!File.Exists(MicrocoursesXmlPath)) return;

            var doc = new XmlDocument(); doc.Load(MicrocoursesXmlPath);
            var nodes = doc.SelectNodes("/microcourses/course[@status='Published']");
            foreach (XmlElement c in nodes)
            {
                var id = c.GetAttribute("id");
                var title = c["title"]?.InnerText ?? "(untitled)";
                CourseSelect.Items.Add(new ListItem(title, id));
            }
        }

        // ===================== Session scheduling =====================

        protected void BtnAddSession_Click(object sender, EventArgs e)
        {
            ScheduleMessage.Text = "";

            var courseId = CourseSelect.SelectedValue;
            if (string.IsNullOrWhiteSpace(courseId))
            {
                ScheduleMessage.Text = "<span class='err'>Pick a course.</span>";
                return;
            }

            if (!TryParseLocalToUtc(SessionDateTimeStart.Text, out var startUtc) ||
                !TryParseLocalToUtc(SessionDateTimeEnd.Text, out var endUtc))
            {
                ScheduleMessage.Text = "<span class='err'>Enter valid start and end times.</span>";
                BindHelpersList();
                return;
            }
            if (endUtc <= startUtc)
            {
                ScheduleMessage.Text = "<span class='err'>End time must be after start.</span>";
                BindHelpersList();
                return;
            }

            var helper = (HelperSelect.SelectedValue ?? "").Trim();
            if (string.IsNullOrWhiteSpace(helper))
            {
                ScheduleMessage.Text = "<span class='err'>Pick a certified or eligible helper from the list.</span>";
                BindHelpersList();
                return;
            }

            var room = (Room.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(room))
            {
                ScheduleMessage.Text = "<span class='err'>Room is required.</span>";
                BindHelpersList();
                return;
            }

            if (!int.TryParse((Capacity.Text ?? "").Trim(), out var cap) || cap < 1)
            {
                ScheduleMessage.Text = "<span class='err'>Max participants must be a whole number of at least 1.</span>";
                BindHelpersList();
                return;
            }

            EnsureEventSessionsXml();

            var doc = new XmlDocument(); doc.Load(EventSessionsXmlPath);
            var exist = doc.SelectNodes($"/eventSessions/session[@eventId='{_eventId}']");

            foreach (XmlElement s in exist)
            {
                var sHelper = (s["helper"]?.InnerText ?? "").Trim();
                if (!string.Equals(helper, sHelper, StringComparison.OrdinalIgnoreCase)) continue;

                var sStart = s["start"]?.InnerText ?? "";
                var sEnd = s["end"]?.InnerText ?? "";
                if (!TryParseIsoUtc(sStart, out var sStartUtc) ||
                    !TryParseIsoUtc(sEnd, out var sEndUtc))
                    continue;

                if (IntervalsOverlap(startUtc, endUtc, sStartUtc, sEndUtc))
                {
                    ScheduleMessage.Text =
                        $"<span class='err'>Helper <strong>{Server.HtmlEncode(helper)}</strong> is already booked " +
                        $"from {sStartUtc.ToLocalTime():yyyy-MM-dd HH:mm} to {sEndUtc.ToLocalTime():HH:mm}.</span>";
                    BindHelpersList();
                    return;
                }
            }

            var root = doc.DocumentElement ?? doc.AppendChild(doc.CreateElement("eventSessions")) as XmlElement;

            var node = doc.CreateElement("session");
            node.SetAttribute("eventId", _eventId);
            node.SetAttribute("id", Guid.NewGuid().ToString("N"));
            node.AppendChild(Mk(doc, "courseId", courseId));
            node.AppendChild(Mk(doc, "start", startUtc.ToString("o")));
            node.AppendChild(Mk(doc, "end", endUtc.ToString("o")));
            node.AppendChild(Mk(doc, "room", room));
            node.AppendChild(Mk(doc, "helper", helper));
            node.AppendChild(Mk(doc, "capacity", cap.ToString()));

            root.AppendChild(node);
            doc.Save(EventSessionsXmlPath);

            BindSessions();

            SessionDateTimeStart.Text = SessionDateTimeEnd.Text = Room.Text = Capacity.Text = "";
            if (HelperSelect.Items.Count > 0)
            {
                HelperSelect.SelectedIndex = 0;
            }

            BindHelpersList();

            ScheduleMessage.Text = "<span class='ok'>Session added.</span>";
        }

        private void EnsureEventSessionsXml()
        {
            if (File.Exists(EventSessionsXmlPath)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(EventSessionsXmlPath));
            var init = "<?xml version='1.0' encoding='utf-8'?><eventSessions></eventSessions>";
            File.WriteAllText(EventSessionsXmlPath, init);
        }

        private void BindSessions()
        {
            var rows = new List<object>();

            if (!File.Exists(EventSessionsXmlPath))
            {
                NoSessionsPH.Visible = true;
                SessionsRepeater.DataSource = rows;
                SessionsRepeater.DataBind();
                return;
            }

            var titles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (File.Exists(MicrocoursesXmlPath))
            {
                var md = new XmlDocument(); md.Load(MicrocoursesXmlPath);
                foreach (XmlElement c in md.SelectNodes("/microcourses/course"))
                {
                    titles[c.GetAttribute("id")] = c["title"]?.InnerText ?? "(untitled)";
                }
            }

            var doc = new XmlDocument(); doc.Load(EventSessionsXmlPath);
            foreach (XmlElement s in doc.SelectNodes($"/eventSessions/session[@eventId='{_eventId}']"))
            {
                var sessionId = s.GetAttribute("id");

                var courseId = s["courseId"]?.InnerText ?? "";
                var startIso = s["start"]?.InnerText ?? "";
                var endIso = s["end"]?.InnerText ?? "";
                var room = s["room"]?.InnerText ?? "";
                var helper = (s["helper"]?.InnerText ?? "").Trim();
                var cap = s["capacity"]?.InnerText ?? "";

                string startLocal = ToLocalHuman(startIso);
                string endLocal = ToLocalHuman(endIso);

                titles.TryGetValue(courseId, out var title);

                rows.Add(new
                {
                    sessionId = sessionId,
                    eventId = _eventId,
                    courseTitle = title ?? "(untitled)",
                    startLocal,
                    endLocal,
                    room,
                    helper,
                    capacity = string.IsNullOrEmpty(cap) ? "—" : cap
                });
            }

            NoSessionsPH.Visible = rows.Count == 0;
            SessionsRepeater.DataSource = rows;
            SessionsRepeater.DataBind();
        }

        // ===================== Helper list & filters =====================

        private void BindHelpersList()
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
                        var hName = (s["helper"]?.InnerText ?? "").Trim();
                        if (!string.Equals(hName, name.Trim(), StringComparison.OrdinalIgnoreCase))
                            continue;

                        var sStartIso = s["start"]?.InnerText ?? "";
                        var sEndIso = s["end"]?.InnerText ?? "";
                        if (!TryParseIsoUtc(sStartIso, out var sStartUtc) ||
                            !TryParseIsoUtc(sEndIso, out var sEndUtc))
                            continue;

                        if (IntervalsOverlap(startUtc, endUtc, sStartUtc, sEndUtc))
                        {
                            hasOverlap = true;
                            break;
                        }
                    }
                }

                rows.Add(new HelperRow
                {
                    HelperId = id,
                    Name = name,
                    IsCertified = isCert,
                    IsEligible = isElig,
                    HasOverlap = hasOverlap,
                    CertLabel = certLabel,
                    CertCssClass = certCss,
                    LastDeliveredUtc = lastDelivered,
                    DeliveredCount = deliveredCount
                });
            }

            // Default base: only eligible + certified helpers
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
            BindHelpersList();
        }

        protected void SessionTime_TextChanged(object sender, EventArgs e)
        {
            BindHelpersList();
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

        private static XmlElement Mk(XmlDocument d, string name, string val)
        {
            var el = d.CreateElement(name);
            el.InnerText = val ?? "";
            return el;
        }

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

        private static string ToLocalHuman(string iso)
        {
            if (TryParseIsoUtc(iso, out var dt))
                return dt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            return "(unset)";
        }
    }
}





