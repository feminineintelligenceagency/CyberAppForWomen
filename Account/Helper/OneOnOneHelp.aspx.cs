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


namespace CyberApp_FIA.Helper
{
    /// <summary>
    /// 1:1 Help workspace view for Helpers.
    /// Shows all participants assigned to the signed-in Helper
    /// with their first name and email address, plus message indicators.
    /// Also lets Helpers log one-to-one help sessions for certification progress.
    /// </summary>
    public partial class OneOnOneHelp : SecurePage
    {
        private string UsersXmlPath => Server.MapPath("~/App_Data/users.xml");

        // Shared helper messages XML (same file as participant side)
        private string HelperMessagesXmlPath => Server.MapPath("~/App_Data/helperMessages.xml");

        // Helper progress + 1:1 help notes
        private string HelperProgressXmlPath => Server.MapPath("~/App_Data/helperProgress.xml");
        private string HelperHelpNotesXmlPath => Server.MapPath("~/App_Data/helperHelpNotes.xml");

        private static readonly object HelperProgressLock = new object();
        private static readonly object HelperHelpNotesLock = new object();
        private static readonly TimeSpan HelpUndoWindow = TimeSpan.FromMinutes(5);

        private const string HelpHistorySessionKey = "OneOnOneHelpHistory";

        private sealed class ParticipantRow
        {
            public string ParticipantId { get; set; }
            public string FirstName { get; set; }
            public string Email { get; set; }
            public string University { get; set; }

            // True if there is at least one conversation with this participant
            public bool HasConversation { get; set; }

            // Link to Helper conversations page for this participant
            public string ConversationsUrl { get; set; }
        }

        /// <summary>
        /// Simple row for recent 1:1 help history.
        /// Snapshot carries the data needed to undo.
        /// </summary>
        private sealed class HelpHistoryRow
        {
            public string CourseTitle { get; set; }
            public string WhenLabel { get; set; }
            public string Snapshot { get; set; }
            public long Ticks { get; set; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            GuardHelperRole();

            if (!IsPostBack)
            {
                var helperId = Session["UserId"] as string ?? string.Empty;

                BindParticipants();

                try
                {
                    if (!string.IsNullOrWhiteSpace(helperId))
                    {
                        BindDeliverableCourses(helperId);
                        BindHelpHistory();
                    }
                }
                catch
                {
                    // Progress/history are best-effort; page should remain usable.
                }
            }
        }

        /// <summary>
        /// Ensures only Helpers can access this page.
        /// Redirects to login if the role or user id is missing/mismatched.
        /// </summary>
        private void GuardHelperRole()
        {
            var roleRaw = Session["Role"] as string ?? "";
            if (!string.Equals(roleRaw.Trim(), "Helper", StringComparison.OrdinalIgnoreCase))
            {
                Response.Redirect("~/Account/Login.aspx");
                return;
            }

            var userId = Session["UserId"] as string;
            if (string.IsNullOrWhiteSpace(userId))
            {
                Response.Redirect("~/Account/Login.aspx");
                return;
            }
        }

        /// <summary>
        /// Loads all participants that are assigned to the current Helper and
        /// binds them into the participants repeater.
        /// </summary>
        private void BindParticipants()
        {
            var currentHelperId = Session["UserId"] as string;
            var rows = new List<ParticipantRow>();

            if (string.IsNullOrWhiteSpace(currentHelperId))
            {
                NoParticipantsPH.Visible = true;
                ParticipantsRepeater.DataSource = null;
                ParticipantsRepeater.DataBind();
                return;
            }

            if (!File.Exists(UsersXmlPath))
            {
                NoParticipantsPH.Visible = true;
                ParticipantsRepeater.DataSource = null;
                ParticipantsRepeater.DataBind();
                return;
            }

            var participantsWithUnreadMessages = LoadParticipantsWithUnreadMessages(currentHelperId);

            try
            {
                var doc = new XmlDocument();
                doc.Load(UsersXmlPath);

                var userNodes = doc.SelectNodes("/users/user");
                if (userNodes != null)
                {
                    foreach (XmlElement user in userNodes)
                    {
                        // Only show participants who are assigned to this Helper
                        if (!IsAssignedToHelper(user, currentHelperId))
                            continue;

                        var participantId = (user.GetAttribute("id") ?? string.Empty).Trim();
                        var firstName = user["firstName"]?.InnerText ?? "";
                        var email = user["email"]?.InnerText ?? "";
                        var uni = user["university"]?.InnerText ?? "";

                        if (string.IsNullOrWhiteSpace(firstName) &&
                            string.IsNullOrWhiteSpace(email))
                        {
                            continue; // skip incomplete rows
                        }

                        var hasConversation = !string.IsNullOrWhiteSpace(participantId) &&
                      participantsWithUnreadMessages.Contains(participantId);

                        var conversationsUrl = ResolveUrl(
                            "~/Account/Helper/ParticipantConversations.aspx?participantId="
                            + Server.UrlEncode(participantId));

                        rows.Add(new ParticipantRow
                        {
                            ParticipantId = participantId,
                            FirstName = firstName.Trim(),
                            Email = email.Trim(),
                            University = (uni ?? string.Empty).Trim(),
                            HasConversation = hasConversation,
                            ConversationsUrl = conversationsUrl
                        });
                    }
                }
            }
            catch
            {
                // If anything goes wrong loading XML, show empty state gracefully.
                rows.Clear();
            }

            rows = rows
                .OrderBy(r => string.IsNullOrWhiteSpace(r.FirstName) ? "{" : r.FirstName)
                .ThenBy(r => r.Email)
                .ToList();

            NoParticipantsPH.Visible = rows.Count == 0;
            ParticipantsRepeater.DataSource = rows;
            ParticipantsRepeater.DataBind();
        }

        private HashSet<string> LoadParticipantsWithUnreadMessages(string helperId)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(helperId))
                return set;

            if (!File.Exists(HelperMessagesXmlPath))
                return set;

            try
            {
                var doc = new XmlDocument();
                doc.Load(HelperMessagesXmlPath);

                foreach (XmlElement conv in doc.SelectNodes($"/helperMessages/conversation[@helperId='{helperId}']"))
                {
                    var pid = (conv.GetAttribute("participantId") ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(pid))
                        continue;

                    var helperLastReadStr = (conv.GetAttribute("helperLastReadUtc") ?? string.Empty).Trim();
                    DateTime helperLastReadUtc = DateTime.MinValue;

                    if (!string.IsNullOrWhiteSpace(helperLastReadStr))
                    {
                        DateTime.TryParse(
                            helperLastReadStr,
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                            out helperLastReadUtc);
                    }

                    DateTime latestParticipantMessageUtc = DateTime.MinValue;

                    foreach (XmlElement msg in conv.SelectNodes("message"))
                    {
                        var from = (msg.GetAttribute("from") ?? string.Empty).Trim();
                        var isFromHelper =
                            from.Equals("helper", StringComparison.OrdinalIgnoreCase) ||
                            from.Equals("Helper", StringComparison.OrdinalIgnoreCase);

                        if (isFromHelper)
                            continue;

                        // Support both participant-side "ts" and helper-side "sentOn"
                        var msgTimeRaw =
                            (msg.GetAttribute("ts") ?? string.Empty).Trim();

                        if (string.IsNullOrWhiteSpace(msgTimeRaw))
                        {
                            msgTimeRaw = (msg.GetAttribute("sentOn") ?? string.Empty).Trim();
                        }

                        if (DateTime.TryParse(
                                msgTimeRaw,
                                CultureInfo.InvariantCulture,
                                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                                out var msgUtc))
                        {
                            if (msgUtc > latestParticipantMessageUtc)
                                latestParticipantMessageUtc = msgUtc;
                        }
                    }

                    if (latestParticipantMessageUtc > helperLastReadUtc)
                    {
                        set.Add(pid);
                    }
                }
            }
            catch
            {
                set.Clear();
            }

            return set;
        }


        /// <summary>
        /// Determines whether a given user node is assigned to the specified Helper.
        /// Supports multiple possible XML shapes:
        ///   - helperId attribute on &lt;user&gt;
        ///   - assignedHelperId attribute on &lt;user&gt;
        ///   - &lt;helperId&gt; child element
        ///   - &lt;assignedHelper id="..." /&gt; child element
        /// </summary>
        private static bool IsAssignedToHelper(XmlElement userNode, string helperId)
        {
            if (userNode == null || string.IsNullOrWhiteSpace(helperId))
                return false;

            // 1) helperId attribute on <user>
            var helperAttr = (userNode.GetAttribute("helperId") ?? string.Empty).Trim();
            if (!string.IsNullOrEmpty(helperAttr) &&
                string.Equals(helperAttr, helperId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // 1b) assignedHelperId attribute on <user> (current participant XML format)
            var assignedHelperAttr = (userNode.GetAttribute("assignedHelperId") ?? string.Empty).Trim();
            if (!string.IsNullOrEmpty(assignedHelperAttr) &&
                string.Equals(assignedHelperAttr, helperId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // 2) <helperId> child element
            var helperElemVal = (userNode["helperId"]?.InnerText ?? string.Empty).Trim();
            if (!string.IsNullOrEmpty(helperElemVal) &&
                string.Equals(helperElemVal, helperId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // 3) <assignedHelper id="..." /> child element
            var assignedHelperNode = userNode.SelectSingleNode("assignedHelper") as XmlElement;
            if (assignedHelperNode != null)
            {
                var assignedId = (assignedHelperNode.GetAttribute("id") ?? string.Empty).Trim();
                if (!string.IsNullOrEmpty(assignedId) &&
                    string.Equals(assignedId, helperId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        // ========= 1:1 help logging and undo =========

        /// <summary>
        /// Populate the microcourse dropdown for 1:1 help based on helperProgress.xml,
        /// including any course where the helper is eligible or certified.
        /// </summary>
        private void BindDeliverableCourses(string helperId)
        {
            if (HelpCourseDropDown == null)
            {
                return;
            }

            HelpCourseDropDown.Items.Clear();
            HelpCourseDropDown.Items.Add(new ListItem("Select a course…", ""));

            if (!File.Exists(HelperProgressXmlPath))
            {
                return;
            }

            var doc = new XmlDocument();
            doc.Load(HelperProgressXmlPath);

            // Search for helper regardless of root name.
            var helperNode = doc.SelectSingleNode($"//helper[@id='{helperId}']") as XmlElement;
            if (helperNode == null)
            {
                return;
            }

            foreach (XmlElement courseEl in helperNode.SelectNodes("course"))
            {
                var id = courseEl.GetAttribute("id");
                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                var titleNode = courseEl["title"];
                var title = titleNode != null ? (titleNode.InnerText ?? "").Trim() : id;

                var isEligibleText = courseEl["isEligible"]?.InnerText ?? "";
                var isCertifiedText = courseEl["isCertified"]?.InnerText ?? "";

                bool isEligible = string.Equals(isEligibleText.Trim(), "true", StringComparison.OrdinalIgnoreCase);
                bool isCertified = string.Equals(isCertifiedText.Trim(), "true", StringComparison.OrdinalIgnoreCase);

                if (!isEligible && !isCertified)
                {
                    continue;
                }

                HelpCourseDropDown.Items.Add(new ListItem(title, id));
            }
        }

        private static int ParseIntSafe(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return 0;
            }

            if (int.TryParse(raw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
            {
                return v;
            }

            return 0;
        }

        private XmlDocument LoadHelperProgressDoc()
        {
            var doc = new XmlDocument();

            if (File.Exists(HelperProgressXmlPath))
            {
                doc.Load(HelperProgressXmlPath);
                if (doc.DocumentElement == null)
                {
                    var rootMissing = doc.CreateElement("helperProgress");
                    doc.AppendChild(rootMissing);
                }
            }
            else
            {
                var root = doc.CreateElement("helperProgress");
                doc.AppendChild(root);
            }

            return doc;
        }

        private void SaveHelperProgressDoc(XmlDocument doc)
        {
            lock (HelperProgressLock)
            {
                doc.Save(HelperProgressXmlPath);
            }
        }

        private XmlDocument LoadHelperHelpNotesDoc()
        {
            var doc = new XmlDocument();

            if (File.Exists(HelperHelpNotesXmlPath))
            {
                doc.Load(HelperHelpNotesXmlPath);
                if (doc.DocumentElement == null)
                {
                    var rootMissing = doc.CreateElement("helperHelpNotes");
                    doc.AppendChild(rootMissing);
                }
            }
            else
            {
                var root = doc.CreateElement("helperHelpNotes");
                doc.AppendChild(root);
            }

            return doc;
        }

        private void SaveHelperHelpNotesDoc(XmlDocument doc)
        {
            lock (HelperHelpNotesLock)
            {
                doc.Save(HelperHelpNotesXmlPath);
            }
        }

        /// <summary>
        /// Increments helpSessions for the selected course plus totalHelpSessions
        /// for this helper in helperProgress.xml, and returns a snapshot string
        /// that can be used later to undo.
        /// </summary>
        private bool TryIncrementHelpSession(
            string helperId,
            string courseId,
            out string courseTitle,
            out string snapshot)
        {
            courseTitle = null;
            snapshot = null;

            if (string.IsNullOrWhiteSpace(helperId) || string.IsNullOrWhiteSpace(courseId))
            {
                return false;
            }

            var doc = LoadHelperProgressDoc();
            var helperNode = doc.SelectSingleNode($"//helper[@id='{helperId}']") as XmlElement;
            if (helperNode == null)
            {
                return false;
            }

            var courseNode = helperNode.SelectSingleNode($"course[@id='{courseId}']") as XmlElement;
            if (courseNode == null)
            {
                return false;
            }

            courseTitle = (courseNode["title"]?.InnerText ?? "").Trim();

            int prevCourseHelp = ParseIntSafe(courseNode["helpSessions"]?.InnerText);

            var totalsNode = helperNode.SelectSingleNode("totals") as XmlElement;
            int prevTotalHelp = 0;
            if (totalsNode != null)
            {
                prevTotalHelp = ParseIntSafe(totalsNode["totalHelpSessions"]?.InnerText);
            }

            int newCourseHelp = prevCourseHelp + 1;
            int newTotalHelp = prevTotalHelp + 1;

            var helpEl = courseNode["helpSessions"];
            if (helpEl == null)
            {
                helpEl = doc.CreateElement("helpSessions");
                courseNode.AppendChild(helpEl);
            }
            helpEl.InnerText = newCourseHelp.ToString(CultureInfo.InvariantCulture);

            if (totalsNode == null)
            {
                totalsNode = doc.CreateElement("totals");
                helperNode.AppendChild(totalsNode);
            }

            var totalHelpEl = totalsNode["totalHelpSessions"];
            if (totalHelpEl == null)
            {
                totalHelpEl = doc.CreateElement("totalHelpSessions");
                totalsNode.AppendChild(totalHelpEl);
            }
            totalHelpEl.InnerText = newTotalHelp.ToString(CultureInfo.InvariantCulture);

            SaveHelperProgressDoc(doc);

            // Build snapshot helperId|courseId|prevCourseHelp|prevTotalHelp|ticks
            var ticks = DateTime.UtcNow.Ticks;
            snapshot = string.Join("|",
                helperId ?? string.Empty,
                courseId ?? string.Empty,
                prevCourseHelp.ToString(CultureInfo.InvariantCulture),
                prevTotalHelp.ToString(CultureInfo.InvariantCulture),
                ticks.ToString(CultureInfo.InvariantCulture));

            // Also keep history in session so we can display last 3
            AddHelpHistoryRow(courseTitle, snapshot, ticks);

            return true;
        }

        /// <summary>
        /// Rolls back a previously logged help session using the snapshot string.
        /// Restores both per-course helpSessions and totalHelpSessions, if
        /// within the configured undo window.
        /// </summary>
        private bool TryUndoHelpSession(string helperId, string snapshot, out string courseTitle)
        {
            courseTitle = null;

            if (string.IsNullOrWhiteSpace(helperId) || string.IsNullOrWhiteSpace(snapshot))
            {
                return false;
            }

            var parts = snapshot.Split('|');
            if (parts.Length != 5)
            {
                return false;
            }

            var snapHelperId = parts[0];
            var courseId = parts[1];

            if (!string.Equals(helperId, snapHelperId, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var prevCourseHelp))
            {
                return false;
            }

            if (!int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var prevTotalHelp))
            {
                return false;
            }

            if (!long.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks))
            {
                return false;
            }

            var savedUtc = new DateTime(ticks, DateTimeKind.Utc);
            var age = DateTime.UtcNow - savedUtc;
            if (age > HelpUndoWindow)
            {
                // Undo window expired: also drop from history.
                RemoveHelpHistoryRow(snapshot);
                return false;
            }

            var doc = LoadHelperProgressDoc();
            var helperNode = doc.SelectSingleNode($"//helper[@id='{helperId}']") as XmlElement;
            if (helperNode == null)
            {
                RemoveHelpHistoryRow(snapshot);
                return false;
            }

            var courseNode = helperNode.SelectSingleNode($"course[@id='{courseId}']") as XmlElement;
            if (courseNode == null)
            {
                RemoveHelpHistoryRow(snapshot);
                return false;
            }

            courseTitle = (courseNode["title"]?.InnerText ?? "").Trim();

            var helpEl = courseNode["helpSessions"];
            if (helpEl == null)
            {
                helpEl = doc.CreateElement("helpSessions");
                courseNode.AppendChild(helpEl);
            }
            helpEl.InnerText = prevCourseHelp.ToString(CultureInfo.InvariantCulture);

            var totalsNode = helperNode.SelectSingleNode("totals") as XmlElement;
            if (totalsNode == null)
            {
                totalsNode = doc.CreateElement("totals");
                helperNode.AppendChild(totalsNode);
            }

            var totalHelpEl = totalsNode["totalHelpSessions"];
            if (totalHelpEl == null)
            {
                totalHelpEl = doc.CreateElement("totalHelpSessions");
                totalsNode.AppendChild(totalHelpEl);
            }
            totalHelpEl.InnerText = prevTotalHelp.ToString(CultureInfo.InvariantCulture);

            SaveHelperProgressDoc(doc);

            // Remove from in-memory history after successful undo
            RemoveHelpHistoryRow(snapshot);

            return true;
        }

        /// <summary>
        /// Append a helper note for this 1:1 help session into helperHelpNotes.xml.
        /// </summary>
        private void AppendHelperHelpNote(string helperId, string courseId, string courseTitle, string notes)
        {
            var doc = LoadHelperHelpNotesDoc();
            var root = doc.DocumentElement;
            if (root == null)
            {
                root = doc.CreateElement("helperHelpNotes");
                doc.AppendChild(root);
            }

            var noteEl = doc.CreateElement("note");
            noteEl.SetAttribute("helperId", helperId ?? string.Empty);
            noteEl.SetAttribute("courseId", courseId ?? string.Empty);
            noteEl.SetAttribute("courseTitle", courseTitle ?? string.Empty);
            noteEl.SetAttribute("tsUtc", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));

            var textEl = doc.CreateElement("text");
            textEl.InnerText = notes ?? string.Empty;
            noteEl.AppendChild(textEl);

            root.AppendChild(noteEl);

            SaveHelperHelpNotesDoc(doc);
        }

        /// <summary>
        /// Adds or updates a help history row in the session list.
        /// </summary>
        private void AddHelpHistoryRow(string courseTitle, string snapshot, long ticks)
        {
            var list = Session[HelpHistorySessionKey] as List<HelpHistoryRow>;
            if (list == null)
            {
                list = new List<HelpHistoryRow>();
            }

            list.Add(new HelpHistoryRow
            {
                CourseTitle = courseTitle ?? string.Empty,
                Snapshot = snapshot ?? string.Empty,
                Ticks = ticks
            });

            Session[HelpHistorySessionKey] = list;
        }

        private void RemoveHelpHistoryRow(string snapshot)
        {
            var list = Session[HelpHistorySessionKey] as List<HelpHistoryRow>;
            if (list == null)
            {
                return;
            }

            list.RemoveAll(h => string.Equals(h.Snapshot, snapshot, StringComparison.Ordinal));
            Session[HelpHistorySessionKey] = list;
        }

        /// <summary>
        /// Binds the last three 1:1 help logs from the session history.
        /// </summary>
        private void BindHelpHistory()
        {
            var list = Session[HelpHistorySessionKey] as List<HelpHistoryRow>;
            if (list == null || list.Count == 0)
            {
                HelpHistoryEmpty.Visible = true;
                HelpHistoryRepeater.DataSource = null;
                HelpHistoryRepeater.DataBind();
                return;
            }

            // Sort newest first and take last three
            var latest = list
                .OrderByDescending(h => h.Ticks)
                .Take(3)
                .ToList();

            foreach (var item in latest)
            {
                var tsUtc = new DateTime(
                    item.Ticks <= 0 ? DateTime.UtcNow.Ticks : item.Ticks,
                    DateTimeKind.Utc);

                var local = tsUtc.ToLocalTime();
                item.WhenLabel = "Logged at " + local.ToString("h:mm tt", CultureInfo.CurrentCulture);
            }

            HelpHistoryEmpty.Visible = false;
            HelpHistoryRepeater.DataSource = latest;
            HelpHistoryRepeater.DataBind();
        }

        // ========= Event handlers for 1:1 help logging =========

        protected void HelpSubmitButton_Click(object sender, EventArgs e)
        {
            HelpStatusLabel.Text = string.Empty;
            HelpStatusLabel.CssClass = "log-status";

            var helperId = Session["UserId"] as string ?? string.Empty;
            if (string.IsNullOrWhiteSpace(helperId))
            {
                HelpStatusLabel.Text = "We could not find your Helper account. Please sign in again.";
                HelpStatusLabel.CssClass = "log-status log-status-error";
                return;
            }

            var selectedCourseId = HelpCourseDropDown != null ? HelpCourseDropDown.SelectedValue : string.Empty;
            if (string.IsNullOrWhiteSpace(selectedCourseId))
            {
                HelpStatusLabel.Text = "Please choose a course before logging a help session.";
                HelpStatusLabel.CssClass = "log-status log-status-error";
                return;
            }

            string courseTitle;
            string snapshot;
            if (!TryIncrementHelpSession(helperId, selectedCourseId, out courseTitle, out snapshot))
            {
                HelpStatusLabel.Text = "We could not log this help session. Please try again.";
                HelpStatusLabel.CssClass = "log-status log-status-error";
                return;
            }

            var notesText = HelpDetailsTextBox.Text ?? string.Empty;
            try
            {
                AppendHelperHelpNote(helperId, selectedCourseId, courseTitle, notesText);
            }
            catch
            {
                // Notes are best-effort; do not block certification progress.
            }

            // --- Audit log: helper logged a 1:1 help session ---
            try
            {
                var courseLabel = string.IsNullOrWhiteSpace(courseTitle) ? "(course)" : courseTitle;

                UniversityAuditLogger.AppendForCurrentUser(
                    this,
                    "Helper 1:1 Help Session",
                    $"Helper logged a one-on-one help session for microcourse \"{courseLabel}\"."
                // Intentionally not including notesText so sensitive details
                // stay only in helperHelpNotes.xml for certification review.
                );
            }
            catch
            {
                // Audit logging should never break main helper flows.
            }
            // --- end audit log block ---

            // Clear notes box after successful log.
            HelpDetailsTextBox.Text = string.Empty;

            // Refresh recent history (last three)
            BindHelpHistory();

            HelpStatusLabel.Text = "Help session logged.";
            HelpStatusLabel.CssClass = "log-status";
        }

        protected void HelpHistoryRepeater_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (!string.Equals(e.CommandName, "undoHelp", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var helperId = Session["UserId"] as string ?? string.Empty;
            if (string.IsNullOrWhiteSpace(helperId))
            {
                return;
            }

            var snapshot = Convert.ToString(e.CommandArgument ?? string.Empty);
            if (string.IsNullOrWhiteSpace(snapshot))
            {
                return;
            }

            string courseTitle;
            if (TryUndoHelpSession(helperId, snapshot, out courseTitle))
            {
                HelpStatusLabel.Text = "Your 1:1 help log has been undone.";
                HelpStatusLabel.CssClass = "log-status";
            }
            else
            {
                HelpStatusLabel.Text = "Undo is no longer available for that help log.";
                HelpStatusLabel.CssClass = "log-status log-status-error";
            }

            // Rebind history so the undone row disappears or updates.
            BindHelpHistory();
        }
    }
}


