using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml;
using CyberApp_FIA.Services;


namespace CyberApp_FIA.Helper
{
    /// <summary>
    /// Helper schedule page.
    /// Shows all sessions where this helper is assigned in eventSessions.xml,
    /// resolving microcourse titles from microcourses.xml and converting
    /// stored UTC timestamps to local, human-readable strings.
    /// </summary>
    public partial class Schedule : SecurePage
    {
        private string UsersXmlPath => Server.MapPath("~/App_Data/users.xml");
        private string EventSessionsXmlPath => Server.MapPath("~/App_Data/eventSessions.xml");
        private string MicrocoursesXmlPath => Server.MapPath("~/App_Data/microcourses.xml");
        private string EnrollmentsXmlPath => Server.MapPath("~/App_Data/enrollments.xml");
        private string HelperCheckinsXmlPath => Server.MapPath("~/App_Data/helperCheckins.xml");

        // NEW: participant completions storage (shared with Participant view)
        private string CompletionsXmlPath => Server.MapPath("~/App_Data/completions.xml");

        // NEW: missing participant sessions storage (Helper marks no-shows)
        private string MissingParticipantSessionsXmlPath => Server.MapPath("~/App_Data/missingParticipantSessions.xml");

        // NEW: participant badge ownership storage
        private string ParticipantBadgesXmlPath => Server.MapPath("~/App_Data/participantBadges.xml");
        private static readonly object ParticipantBadgesLock = new object();

        private static readonly object EnrollmentsLock = new object();
        private static readonly object CompletionsLock = new object();
        private static readonly object MissingLock = new object();

        private static readonly object ParticipantSessionActionLock = new object();
        private static readonly TimeSpan ParticipantActionUndoWindow = TimeSpan.FromMinutes(2);
        private const string ParticipantActionUndoSessionKey = "ParticipantActionUndoSnapshots";

        // New: helper progress + notes storage
        private string HelperProgressXmlPath => Server.MapPath("~/App_Data/helperProgress.xml");
      

        // NEW: helper badge ownership storage
        private string HelperBadgesXmlPath => Server.MapPath("~/App_Data/helperBadges.xml");
        private static readonly object HelperBadgesLock = new object();

        private static readonly object HelperCheckinsLock = new object();
        private static readonly TimeSpan HelperCheckinUndoWindow = TimeSpan.FromMinutes(5);

        // New: locks + window for delivered-session undo
        private static readonly object HelperProgressLock = new object();



        private static readonly Dictionary<string, string> BadgeImageByCourseTitle =
    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    { "Enhancing Social Media Privacy Settings", "~/ParticipantBadges/ParticipantSMSettingsBadge.png" },
    { "Phishing Awareness And Email Security", "~/ParticipantBadges/ParticipantPhishingBadge.png" },
    { "Privacy Settings on Popular Apps", "~/ParticipantBadges/ParticipantPopAppsBadge.png" },
    { "Detecting Spyware Infection on Devices", "~/ParticipantBadges/ParticipantSpywareBadge.png" },
    { "Detecting Spyware Infections on Devices", "~/ParticipantBadges/ParticipantSpywareBadge.png" },
    { "2FA Setup And Management", "~/ParticipantBadges/Participant2FABadge.png" },
    { "Password Management And Security", "~/ParticipantBadges/ParticipantPasswordManagementBadge.png" },
    { "Managing Digital Footprint", "~/ParticipantBadges/ParticipantFootprintBadge.png" },
    { "Managing Your Digital Footprint", "~/ParticipantBadges/ParticipantFootprintBadge.png" },
    { "Recognizing AI-Assisted Manipulation And Deepfakes", "~/ParticipantBadges/ParticipantAIBadge.png" },
    { "Using VPNs for Secure Browsing", "~/ParticipantBadges/ParticipantVPNBadge.png" },
    { "Safe Use of Public Computers and Wi-Fi", "~/ParticipantBadges/ParticipantPublicComputerBadge.png" },
    { "Identifying Hidden-Surveilance Devices", "~/ParticipantBadges/ParticipantElectronicScanningBadge.png" },
    { "Identifying Hidden Surveillance Devices (Electronic Scanning)", "~/ParticipantBadges/ParticipantElectronicScanningBadge.png" },
    { "Safe Online Banking Practices", "~/ParticipantBadges/ParticipantBankingBadge.png" },
    { "Recognizing Malicious Mobile Apps", "~/ParticipantBadges/ParticipantMobileAppsBadge.png" },
    { "Verifying Online Identities and Combating Catfishing", "~/ParticipantBadges/ParticipantOnlineIdentityBadge.png" },
    { "Securing Home Wi-Fi Networks", "~/ParticipantBadges/ParticipantHomeNetBadge.png" }
};


        private static readonly Dictionary<string, string> HelperBadgeBaseByCourseTitle =
     new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
 {
    { "Enhancing Social Media Privacy Settings", "SMSettingsHelper.png" },
    { "Phishing Awareness And Email Security", "PhishingHelper.png" },
    { "Privacy Settings on Popular Apps", "PopAppsHelper.png" },
    { "Detecting Spyware Infection on Devices", "SpywareHelper.png" },
    { "Detecting Spyware Infections on Devices", "SpywareHelper.png" },
    { "2FA Setup And Management", "2FAHelper.png" },
    { "Password Management And Security", "PassManagementHelper.png" },
    { "Managing Digital Footprint", "FootprintHelper.png" },
    { "Managing Your Digital Footprint", "FootprintHelper.png" },
    { "Recognizing AI-Assisted Manipulation And Deepfakes", "AIHelper.png" },
    { "Using VPNs for Secure Browsing", "VPNHelper.png" },
    { "Safe Use of Public Computers and Wi-Fi", "PublicComputersHelper.png" },
    { "Identifying Hidden-Surveilance Devices", "ElectronicScanningHelper.png" },
    { "Identifying Hidden Surveillance Devices (Electronic Scanning)", "ElectronicScanningHelper.png" },
    { "Safe Online Banking Practices", "BankingHelper.png" },
    { "Recognizing Malicious Mobile Apps", "MaliciousAppsHelper.png" },
    { "Verifying Online Identities and Combating Catfishing", "IdentityHelper.png" },
    { "Securing Home Wi-Fi Networks", "HomeNetHelper.png" }
 };

        private const int Tier1Threshold = 5;
        private const int Tier2Threshold = 10;
        private const int Tier3Threshold = 20;

        private static string NormalizeTitle(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            return string.Join(" ", s.Trim().Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
        }

        /// <summary>
        /// Small DTO for binding schedule cards.
        /// </summary>
        private sealed class SessionRow
        {
            public string CourseTitle { get; set; }
            public string DayTime { get; set; }
            public string Capacity { get; set; }
            public DateTime StartUtc { get; set; } // for sorting
            public string SessionId { get; set; }  // for admit actions
            public string EventId { get; set; }    // for admit actions
            public string Room { get; set; }       // Zoom / session room link
            public bool HasCheckin { get; set; }   // whether helper has a check-in recorded
            public bool CanUndoCheckin { get; set; } // undo availability window
            public string CheckedInAtLabel { get; set; } // local time display

            public bool HasParticipants { get; set; }           // enrolled participants present
            public List<ParticipantRow> Participants { get; set; } // enrolled participants

            public bool HasActiveInvite { get; set; }      // true if #1 participant has been invited/admitted
            public string ActiveParticipantId { get; set; } // the admitted participant id (the one the new buttons act on)

            public bool CanUndoParticipantAction { get; set; }
            public string UndoActionText { get; set; }
            public string UndoSnapshotId { get; set; }
        }

        /// <summary>
        /// Small DTO for enrolled participants shown in the card.
        /// </summary>
        private sealed class ParticipantRow
        {
            public string Name { get; set; }
            public bool Invited { get; set; } // true if admitted == true
        }

        [Serializable]
        private sealed class ParticipantActionUndoSnapshot
        {
            public string SnapshotId { get; set; }
            public string HelperId { get; set; }
            public string EventId { get; set; }
            public string SessionId { get; set; }
            public string ParticipantId { get; set; }
            public string ParticipantFirstName { get; set; }

            public string CourseId { get; set; }
            public string CourseTitle { get; set; }
            public string ActionType { get; set; } // complete | missing
            public string SessionOuterXmlBefore { get; set; }

            public bool ParticipantCompletionCreated { get; set; }
            public string MissingRecordId { get; set; }

            public int PreviousCourseTeachingSessions { get; set; }
            public int PreviousTotalTeachingSessions { get; set; }

            public DateTime CreatedUtc { get; set; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            // Always enforce Helper role on load
            if (!EnsureHelperRole(out var userId))
            {
                return;
            }

            if (!IsPostBack)
            {
                GetHelperIdentity(userId, out var email, out var fullName, out var firstName);
                BindSessions(email, fullName, firstName, userId);
            }
        }

        /// <summary>
        /// Ensures only Helpers reach this page.
        /// Returns false and redirects if the role or user id is missing/invalid.
        /// </summary>
        private bool EnsureHelperRole(out string userId)
        {
            userId = Session["UserId"] as string ?? "";

            var roleRaw = Session["Role"] as string ?? "";
            if (!string.Equals(roleRaw.Trim(), "Helper", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(userId))
            {
                Response.Redirect("~/Account/Login.aspx");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Reads users.xml and returns the helper's email, full name, and first name.
        /// We use these to match sessions that may store the helper as an email,
        /// full display name, or just first name (older data).
        /// </summary>
        private void GetHelperIdentity(string userId, out string email, out string fullName, out string firstName)
        {
            email = "";
            fullName = "";
            firstName = "";

            try
            {
                if (File.Exists(UsersXmlPath))
                {
                    var doc = new XmlDocument();
                    doc.Load(UsersXmlPath);

                    var node = doc.SelectSingleNode($"/users/user[@id='{userId}']") as XmlElement;
                    if (node != null)
                    {
                        firstName = (node["firstName"]?.InnerText ?? "").Trim();
                        var lastName = (node["lastName"]?.InnerText ?? "").Trim();
                        email = (node["email"]?.InnerText ?? "").Trim();

                        fullName = (firstName + " " + lastName).Trim();
                    }
                }
            }
            catch
            {
                // If something goes wrong, we fall back to session-based info if available later.
            }

            // Basic fallbacks so we always have some keys to try.
            var sessionEmail = Session["Email"] as string ?? "";
            if (string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(sessionEmail))
            {
                email = sessionEmail.Trim();
            }

            if (string.IsNullOrWhiteSpace(fullName))
            {
                fullName = (Session["DisplayName"] as string ?? "").Trim();
            }
        }

        /// <summary>
        /// Binds all sessions where the &lt;helper&gt; node matches the current helper
        /// (by email, full name, or first name), and decorates each row with
        /// session room + helper check-in state + enrolled participants.
        /// </summary>
        private void BindSessions(string email, string fullName, string firstName, string helperId)
        {
            var rows = new List<SessionRow>();

            if (!File.Exists(EventSessionsXmlPath))
            {
                NoSessionsPH.Visible = true;
                SessionsRepeater.DataSource = rows;
                SessionsRepeater.DataBind();
                return;
            }

            // Build courseId -> title map from microcourses.xml (best-effort).
            var courseTitles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (File.Exists(MicrocoursesXmlPath))
            {
                try
                {
                    var md = new XmlDocument();
                    md.Load(MicrocoursesXmlPath);

                    foreach (XmlElement c in md.SelectNodes("/microcourses/course"))
                    {
                        var id = c.GetAttribute("id");
                        if (string.IsNullOrEmpty(id)) continue;

                        var title = c["title"]?.InnerText ?? "(untitled)";
                        courseTitles[id] = title;
                    }
                }
                catch
                {
                    // If microcourses fail to load, we just fall back to "(untitled)" later.
                }
            }

            XmlDocument checkinsDoc = null;
            try
            {
                checkinsDoc = LoadHelperCheckinsDoc();
            }
            catch
            {
                // If helper checkins file cannot be read, we just skip check-in state.
            }

            // Load enrollments.xml (read-only) to show currently enrolled participants.
            XmlDocument enrollmentsDoc = null;
            if (File.Exists(EnrollmentsXmlPath))
            {
                try
                {
                    enrollmentsDoc = new XmlDocument();
                    enrollmentsDoc.Load(EnrollmentsXmlPath);
                }
                catch
                {
                    enrollmentsDoc = null;
                }
            }

            // Load all sessions and filter to those assigned to this helper.
            var doc = new XmlDocument();
            doc.Load(EventSessionsXmlPath);

            foreach (XmlElement s in doc.SelectNodes("/eventSessions/session"))
            {
                var helperVal = (s["helper"]?.InnerText ?? "").Trim();
                if (string.IsNullOrWhiteSpace(helperVal))
                {
                    continue;
                }

                if (!HelperMatches(helperVal, email, fullName, firstName))
                {
                    continue;
                }

                var eventId = s.GetAttribute("eventId");
                var sessionId = s.GetAttribute("id");

                var courseId = s["courseId"]?.InnerText ?? "";
                var startIso = s["start"]?.InnerText ?? "";
                var endIso = s["end"]?.InnerText ?? "";
                var capRaw = (s["capacity"]?.InnerText ?? "").Trim();
                var room = s["room"]?.InnerText ?? "";

                if (!TryParseIsoUtc(startIso, out var startUtc) ||
                    !TryParseIsoUtc(endIso, out var endUtc))
                {
                    // Skip malformed or empty times; schedule view should be clean.
                    continue;
                }

                var localStart = startUtc.ToLocalTime();
                var localEnd = endUtc.ToLocalTime();

                // Hide sessions 30 minutes after they end
                if (DateTime.UtcNow > endUtc.AddMinutes(30))
                {
                    continue;
                }

                string title;
                if (!string.IsNullOrEmpty(courseId) && courseTitles.TryGetValue(courseId, out var t))
                {
                    title = t;
                }
                else
                {
                    title = "(untitled microcourse)";
                }

                // Example: "Thu, Nov 6 2025 • 5:00–5:30 PM"
                var dayPart = localStart.ToString("ddd, MMM d yyyy", CultureInfo.CurrentCulture);
                var timeStart = localStart.ToString("h:mm tt", CultureInfo.CurrentCulture);
                var timeEnd = localEnd.ToString("h:mm tt", CultureInfo.CurrentCulture);
                var dayTime = $"{dayPart} • {timeStart}–{timeEnd}";

                var capacity = string.IsNullOrWhiteSpace(capRaw) ? "—" : capRaw;

                bool hasCheckin = false;
                bool canUndo = false;
                string checkedLabel = null;

                if (!string.IsNullOrWhiteSpace(helperId) && checkinsDoc != null)
                {
                    GetCheckinState(checkinsDoc, helperId, sessionId, out hasCheckin, out canUndo, out checkedLabel);
                }

                // Enrolled participants for this session.
                var participants = GetParticipantsForSession(enrollmentsDoc, eventId, sessionId);
                var hasParticipants = participants != null && participants.Count > 0;

                // Active invited participant = the first enrolled user who has admitted="true"
                string activeParticipantId = null;
                bool hasActiveInvite = false;

                if (enrollmentsDoc != null)
                {
                    var sesNode = enrollmentsDoc.SelectSingleNode(
                        $"/enrollments/session[@eventId='{eventId}' and @id='{sessionId}']") as XmlElement;

                    if (sesNode != null)
                    {
                        var activeNode = sesNode.SelectSingleNode("enrolled/user[@admitted='true'][1]") as XmlElement;
                        if (activeNode != null)
                        {
                            activeParticipantId = activeNode.GetAttribute("id");
                            hasActiveInvite = !string.IsNullOrWhiteSpace(activeParticipantId);
                        }
                    }
                }

                var undoSnap = GetLatestParticipantUndoSnapshot(helperId, eventId, sessionId);

                var canUndoParticipantAction = undoSnap != null;
                var undoActionText = string.Empty;
                var undoSnapshotId = string.Empty;

                if (undoSnap != null)
                {
                    undoSnapshotId = undoSnap.SnapshotId;

                    var participantFirstName = string.IsNullOrWhiteSpace(undoSnap.ParticipantFirstName)
                        ? "Participant"
                        : undoSnap.ParticipantFirstName.Trim();

                    undoActionText =
                        string.Equals(undoSnap.ActionType, "missing", StringComparison.OrdinalIgnoreCase)
                            ? participantFirstName + " was marked missing. Undo available for 2 minutes."
                            : participantFirstName + " was marked complete. Undo available for 2 minutes.";
                }

                rows.Add(new SessionRow
                {
                    CourseTitle = title,
                    DayTime = dayTime,
                    Capacity = capacity,
                    StartUtc = startUtc,
                    SessionId = sessionId,
                    EventId = eventId,
                    Room = room,
                    HasCheckin = hasCheckin,
                    CanUndoCheckin = canUndo,
                    CheckedInAtLabel = checkedLabel,
                    HasParticipants = hasParticipants,
                    Participants = participants,
                    HasActiveInvite = hasActiveInvite,
                    ActiveParticipantId = activeParticipantId,
                    CanUndoParticipantAction = canUndoParticipantAction,
                    UndoActionText = undoActionText,
                    UndoSnapshotId = undoSnapshotId
                });
            }

            // Sort by start time ascending so upcoming sessions read top-to-bottom.
            rows.Sort((a, b) => a.StartUtc.CompareTo(b.StartUtc));

            NoSessionsPH.Visible = rows.Count == 0;
            SessionsRepeater.DataSource = rows;
            SessionsRepeater.DataBind();
        }

        /// <summary>
        /// Matches the stored helper string from eventSessions.xml against
        /// the current helper's email, full name, or first name (case-insensitive).
        /// This supports both new and older styles of storing helper identifiers.
        /// </summary>
        private static bool HelperMatches(string storedHelper, string email, string fullName, string firstName)
        {
            if (string.IsNullOrWhiteSpace(storedHelper))
            {
                return false;
            }

            var val = storedHelper.Trim();

            bool EqualsIgnoreCase(string a, string b)
                => !string.IsNullOrWhiteSpace(a) &&
                   !string.IsNullOrWhiteSpace(b) &&
                   string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);

            if (EqualsIgnoreCase(val, email)) return true;
            if (EqualsIgnoreCase(val, fullName)) return true;
            if (EqualsIgnoreCase(val, firstName)) return true;

            return false;
        }



        /// <summary>
        /// Parses an ISO-8601 "o" format string that is expected to be UTC,
        /// returning a DateTime in UTC on success.
        /// </summary>
        private static bool TryParseIsoUtc(string iso, out DateTime utc)
        {
            return DateTime.TryParseExact(
                iso,
                "o",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                out utc);
        }

        protected void SessionsRepeater_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            var arg = Convert.ToString(e.CommandArgument ?? string.Empty);

            // Ensure helper is valid first
            if (!EnsureHelperRole(out var helperId))
            {
                return;
            }

            string email, fullName, firstName;
            GetHelperIdentity(helperId, out email, out fullName, out firstName);

            // Special-case undo first because its CommandArgument is only the snapshot id
            if (string.Equals(e.CommandName, "undoParticipantAction", StringComparison.OrdinalIgnoreCase))
            {
                var snapshotId = arg;

                try
                {
                    string undoneType;
                    if (TryUndoParticipantAction(helperId, snapshotId, out undoneType))
                    {
                        var message = string.Equals(undoneType, "missing", StringComparison.OrdinalIgnoreCase)
                            ? "Missing action undone. The roster has been restored."
                            : "Completed-session action undone. The roster and teaching progress were restored.";

                        ScriptManager.RegisterStartupScript(
                            this, GetType(),
                            "UndoParticipantAction",
                            "showFiaToast('" + message.Replace("'", "\\'") + "');",
                            true);
                    }
                    else
                    {
                        ScriptManager.RegisterStartupScript(
                            this, GetType(),
                            "UndoParticipantActionFail",
                            "showFiaToast('Undo is no longer available for that action.');",
                            true);
                    }
                }
                catch
                {
                }
                finally
                {
                    BindSessions(email, fullName, firstName, helperId);
                }

                return;
            }

            var parts = arg.Split('|');
            if (parts.Length < 2)
                return;

            var eventId = parts[0];
            var sessionId = parts[1];

            if (string.IsNullOrWhiteSpace(eventId) || string.IsNullOrWhiteSpace(sessionId))
                return;

            var courseTitleForLog = GetCourseTitleForSession(eventId, sessionId);

            var helperName =
                !string.IsNullOrWhiteSpace(fullName) ? fullName :
                !string.IsNullOrWhiteSpace(email) ? email :
                helperId;

            try
            {
                if (string.Equals(e.CommandName, "admit", StringComparison.OrdinalIgnoreCase))
                {
                    AdmitParticipants(eventId, sessionId);

                    try
                    {
                        var details = string.Format(
                            "Helper admitted enrolled participants and sent the room link for session \"{0}\" (eventId={1}, sessionId={2}).",
                            courseTitleForLog,
                            eventId,
                            sessionId);

                        UniversityAuditLogger.AppendForCurrentUser(
                            this,
                            "Helper Admit Participants",
                            details);
                    }
                    catch
                    {
                    }

                    ScriptManager.RegisterStartupScript(
                        this, GetType(),
                        "AdmitConfirm",
                        "showFiaToast('Participants were sent the room link.');",
                        true);
                }
                else if (string.Equals(e.CommandName, "completeParticipant", StringComparison.OrdinalIgnoreCase))
                {
                    if (parts.Length != 3) return;
                    var participantId = parts[2];

                    string completedCourseTitle;
                    if (CompleteWithParticipant(eventId, sessionId, participantId, helperId, out completedCourseTitle))
                    {
                        ScriptManager.RegisterStartupScript(
                            this, GetType(),
                            "CompleteParticipant",
                            "showFiaToast('Participant marked complete. Teaching progress was logged automatically. You can undo this for 2 minutes.');",
                            true);
                    }
                }
                else if (string.Equals(e.CommandName, "markMissingParticipant", StringComparison.OrdinalIgnoreCase))
                {
                    if (parts.Length != 3) return;
                    var participantId = parts[2];

                    if (MarkParticipantMissing(eventId, sessionId, participantId, helperId))
                    {
                        ScriptManager.RegisterStartupScript(
                            this, GetType(),
                            "MissingParticipant",
                            "showFiaToast('Participant marked missing. You can undo this for 2 minutes.');",
                            true);
                    }
                }
                else if (string.Equals(e.CommandName, "checkinHelper", StringComparison.OrdinalIgnoreCase))
                {
                    RecordHelperCheckin(helperId, helperName, eventId, sessionId);

                    try
                    {
                        var details = string.Format(
                            "Helper checked in for session \"{0}\" (eventId={1}, sessionId={2}).",
                            courseTitleForLog,
                            eventId,
                            sessionId);

                        UniversityAuditLogger.AppendForCurrentUser(
                            this,
                            "Helper Check In",
                            details);
                    }
                    catch
                    {
                    }

                    ScriptManager.RegisterStartupScript(
                        this, GetType(),
                        "HelperCheckin",
                        "showFiaToast('You are checked in for this session. You can undo this for a few minutes.');",
                        true);
                }
                else if (string.Equals(e.CommandName, "undoCheckinHelper", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryUndoHelperCheckin(helperId, eventId, sessionId))
                    {
                        try
                        {
                            var details = string.Format(
                                "Helper undid their check-in for session \"{0}\" (eventId={1}, sessionId={2}).",
                                courseTitleForLog,
                                eventId,
                                sessionId);

                            UniversityAuditLogger.AppendForCurrentUser(
                                this,
                                "Helper Check In (Undo)",
                                details);
                        }
                        catch
                        {
                        }

                        ScriptManager.RegisterStartupScript(
                            this, GetType(),
                            "UndoHelperCheckin",
                            "showFiaToast('Your check-in has been undone.');",
                            true);
                    }
                }
            }
            catch
            {
            }
            finally
            {
                BindSessions(email, fullName, firstName, helperId);
            }
        }

        private XmlDocument LoadEnrollmentsDoc()
        {
            var doc = new XmlDocument();

            if (File.Exists(EnrollmentsXmlPath))
            {
                doc.Load(EnrollmentsXmlPath);
                if (doc.DocumentElement == null)
                {
                    var rootMissing = doc.CreateElement("enrollments");
                    doc.AppendChild(rootMissing);
                }
            }
            else
            {
                var root = doc.CreateElement("enrollments");
                doc.AppendChild(root);
            }

            return doc;
        }

        private void SaveEnrollmentsDoc(XmlDocument doc)
        {
            lock (EnrollmentsLock)
            {
                doc.Save(EnrollmentsXmlPath);
            }
        }

        /// <summary>
        /// Mark all currently enrolled users in a session as admitted so
        /// their "My Sessions" cards can show the Join Room link.
        /// </summary>
        private void AdmitParticipants(string eventId, string sessionId)
        {
            var doc = LoadEnrollmentsDoc();
            var ses = doc.SelectSingleNode($"/enrollments/session[@eventId='{eventId}' and @id='{sessionId}']") as XmlElement;
            if (ses == null)
                return;

            var nowIso = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);

            // Admit ONLY the first enrolled participant (1-based XPath index).
            var first = ses.SelectSingleNode("enrolled/user[1]") as XmlElement;
            if (first == null)
            {
                // No enrolled participants to admit.
                return;
            }

            first.SetAttribute("admitted", "true");
            first.SetAttribute("admittedAt", nowIso);

            SaveEnrollmentsDoc(doc);
        }

        private void EnsureXmlDoc(string path, string rootName)
        {
            if (File.Exists(path)) return;
            var doc = new XmlDocument();
            doc.AppendChild(doc.CreateElement(rootName));
            doc.Save(path);
        }

       

        private void AwardBadgeIfFirstCompletion(string userId, string courseId, string courseTitle)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(courseTitle))
                return;

            var titleKey = NormalizeTitle(courseTitle);

            if (!BadgeImageByCourseTitle.TryGetValue(titleKey, out var imagePath) || string.IsNullOrWhiteSpace(imagePath))
                return; // no badge configured for this title

            lock (ParticipantBadgesLock)
            {
                EnsureXmlDoc(ParticipantBadgesXmlPath, "participantBadges");

                var doc = new XmlDocument();
                doc.Load(ParticipantBadgesXmlPath);

                var root = doc.DocumentElement;
                if (root == null)
                {
                    root = doc.CreateElement("participantBadges");
                    root.SetAttribute("version", "1");
                    doc.AppendChild(root);
                }

                var user = doc.SelectSingleNode($"/participantBadges/user[@id='{userId}']") as XmlElement;
                if (user == null)
                {
                    user = doc.CreateElement("user");
                    user.SetAttribute("id", userId);
                    root.AppendChild(user);
                }

                // Prevent duplicates: one badge per course (prefer courseId, fallback to title)
                XmlElement existing = null;
                if (!string.IsNullOrWhiteSpace(courseId))
                    existing = user.SelectSingleNode($"badge[@courseId='{courseId}']") as XmlElement;

                if (existing == null)
                    existing = user.SelectSingleNode($"badge[@courseTitle='{titleKey}']") as XmlElement;

                if (existing != null)
                    return;

                var badge = doc.CreateElement("badge");
                if (!string.IsNullOrWhiteSpace(courseId))
                    badge.SetAttribute("courseId", courseId);

                badge.SetAttribute("courseTitle", titleKey);
                badge.SetAttribute("image", imagePath);
                badge.SetAttribute("awardedOnUtc", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));

                user.AppendChild(badge);
                doc.Save(ParticipantBadgesXmlPath);
            }
        }

        private string GetCourseIdForSession(string eventId, string sessionId)
        {
            try
            {
                if (!File.Exists(EventSessionsXmlPath)) return null;
                var doc = new XmlDocument();
                doc.Load(EventSessionsXmlPath);

                var ses = doc.SelectSingleNode($"/eventSessions/session[@eventId='{eventId}' and @id='{sessionId}']") as XmlElement;
                return ses?["courseId"]?.InnerText?.Trim();
            }
            catch
            {
                return null;
            }
        }

        private bool MarkCourseCompletedForUser(string userId, string courseId, string courseTitle)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(courseId))
                return false;

            bool created = false;

            lock (CompletionsLock)
            {
                EnsureXmlDoc(CompletionsXmlPath, "completions");
                var doc = new XmlDocument();
                doc.Load(CompletionsXmlPath);

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

                    if (!string.IsNullOrWhiteSpace(courseTitle))
                        c.SetAttribute("title", courseTitle.Trim());

                    c.SetAttribute("completedOn", DateTime.UtcNow.ToString("yyyy-MM-dd"));
                    user.AppendChild(c);
                    AwardBadgeIfFirstCompletion(userId, courseId, courseTitle);
                    created = true;
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(courseTitle) && !existing.HasAttribute("title"))
                        existing.SetAttribute("title", courseTitle.Trim());

                    if (!existing.HasAttribute("completedOn"))
                        existing.SetAttribute("completedOn", DateTime.UtcNow.ToString("yyyy-MM-dd"));
                }

                doc.Save(CompletionsXmlPath);
            }

            return created;
        }

        private void RemoveCourseCompletionForUser(string userId, string courseId)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(courseId))
                return;

            lock (CompletionsLock)
            {
                EnsureXmlDoc(CompletionsXmlPath, "completions");
                var doc = new XmlDocument();
                doc.Load(CompletionsXmlPath);

                var courseNode = doc.SelectSingleNode($"/completions/user[@id='{userId}']/course[@id='{courseId}']") as XmlElement;
                if (courseNode != null && courseNode.ParentNode != null)
                {
                    var userNode = courseNode.ParentNode as XmlElement;
                    courseNode.ParentNode.RemoveChild(courseNode);

                    if (userNode != null && !userNode.HasChildNodes)
                    {
                        userNode.ParentNode?.RemoveChild(userNode);
                    }

                    doc.Save(CompletionsXmlPath);
                }
            }
        }



        private string AppendMissingRecord(string eventId, string sessionId, string participantId, string helperId)
        {
            lock (MissingLock)
            {
                EnsureXmlDoc(MissingParticipantSessionsXmlPath, "missingParticipantSessions");

                var doc = new XmlDocument();
                doc.Load(MissingParticipantSessionsXmlPath);

                var root = doc.DocumentElement;
                if (root == null)
                {
                    root = doc.CreateElement("missingParticipantSessions");
                    doc.AppendChild(root);
                }

                var id = Guid.NewGuid().ToString("N");

                var miss = doc.CreateElement("missing");
                miss.SetAttribute("id", id);
                miss.SetAttribute("eventId", eventId ?? "");
                miss.SetAttribute("sessionId", sessionId ?? "");
                miss.SetAttribute("participantId", participantId ?? "");
                miss.SetAttribute("helperId", helperId ?? "");
                miss.SetAttribute("tsUtc", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));

                root.AppendChild(miss);
                doc.Save(MissingParticipantSessionsXmlPath);

                return id;
            }
        }

        private void RemoveMissingRecordById(string missingId)
        {
            if (string.IsNullOrWhiteSpace(missingId))
                return;

            lock (MissingLock)
            {
                if (!File.Exists(MissingParticipantSessionsXmlPath))
                    return;

                var doc = new XmlDocument();
                doc.Load(MissingParticipantSessionsXmlPath);

                var node = doc.SelectSingleNode($"/missingParticipantSessions/missing[@id='{missingId}']") as XmlElement;
                if (node != null && node.ParentNode != null)
                {
                    node.ParentNode.RemoveChild(node);
                    doc.Save(MissingParticipantSessionsXmlPath);
                }
            }
        }

        private void ClearAdmittedFlags(XmlElement sessionNode)
        {
            if (sessionNode == null) return;
            foreach (XmlElement u in sessionNode.SelectNodes("enrolled/user"))
            {
                if (u.HasAttribute("admitted")) u.RemoveAttribute("admitted");
                if (u.HasAttribute("admittedAt")) u.RemoveAttribute("admittedAt");
            }
        }

        private void PromoteFirstWaitlistToEnrolled(XmlDocument doc, XmlElement sessionNode)
        {
            if (doc == null || sessionNode == null) return;

            var firstWait = sessionNode.SelectSingleNode("waitlist/user[1]") as XmlElement;
            if (firstWait == null) return;

            var moved = doc.CreateElement("user");
            moved.SetAttribute("id", firstWait.GetAttribute("id"));
            moved.SetAttribute("ts", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));

            var enrolled = sessionNode.SelectSingleNode("enrolled") as XmlElement;
            enrolled?.AppendChild(moved);

            firstWait.ParentNode.RemoveChild(firstWait);
        }

        private bool CompleteWithParticipant(string eventId, string sessionId, string participantId, string helperId, out string courseTitle)
        {
            courseTitle = null;

            if (string.IsNullOrWhiteSpace(eventId) ||
                string.IsNullOrWhiteSpace(sessionId) ||
                string.IsNullOrWhiteSpace(participantId) ||
                string.IsNullOrWhiteSpace(helperId))
                return false;

            lock (ParticipantSessionActionLock)
            {
                var enrollmentsDoc = LoadEnrollmentsDoc();
                var ses = enrollmentsDoc.SelectSingleNode($"/enrollments/session[@eventId='{eventId}' and @id='{sessionId}']") as XmlElement;
                if (ses == null) return false;

                var enrolledNode = ses.SelectSingleNode($"enrolled/user[@id='{participantId}']") as XmlElement;
                if (enrolledNode == null) return false;

                var snapshot = new ParticipantActionUndoSnapshot
                {
                    SnapshotId = Guid.NewGuid().ToString("N"),
                    HelperId = helperId,
                    EventId = eventId,
                    SessionId = sessionId,
                    ParticipantId = participantId,
                    ParticipantFirstName = GetParticipantFirstNameForUndo(enrolledNode),
                    ActionType = "complete",
                    SessionOuterXmlBefore = ses.OuterXml,
                    CreatedUtc = DateTime.UtcNow
                };

                var courseId = GetCourseIdForSession(eventId, sessionId);
                snapshot.CourseId = courseId ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(courseId))
                {
                    courseTitle = GetCourseTitleById(courseId);
                    snapshot.CourseTitle = courseTitle ?? string.Empty;
                }

                enrolledNode.ParentNode.RemoveChild(enrolledNode);
                PromoteFirstWaitlistToEnrolled(enrollmentsDoc, ses);
                ClearAdmittedFlags(ses);
                SaveEnrollmentsDoc(enrollmentsDoc);

                if (!string.IsNullOrWhiteSpace(courseId))
                {
                    snapshot.ParticipantCompletionCreated = MarkCourseCompletedForUser(participantId, courseId, courseTitle);

                    int prevCourseTeaching;
                    int prevTotalTeaching;
                    string incrementTitle;
                    if (TryIncrementTeachingProgressForCourse(helperId, courseId, out incrementTitle, out prevCourseTeaching, out prevTotalTeaching))
                    {
                        snapshot.PreviousCourseTeachingSessions = prevCourseTeaching;
                        snapshot.PreviousTotalTeachingSessions = prevTotalTeaching;

                        if (string.IsNullOrWhiteSpace(courseTitle))
                        {
                            courseTitle = incrementTitle;
                            snapshot.CourseTitle = incrementTitle ?? string.Empty;
                        }
                    }

                    try
                    {
                        UniversityAuditLogger.AppendForCurrentUser(
                            this,
                            "Helper Delivered Session",
                            $"Helper logged a delivered teaching session for \"{(courseTitle ?? courseId)}\" (courseId={courseId}) automatically after marking a participant session complete.");
                    }
                    catch { }
                }

                AddParticipantUndoSnapshot(snapshot);

                try
                {
                    UniversityAuditLogger.AppendForCurrentUser(
                        this,
                        "Helper Complete With Participant",
                        $"Helper marked participant complete (participantId={participantId}) for session (eventId={eventId}, sessionId={sessionId}).");
                }
                catch { }

                return true;
            }
        }

        // Add this method to the same class where you're calling it (e.g., Helper/Schedule.aspx.cs
        // or Participant/Home.aspx.cs). It reads microcourses.xml and returns the <title> for a course id.
        //
        // Requires: using System.Xml; using System.IO;

        private string GetCourseTitleById(string courseId)
        {
            if (string.IsNullOrWhiteSpace(courseId))
                return null;

            if (!File.Exists(MicrocoursesXmlPath))
                return null;

            try
            {
                var doc = new XmlDocument();
                doc.Load(MicrocoursesXmlPath);

                var node = doc.SelectSingleNode($"/microcourses/course[@id='{courseId}']") as XmlElement;
                if (node == null)
                    return null;

                var title = (node["title"]?.InnerText ?? "").Trim();
                return string.IsNullOrWhiteSpace(title) ? null : title;
            }
            catch
            {
                return null;
            }
        }

        private bool MarkParticipantMissing(string eventId, string sessionId, string participantId, string helperId)
        {
            if (string.IsNullOrWhiteSpace(eventId) ||
                string.IsNullOrWhiteSpace(sessionId) ||
                string.IsNullOrWhiteSpace(participantId) ||
                string.IsNullOrWhiteSpace(helperId))
                return false;

            lock (ParticipantSessionActionLock)
            {
                var enrollmentsDoc = LoadEnrollmentsDoc();
                var ses = enrollmentsDoc.SelectSingleNode($"/enrollments/session[@eventId='{eventId}' and @id='{sessionId}']") as XmlElement;
                if (ses == null) return false;

                var enrolledNode = ses.SelectSingleNode($"enrolled/user[@id='{participantId}']") as XmlElement;
                if (enrolledNode == null) return false;

                var snapshot = new ParticipantActionUndoSnapshot
                {
                    SnapshotId = Guid.NewGuid().ToString("N"),
                    HelperId = helperId,
                    EventId = eventId,
                    SessionId = sessionId,
                    ParticipantId = participantId,
                    ParticipantFirstName = GetParticipantFirstNameForUndo(enrolledNode),
                    ActionType = "missing",
                    SessionOuterXmlBefore = ses.OuterXml,
                    CreatedUtc = DateTime.UtcNow
                };

                snapshot.MissingRecordId = AppendMissingRecord(eventId, sessionId, participantId, helperId);

                enrolledNode.ParentNode.RemoveChild(enrolledNode);
                PromoteFirstWaitlistToEnrolled(enrollmentsDoc, ses);
                ClearAdmittedFlags(ses);
                SaveEnrollmentsDoc(enrollmentsDoc);

                AddParticipantUndoSnapshot(snapshot);

                try
                {
                    UniversityAuditLogger.AppendForCurrentUser(
                        this,
                        "Helper Mark Participant Missing",
                        $"Helper marked participant missing (participantId={participantId}) for session (eventId={eventId}, sessionId={sessionId}).");
                }
                catch { }

                return true;
            }
        }

        private bool TryUndoParticipantAction(string helperId, string snapshotId, out string actionType)
        {
            actionType = string.Empty;

            if (string.IsNullOrWhiteSpace(helperId) || string.IsNullOrWhiteSpace(snapshotId))
                return false;

            lock (ParticipantSessionActionLock)
            {
                var snapshot = GetParticipantUndoSnapshotById(helperId, snapshotId);
                if (snapshot == null)
                    return false;

                if (DateTime.UtcNow - snapshot.CreatedUtc > ParticipantActionUndoWindow)
                {
                    RemoveParticipantUndoSnapshot(helperId, snapshotId);
                    return false;
                }

                actionType = snapshot.ActionType ?? string.Empty;

                var enrollmentsDoc = LoadEnrollmentsDoc();
                var currentSession = enrollmentsDoc.SelectSingleNode($"/enrollments/session[@eventId='{snapshot.EventId}' and @id='{snapshot.SessionId}']") as XmlElement;
                if (currentSession == null)
                    return false;

                var temp = new XmlDocument();
                temp.LoadXml(snapshot.SessionOuterXmlBefore);

                var restoredSession = enrollmentsDoc.ImportNode(temp.DocumentElement, true);
                currentSession.ParentNode.ReplaceChild(restoredSession, currentSession);
                SaveEnrollmentsDoc(enrollmentsDoc);

                if (string.Equals(snapshot.ActionType, "complete", StringComparison.OrdinalIgnoreCase))
                {
                    if (snapshot.ParticipantCompletionCreated && !string.IsNullOrWhiteSpace(snapshot.CourseId))
                    {
                        RemoveCourseCompletionForUser(snapshot.ParticipantId, snapshot.CourseId);
                    }

                    if (!string.IsNullOrWhiteSpace(snapshot.CourseId))
                    {
                        RestoreTeachingProgressFromSnapshot(
                            helperId,
                            snapshot.CourseId,
                            snapshot.CourseTitle,
                            snapshot.PreviousCourseTeachingSessions,
                            snapshot.PreviousTotalTeachingSessions);
                    }

                    try
                    {
                        UniversityAuditLogger.AppendForCurrentUser(
                            this,
                            "Helper Complete With Participant (Undo)",
                            $"Helper undid a completed participant action for session (eventId={snapshot.EventId}, sessionId={snapshot.SessionId}, participantId={snapshot.ParticipantId}).");
                    }
                    catch { }
                }
                else if (string.Equals(snapshot.ActionType, "missing", StringComparison.OrdinalIgnoreCase))
                {
                    RemoveMissingRecordById(snapshot.MissingRecordId);

                    try
                    {
                        UniversityAuditLogger.AppendForCurrentUser(
                            this,
                            "Helper Mark Participant Missing (Undo)",
                            $"Helper undid a missing-participant action for session (eventId={snapshot.EventId}, sessionId={snapshot.SessionId}, participantId={snapshot.ParticipantId}).");
                    }
                    catch { }
                }

                RemoveParticipantUndoSnapshot(helperId, snapshotId);
                return true;
            }
        }

        // ---------- Helper check-ins storage ----------

        private XmlDocument LoadHelperCheckinsDoc()
        {
            var doc = new XmlDocument();

            if (File.Exists(HelperCheckinsXmlPath))
            {
                doc.Load(HelperCheckinsXmlPath);
                if (doc.DocumentElement == null)
                {
                    var rootMissing = doc.CreateElement("helperCheckins");
                    doc.AppendChild(rootMissing);
                }
            }
            else
            {
                var root = doc.CreateElement("helperCheckins");
                doc.AppendChild(root);
            }

            return doc;
        }

        private void SaveHelperCheckinsDoc(XmlDocument doc)
        {
            lock (HelperCheckinsLock)
            {
                doc.Save(HelperCheckinsXmlPath);
            }
        }

        /// <summary>
        /// Records a helper check-in for the given session with timestamp,
        /// helper id, helper name, event id, and session id.
        /// </summary>
        private void RecordHelperCheckin(string helperId, string helperName, string eventId, string sessionId)
        {
            if (string.IsNullOrWhiteSpace(helperId) || string.IsNullOrWhiteSpace(sessionId))
                return;

            var doc = LoadHelperCheckinsDoc();
            var root = doc.DocumentElement;
            if (root == null)
            {
                root = doc.CreateElement("helperCheckins");
                doc.AppendChild(root);
            }

            var node = doc.CreateElement("checkin");
            node.SetAttribute("helperId", helperId);
            node.SetAttribute("helperName", helperName ?? string.Empty);
            node.SetAttribute("eventId", eventId ?? string.Empty);
            node.SetAttribute("sessionId", sessionId ?? string.Empty);
            node.SetAttribute("tsUtc", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));

            root.AppendChild(node);
            SaveHelperCheckinsDoc(doc);
        }

        /// <summary>
        /// Attempts to undo the most recent helper check-in for this helper and session
        /// if it falls within the configured undo window.
        /// </summary>
        private bool TryUndoHelperCheckin(string helperId, string eventId, string sessionId)
        {
            if (string.IsNullOrWhiteSpace(helperId) || string.IsNullOrWhiteSpace(sessionId))
                return false;

            var doc = LoadHelperCheckinsDoc();
            var root = doc.DocumentElement;
            if (root == null)
                return false;

            XmlElement latest = null;
            DateTime latestTsUtc = default;
            foreach (XmlElement ck in doc.SelectNodes($"/helperCheckins/checkin[@helperId='{helperId}' and @sessionId='{sessionId}']"))
            {
                var tsRaw = ck.GetAttribute("tsUtc");
                if (!DateTime.TryParse(tsRaw, null, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var tsUtc))
                    continue;

                if (latest == null || tsUtc > latestTsUtc)
                {
                    latest = ck;
                    latestTsUtc = tsUtc;
                }
            }

            if (latest == null)
                return false;

            var age = DateTime.UtcNow - latestTsUtc;
            if (age > HelperCheckinUndoWindow)
            {
                // Undo window expired.
                return false;
            }

            root.RemoveChild(latest);
            SaveHelperCheckinsDoc(doc);
            return true;
        }

        /// <summary>
        /// Reads helper check-in state (has check-in, undo availability, display label)
        /// for the given helper and session from the helper check-ins document.
        /// </summary>
        private void GetCheckinState(
            XmlDocument doc,
            string helperId,
            string sessionId,
            out bool hasCheckin,
            out bool canUndo,
            out string label)
        {
            hasCheckin = false;
            canUndo = false;
            label = null;

            if (doc == null || doc.DocumentElement == null ||
                string.IsNullOrWhiteSpace(helperId) ||
                string.IsNullOrWhiteSpace(sessionId))
            {
                return;
            }

            XmlElement latest = null;
            DateTime latestTsUtc = default;

            foreach (XmlElement ck in doc.SelectNodes($"/helperCheckins/checkin[@helperId='{helperId}' and @sessionId='{sessionId}']"))
            {
                var tsRaw = ck.GetAttribute("tsUtc");
                if (!DateTime.TryParse(tsRaw, null, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var tsUtc))
                    continue;

                if (latest == null || tsUtc > latestTsUtc)
                {
                    latest = ck;
                    latestTsUtc = tsUtc;
                }
            }

            if (latest == null)
            {
                return;
            }

            hasCheckin = true;

            var local = DateTime.SpecifyKind(latestTsUtc, DateTimeKind.Utc).ToLocalTime();
            label = local.ToString("h:mm tt", CultureInfo.CurrentCulture);

            var age = DateTime.UtcNow - latestTsUtc;
            canUndo = age <= HelperCheckinUndoWindow;
        }

        /// <summary>
        /// Returns enrolled participants for a session with their invite status.
        /// Names are resolved from enrollments.xml attributes if present,
        /// otherwise we fall back to users.xml via userId / id.
        /// </summary>
        private List<ParticipantRow> GetParticipantsForSession(XmlDocument enrollmentsDoc, string eventId, string sessionId)
        {
            var result = new List<ParticipantRow>();

            if (enrollmentsDoc == null || enrollmentsDoc.DocumentElement == null ||
                string.IsNullOrWhiteSpace(sessionId))
            {
                return result;
            }

            var ses = enrollmentsDoc.SelectSingleNode($"/enrollments/session[@eventId='{eventId}' and @id='{sessionId}']") as XmlElement;
            if (ses == null)
            {
                return result;
            }

            // Load users.xml once to help resolve names from userId / id.
            XmlDocument usersDoc = null;
            if (File.Exists(UsersXmlPath))
            {
                try
                {
                    usersDoc = new XmlDocument();
                    usersDoc.Load(UsersXmlPath);
                }
                catch
                {
                    usersDoc = null;
                }
            }

            foreach (XmlElement u in ses.SelectNodes("enrolled/user"))
            {
                var name = ResolveParticipantName(u, usersDoc);

                var admittedAttr = u.GetAttribute("admitted");
                var invited = string.Equals(admittedAttr, "true", StringComparison.OrdinalIgnoreCase);

                result.Add(new ParticipantRow
                {
                    Name = name,
                    Invited = invited
                });
            }

            return result;
        }

        /// <summary>
        /// Resolves a participant's display name from the enrollment node,
        /// falling back to users.xml (firstName, displayName, or email)
        /// and finally to "Participant".
        /// </summary>
        private string ResolveParticipantName(XmlElement enrolledUser, XmlDocument usersDoc)
        {
            if (enrolledUser == null)
            {
                return "Participant";
            }

            // Try attributes directly on the enrollment <user> node.
            var firstAttr = enrolledUser.GetAttribute("firstName");
            if (!string.IsNullOrWhiteSpace(firstAttr))
            {
                return firstAttr;
            }

            var displayAttr = enrolledUser.GetAttribute("displayName");
            if (!string.IsNullOrWhiteSpace(displayAttr))
            {
                return displayAttr;
            }

            var nameAttr = enrolledUser.GetAttribute("name");
            if (!string.IsNullOrWhiteSpace(nameAttr))
            {
                return nameAttr;
            }

            // In enrollments.xml the user is stored as either userId="..." or id="..."
            var userId = enrolledUser.GetAttribute("userId");
            if (string.IsNullOrWhiteSpace(userId))
            {
                userId = enrolledUser.GetAttribute("id");
            }

            if (!string.IsNullOrWhiteSpace(userId) && usersDoc != null)
            {
                try
                {
                    var node = usersDoc.SelectSingleNode($"/users/user[@id='{userId}']") as XmlElement;
                    if (node != null)
                    {
                        var firstNode = (node["firstName"]?.InnerText ?? "").Trim();
                        if (!string.IsNullOrWhiteSpace(firstNode))
                        {
                            return firstNode;
                        }

                        var displayNode = (node["displayName"]?.InnerText ?? "").Trim();
                        if (!string.IsNullOrWhiteSpace(displayNode))
                        {
                            return displayNode;
                        }

                        var emailNode = (node["email"]?.InnerText ?? "").Trim();
                        if (!string.IsNullOrWhiteSpace(emailNode))
                        {
                            return emailNode;
                        }
                    }
                }
                catch
                {
                    // If users.xml lookup fails, fall through to generic label.
                }
            }

            return "Participant";
        }

        private string GetParticipantFirstNameForUndo(XmlElement enrolledUser)
        {
            XmlDocument usersDoc = null;

            if (File.Exists(UsersXmlPath))
            {
                try
                {
                    usersDoc = new XmlDocument();
                    usersDoc.Load(UsersXmlPath);
                }
                catch
                {
                    usersDoc = null;
                }
            }

            var display = ResolveParticipantName(enrolledUser, usersDoc);
            if (string.IsNullOrWhiteSpace(display))
                return "Participant";

            var parts = display.Trim()
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            return parts.Length > 0 ? parts[0] : "Participant";
        }

        /// <summary>
        /// Best-effort lookup of the microcourse title for a given event/session.
        /// Used only for audit logging; never blocks helper flows.
        /// </summary>
        private string GetCourseTitleForSession(string eventId, string sessionId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(eventId) || string.IsNullOrWhiteSpace(sessionId))
                {
                    return "(session)";
                }

                if (!File.Exists(EventSessionsXmlPath))
                {
                    return "(session)";
                }

                var doc = new XmlDocument();
                doc.Load(EventSessionsXmlPath);

                var ses = doc.SelectSingleNode(
                    $"/eventSessions/session[@eventId='{eventId}' and @id='{sessionId}']") as XmlElement;
                if (ses == null)
                {
                    return "(session)";
                }

                var courseId = (ses["courseId"]?.InnerText ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(courseId))
                {
                    return "(session)";
                }

                if (!File.Exists(MicrocoursesXmlPath))
                {
                    return courseId;
                }

                var mdoc = new XmlDocument();
                mdoc.Load(MicrocoursesXmlPath);

                var courseEl = mdoc.SelectSingleNode(
                    $"/microcourses/course[@id='{courseId}']") as XmlElement;
                var title = courseEl?["title"]?.InnerText;

                title = (title ?? string.Empty).Trim();
                return string.IsNullOrWhiteSpace(title) ? courseId : title;
            }
            catch
            {
                // Audit logging should never break main helper flows.
                return "(session)";
            }
        }




        // ---------- New: helper progress + notes for delivered sessions ----------

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

        private bool TryIncrementTeachingProgressForCourse(
    string helperId,
    string courseId,
    out string courseTitle,
    out int previousCourseTeaching,
    out int previousTotalTeaching)
        {
            courseTitle = null;
            previousCourseTeaching = 0;
            previousTotalTeaching = 0;

            if (string.IsNullOrWhiteSpace(helperId) || string.IsNullOrWhiteSpace(courseId))
                return false;

            var doc = LoadHelperProgressDoc();
            var helperNode = doc.SelectSingleNode($"//helper[@id='{helperId}']") as XmlElement;
            if (helperNode == null)
                return false;

            var courseNode = helperNode.SelectSingleNode($"course[@id='{courseId}']") as XmlElement;
            if (courseNode == null)
                return false;

            courseTitle = (courseNode["title"]?.InnerText ?? "").Trim();

            previousCourseTeaching = ParseIntSafe(courseNode["teachingSessions"]?.InnerText);

            var totalsNode = helperNode.SelectSingleNode("totals") as XmlElement;
            previousTotalTeaching = totalsNode != null
                ? ParseIntSafe(totalsNode["totalTeachingSessions"]?.InnerText)
                : 0;

            var teachingEl = courseNode["teachingSessions"] ?? doc.CreateElement("teachingSessions");
            if (teachingEl.ParentNode == null) courseNode.AppendChild(teachingEl);
            teachingEl.InnerText = (previousCourseTeaching + 1).ToString(CultureInfo.InvariantCulture);

            if (totalsNode == null)
            {
                totalsNode = doc.CreateElement("totals");
                helperNode.AppendChild(totalsNode);
            }

            var totalTeachEl = totalsNode["totalTeachingSessions"] ?? doc.CreateElement("totalTeachingSessions");
            if (totalTeachEl.ParentNode == null) totalsNode.AppendChild(totalTeachEl);
            totalTeachEl.InnerText = (previousTotalTeaching + 1).ToString(CultureInfo.InvariantCulture);

            SaveHelperProgressDoc(doc);

            return true;
        }

        private void RestoreTeachingProgressFromSnapshot(
            string helperId,
            string courseId,
            string courseTitle,
            int previousCourseTeaching,
            int previousTotalTeaching)
        {
            if (string.IsNullOrWhiteSpace(helperId) || string.IsNullOrWhiteSpace(courseId))
                return;

            var doc = LoadHelperProgressDoc();
            var helperNode = doc.SelectSingleNode($"//helper[@id='{helperId}']") as XmlElement;
            if (helperNode == null)
                return;

            var courseNode = helperNode.SelectSingleNode($"course[@id='{courseId}']") as XmlElement;
            if (courseNode == null)
                return;

            var teachingEl = courseNode["teachingSessions"] ?? doc.CreateElement("teachingSessions");
            if (teachingEl.ParentNode == null) courseNode.AppendChild(teachingEl);
            teachingEl.InnerText = previousCourseTeaching.ToString(CultureInfo.InvariantCulture);

            var totalsNode = helperNode.SelectSingleNode("totals") as XmlElement;
            if (totalsNode == null)
            {
                totalsNode = doc.CreateElement("totals");
                helperNode.AppendChild(totalsNode);
            }

            var totalTeachEl = totalsNode["totalTeachingSessions"] ?? doc.CreateElement("totalTeachingSessions");
            if (totalTeachEl.ParentNode == null) totalsNode.AppendChild(totalTeachEl);
            totalTeachEl.InnerText = previousTotalTeaching.ToString(CultureInfo.InvariantCulture);

            SaveHelperProgressDoc(doc);

        }

        private static int ParseIntSafe(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return 0;
            }

            int val;
            return int.TryParse(raw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out val)
                ? val
                : 0;
        }

        private List<ParticipantActionUndoSnapshot> GetParticipantUndoStorage()
        {
            var list = Session[ParticipantActionUndoSessionKey] as List<ParticipantActionUndoSnapshot>;
            if (list == null)
            {
                list = new List<ParticipantActionUndoSnapshot>();
                Session[ParticipantActionUndoSessionKey] = list;
            }

            return list;
        }

        private void PruneExpiredParticipantUndoSnapshots()
        {
            var list = GetParticipantUndoStorage();
            var cutoff = DateTime.UtcNow - ParticipantActionUndoWindow;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] == null || list[i].CreatedUtc < cutoff)
                {
                    list.RemoveAt(i);
                }
            }
        }

        private void AddParticipantUndoSnapshot(ParticipantActionUndoSnapshot snapshot)
        {
            if (snapshot == null) return;

            var list = GetParticipantUndoStorage();
            PruneExpiredParticipantUndoSnapshots();
            list.Add(snapshot);

            if (list.Count > 25)
            {
                list.RemoveAt(0);
            }
        }

        private ParticipantActionUndoSnapshot GetLatestParticipantUndoSnapshot(string helperId, string eventId, string sessionId)
        {
            PruneExpiredParticipantUndoSnapshots();

            var list = GetParticipantUndoStorage();
            ParticipantActionUndoSnapshot best = null;

            foreach (var item in list)
            {
                if (item == null) continue;
                if (!string.Equals(item.HelperId, helperId, StringComparison.OrdinalIgnoreCase)) continue;
                if (!string.Equals(item.EventId, eventId, StringComparison.OrdinalIgnoreCase)) continue;
                if (!string.Equals(item.SessionId, sessionId, StringComparison.OrdinalIgnoreCase)) continue;

                if (best == null || item.CreatedUtc > best.CreatedUtc)
                {
                    best = item;
                }
            }

            return best;
        }

        private ParticipantActionUndoSnapshot GetParticipantUndoSnapshotById(string helperId, string snapshotId)
        {
            if (string.IsNullOrWhiteSpace(snapshotId)) return null;

            PruneExpiredParticipantUndoSnapshots();

            var list = GetParticipantUndoStorage();
            foreach (var item in list)
            {
                if (item == null) continue;
                if (!string.Equals(item.HelperId, helperId, StringComparison.OrdinalIgnoreCase)) continue;
                if (string.Equals(item.SnapshotId, snapshotId, StringComparison.OrdinalIgnoreCase))
                {
                    return item;
                }
            }

            return null;
        }

        private void RemoveParticipantUndoSnapshot(string helperId, string snapshotId)
        {
            var list = GetParticipantUndoStorage();

            for (int i = list.Count - 1; i >= 0; i--)
            {
                var item = list[i];
                if (item == null) continue;
                if (!string.Equals(item.HelperId, helperId, StringComparison.OrdinalIgnoreCase)) continue;
                if (string.Equals(item.SnapshotId, snapshotId, StringComparison.OrdinalIgnoreCase))
                {
                    list.RemoveAt(i);
                    break;
                }
            }
        }


    }
}

