using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml;
using CyberApp_FIA.Services;

namespace CyberApp_FIA.Account
{
    public partial class UniversityAdminHelperAudit : SecurePage
    {
        private const int PageSize = 25;

        private string AuditXmlPath => Server.MapPath("~/App_Data/Audit_Log/UnvAdminAudit.xml");
        private string UsersXmlPath => Server.MapPath("~/App_Data/users.xml");
        private string HelperProgressXmlPath => Server.MapPath("~/App_Data/helperProgress.xml");
        private string HelperSessionReviewsXmlPath => Server.MapPath("~/App_Data/helperSessionReviews.xml");


        private string HelperEmailQuery =>
            (Request.QueryString["helperEmail"] ?? string.Empty).Trim();

        private const int Tier1Threshold = 5;
        private const int Tier2Threshold = 10;
        private const int Tier3Threshold = 20;

        private static string NormalizeBadgeTitle(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            return string.Join(" ", s.Trim().Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
        }

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

        private int CurrentPageIndex
        {
            get => ViewState["HelperAuditPageIndex"] is int i ? i : 0;
            set => ViewState["HelperAuditPageIndex"] = value < 0 ? 0 : value;
        }

        private sealed class SessionLogReviewItem
        {
            public string LogId { get; set; }
            public string CourseId { get; set; }
            public string CourseTitle { get; set; }
            public string Scope { get; set; } // Teaching | Help
            public DateTime LoggedLocal { get; set; }
            public string WhenLabel { get; set; }
            public string Details { get; set; }

            public string Status { get; set; } // Pending | Verified | Questioned
            public string StatusLabel { get; set; }
            public string CssClass { get; set; }
            public string AdminNote { get; set; }
        }

        private sealed class AuditRow
        {
            public string TimestampDate { get; set; }
            public string TimestampTime { get; set; }
            public DateTime TimestampLocal { get; set; }
            public string Role { get; set; }
            public string Type { get; set; }
            public string FirstName { get; set; }
            public string Email { get; set; }
            public string Details { get; set; }
        }

        private sealed class VerificationItem
        {
            public string CourseId { get; set; }
            public string CourseTitle { get; set; }
            public string Status { get; set; }
            public string StatusLabel { get; set; }
            public string CssClass { get; set; }
            public string LastUpdatedLabel { get; set; }
            public string HelperDisplayName { get; set; }
            public string AdminNote { get; set; }
            public string HelperNote { get; set; }
            public bool HasHelperNote { get; set; }
            public bool HasAdminNote { get; set; }
            public bool IsResubmission { get; set; }
            public string SubmissionLabel { get; set; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                var role = (string)Session["Role"];
                if (!string.Equals(role, "UniversityAdmin", StringComparison.OrdinalIgnoreCase))
                {
                    Response.Redirect("~/Account/Login.aspx");
                    return;
                }

                var helperEmail = HelperEmailQuery;
                if (string.IsNullOrWhiteSpace(helperEmail))
                {
                    Response.Redirect("~/Account/UniversityAdmin/UniversityAdminAudit.aspx");
                    return;
                }

                var adminEmail = (string)Session["Email"] ?? string.Empty;
                WelcomeName.Text = adminEmail.Length > 0 ? adminEmail : "University Admin";

                var adminUni = (string)Session["University"];
                if (string.IsNullOrWhiteSpace(adminUni))
                {
                    adminUni = LookupUniversityByEmail(adminEmail);
                }

                if (!TryBindHelperHeader(helperEmail, adminUni, out var helperUni, out var helperId))
                {
                    Response.Redirect("~/Account/UniversityAdmin/UniversityAdminAudit.aspx");
                    return;
                }

                UniversityValue.Value = helperUni ?? string.Empty;
                HelperIdValue.Value = helperId ?? string.Empty;

                EnsureAuditXml();
                EnsureHelperProgressXml();
                EnsureHelperSessionReviewsXml();

                InitializeFilters(helperEmail, helperUni);
                BindAuditLog();
                BindVerificationCards();
                BindIndividualSessionReviewCards();
            }
        }

        protected void BtnBack_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/Account/UniversityAdmin/UniversityAdminAudit.aspx");
        }

        protected void BtnApplyFilters_Click(object sender, EventArgs e)
        {
            CurrentPageIndex = 0;
            BindAuditLog();
        }

        protected void BtnClearFilters_Click(object sender, EventArgs e)
        {
            TxtSearch.Text = string.Empty;
            TxtFromTime.Text = string.Empty;
            TxtToTime.Text = string.Empty;

            if (DdlTypeFilter.Items.Count > 0)
            {
                DdlTypeFilter.SelectedIndex = 0;
            }

            CurrentPageIndex = 0;
            BindAuditLog();
        }

        protected void BtnPrevPage_Click(object sender, EventArgs e)
        {
            if (CurrentPageIndex > 0)
            {
                CurrentPageIndex--;
                BindAuditLog();
            }
        }

        protected void BtnNextPage_Click(object sender, EventArgs e)
        {
            CurrentPageIndex++;
            BindAuditLog();
        }

        protected void BtnExportCsv_Click(object sender, EventArgs e)
        {
            var rows = GetFilteredAuditRows();

            Response.Clear();
            Response.ContentType = "text/csv";
            var fileName = $"FIA_HelperAudit_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
            Response.AddHeader("Content-Disposition", "attachment; filename=" + fileName);

            using (var writer = new StringWriter())
            {
                writer.WriteLine("LocalTimestamp,Role,Type,FirstName,Email,Details");

                foreach (var row in rows)
                {
                    var ts = row.TimestampLocal != DateTime.MinValue
                        ? row.TimestampLocal.ToString("yyyy-MM-dd HH:mm:ss")
                        : "";

                    writer.WriteLine(string.Join(",",
                        CsvEscape(ts),
                        CsvEscape(row.Role),
                        CsvEscape(row.Type),
                        CsvEscape(row.FirstName),
                        CsvEscape(row.Email),
                        CsvEscape(row.Details)
                    ));
                }

                Response.Write(writer.ToString());
            }

            Response.End();
        }

        protected void ReviewFilter_Changed(object sender, EventArgs e)
        {
            BindVerificationCards();
            BindIndividualSessionReviewCards();
        }

        protected void VerificationRepeater_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            var helperId = HelperIdValue.Value;
            if (string.IsNullOrWhiteSpace(helperId))
            {
                return;
            }

            var courseId = e.CommandArgument as string;
            if (string.IsNullOrWhiteSpace(courseId))
            {
                return;
            }

            string newStatus;
            string auditType;

            if (string.Equals(e.CommandName, "markVerified", StringComparison.OrdinalIgnoreCase))
            {
                newStatus = "Verified";
                auditType = "Helper Certification Verified";
            }
            else if (string.Equals(e.CommandName, "markQuestioned", StringComparison.OrdinalIgnoreCase))
            {
                newStatus = "Questioned";
                auditType = "Helper Certification Questioned";
            }
            else
            {
                return;
            }

            var adminNote = string.Empty;
            var noteBox = (TextBox)e.Item.FindControl("TxtAdminNote");
            if (noteBox != null)
            {
                adminNote = (noteBox.Text ?? string.Empty).Trim();
            }

            UpdateVerificationStatus(helperId, courseId, newStatus, adminNote);

            try
            {
                var details = $"Admin set certification verification to '{newStatus}' for helperId={helperId}, courseId={courseId}.";
                if (!string.IsNullOrWhiteSpace(adminNote))
                {
                    var preview = adminNote;
                    if (preview.Length > 180) preview = preview.Substring(0, 177) + "...";
                    details += " Admin note: " + preview;
                }

                UniversityAuditLogger.AppendForCurrentUser(this, auditType, details);
            }
            catch
            {
            }

            BindVerificationCards();
            BindIndividualSessionReviewCards();
            BindAuditLog();
        }

        protected void TeachingLogReviewRepeater_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            HandleSessionLogReviewCommand(
                e,
                noteControlId: "TxtTeachingLogNote",
                questionCommandName: "questionTeachingLog",
                scope: "Teaching",
                verifiedAuditType: "Helper Teaching Session Verified",
                questionedAuditType: "Helper Teaching Session Questioned");
        }

        protected void HelpLogReviewRepeater_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            HandleSessionLogReviewCommand(
                e,
                noteControlId: "TxtHelpLogNote",
                questionCommandName: "questionHelpLog",
                scope: "Help",
                verifiedAuditType: "Helper 1:1 Help Session Verified",
                questionedAuditType: "Helper 1:1 Help Session Questioned");
        }

        private void HandleSessionLogReviewCommand(
            RepeaterCommandEventArgs e,
            string noteControlId,
            string questionCommandName,
            string scope,
            string verifiedAuditType,
            string questionedAuditType)
        {
            var helperId = HelperIdValue.Value ?? string.Empty;
            var helperEmail = HelperEmailQuery;
            var uni = UniversityValue.Value ?? string.Empty;

            var logId = e.CommandArgument as string;
            if (string.IsNullOrWhiteSpace(logId) ||
                string.IsNullOrWhiteSpace(helperId) ||
                string.IsNullOrWhiteSpace(helperEmail) ||
                string.IsNullOrWhiteSpace(uni))
            {
                return;
            }

            var noteBox = (TextBox)e.Item.FindControl(noteControlId);
            var adminNote = noteBox != null ? (noteBox.Text ?? string.Empty).Trim() : string.Empty;

            var row = LoadHelperSessionAuditLogs(helperEmail, helperId, uni)
                .FirstOrDefault(x => string.Equals(x.LogId, logId, StringComparison.OrdinalIgnoreCase));

            if (row == null)
            {
                return;
            }

            var newStatus = string.Equals(e.CommandName, questionCommandName, StringComparison.OrdinalIgnoreCase)
                ? "Questioned"
                : "Verified";

            SaveSessionLogReview(
                row.LogId,
                helperId,
                helperEmail,
                row.CourseId,
                row.CourseTitle,
                scope,
                row.LoggedLocal == DateTime.MinValue ? DateTime.UtcNow : row.LoggedLocal.ToUniversalTime(),
                newStatus,
                adminNote);

            RecalculateHelperProgressFromSessionReviews(helperId);

            try
            {
                var auditType = newStatus == "Questioned" ? questionedAuditType : verifiedAuditType;

                var details = $"Admin reviewed {scope} logId={row.LogId} for helperId={helperId}, courseId={row.CourseId}, course=\"{row.CourseTitle}\" and set status to '{newStatus}'.";
                if (!string.IsNullOrWhiteSpace(adminNote))
                {
                    var preview = adminNote;
                    if (preview.Length > 180) preview = preview.Substring(0, 177) + "...";
                    details += " Admin note: " + preview;
                }

                UniversityAuditLogger.AppendForCurrentUser(this, auditType, details);
            }
            catch
            {
            }

            BindVerificationCards();
            BindIndividualSessionReviewCards();
            BindAuditLog();
        }

        private static string CsvEscape(string s)
        {
            if (s == null) return "";
            if (s.Contains("\"") || s.Contains(",") || s.Contains("\r") || s.Contains("\n"))
            {
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            }

            return s;
        }

        private void EnsureAuditXml()
        {
            var dir = Path.GetDirectoryName(AuditXmlPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (!File.Exists(AuditXmlPath))
            {
                var init = "<?xml version='1.0' encoding='utf-8'?><auditLog version='1'></auditLog>";
                File.WriteAllText(AuditXmlPath, init);
            }
        }

        private void EnsureHelperProgressXml()
        {
            var dir = Path.GetDirectoryName(HelperProgressXmlPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (!File.Exists(HelperProgressXmlPath))
            {
                var init = "<?xml version='1.0' encoding='utf-8'?><helperProgress version='1'></helperProgress>";
                File.WriteAllText(HelperProgressXmlPath, init);
            }
        }

        private void EnsureHelperSessionReviewsXml()
        {
            var dir = Path.GetDirectoryName(HelperSessionReviewsXmlPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (!File.Exists(HelperSessionReviewsXmlPath))
            {
                File.WriteAllText(
                    HelperSessionReviewsXmlPath,
                    "<?xml version='1.0' encoding='utf-8'?><helperSessionReviews version='1'></helperSessionReviews>");
            }
        }

       

        private bool TryBindHelperHeader(string helperEmail, string adminUni, out string helperUni, out string helperId)
        {
            helperUni = string.Empty;
            helperId = string.Empty;

            try
            {
                if (string.IsNullOrWhiteSpace(helperEmail) || !File.Exists(UsersXmlPath))
                    return false;

                var doc = new XmlDocument();
                doc.Load(UsersXmlPath);
                var emailLower = helperEmail.ToLowerInvariant();

                var node = doc.SelectSingleNode(
                    $"/users/user[translate(email,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='{emailLower}']"
                ) as XmlElement;

                if (node == null)
                {
                    return false;
                }

                helperId = node.GetAttribute("id") ?? string.Empty;

                var roleAttr = node.GetAttribute("role") ?? string.Empty;
                if (!string.Equals(roleAttr, "Helper", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                helperUni = node["university"]?.InnerText ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(adminUni) &&
                    !string.Equals(adminUni, helperUni, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                var firstName = node["firstName"]?.InnerText ?? "";
                var lastName = node["lastName"]?.InnerText ?? "";

                var displayName = (firstName + " " + lastName).Trim();
                if (string.IsNullOrWhiteSpace(displayName))
                {
                    displayName = helperEmail;
                }

                HelperName.Text = displayName;
                HelperEmailLiteral.Text = helperEmail;
                HelperUniversityLiteral.Text = string.IsNullOrWhiteSpace(helperUni)
                    ? "(university not set)"
                    : helperUni;

                return true;
            }
            catch
            {
                return false;
            }
        }

        private void InitializeFilters(string helperEmail, string helperUni)
        {
            DdlTypeFilter.Items.Clear();
            DdlTypeFilter.Items.Add(new ListItem("All types", ""));

            var types = GetHelperAuditTypes(helperEmail, helperUni);
            foreach (var t in types.OrderBy(x => x))
            {
                DdlTypeFilter.Items.Add(new ListItem(t, t));
            }
        }

        private HashSet<string> GetHelperAuditTypes(string helperEmail, string helperUni)
        {
            var types = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                if (File.Exists(AuditXmlPath) &&
                    !string.IsNullOrWhiteSpace(helperEmail) &&
                    !string.IsNullOrWhiteSpace(helperUni))
                {
                    var doc = new XmlDocument();
                    doc.Load(AuditXmlPath);
                    var nodes = doc.SelectNodes("/auditLog/entry");

                    foreach (XmlElement entry in nodes)
                    {
                        var entryUni = entry.GetAttribute("university");
                        if (!string.Equals(entryUni, helperUni, StringComparison.OrdinalIgnoreCase))
                            continue;

                        var emailAttr = entry.GetAttribute("email");
                        if (!string.Equals(emailAttr, helperEmail, StringComparison.OrdinalIgnoreCase))
                            continue;

                        var roleAttr = entry.GetAttribute("role") ?? string.Empty;
                        if (!string.Equals(roleAttr, "Helper", StringComparison.OrdinalIgnoreCase))
                            continue;

                        var typeAttr = entry.GetAttribute("type");
                        if (!string.IsNullOrWhiteSpace(typeAttr))
                        {
                            types.Add(typeAttr.Trim());
                        }
                    }
                }
            }
            catch
            {
            }

            return types;
        }

        private Dictionary<string, SessionLogReviewItem> LoadSessionReviewLookup()
        {
            var dict = new Dictionary<string, SessionLogReviewItem>(StringComparer.OrdinalIgnoreCase);

            EnsureHelperSessionReviewsXml();

            var doc = new XmlDocument();
            doc.Load(HelperSessionReviewsXmlPath);

            foreach (XmlElement r in doc.SelectNodes("/helperSessionReviews/review"))
            {
                var logId = r.GetAttribute("logId");
                if (string.IsNullOrWhiteSpace(logId))
                    continue;

                dict[logId] = new SessionLogReviewItem
                {
                    LogId = logId,
                    Status = r.GetAttribute("status"),
                    AdminNote = r["adminNote"]?.InnerText ?? string.Empty
                };
            }

            return dict;
        }

        private void SaveSessionLogReview(
            string logId,
            string helperId,
            string helperEmail,
            string courseId,
            string courseTitle,
            string scope,
            DateTime loggedUtc,
            string status,
            string adminNote)
        {
            EnsureHelperSessionReviewsXml();

            var doc = new XmlDocument();
            doc.Load(HelperSessionReviewsXmlPath);

            var root = doc.DocumentElement;
            var review = doc.SelectSingleNode($"/helperSessionReviews/review[@logId='{logId}']") as XmlElement;

            if (review == null)
            {
                review = doc.CreateElement("review");
                review.SetAttribute("logId", logId);
                root.AppendChild(review);
            }

            review.SetAttribute("helperId", helperId ?? string.Empty);
            review.SetAttribute("helperEmail", helperEmail ?? string.Empty);
            review.SetAttribute("courseId", courseId ?? string.Empty);
            review.SetAttribute("courseTitle", courseTitle ?? string.Empty);
            review.SetAttribute("scope", scope ?? string.Empty);
            review.SetAttribute("loggedUtc", loggedUtc.ToString("o"));
            review.SetAttribute("status", status ?? "Pending");
            review.SetAttribute("reviewedUtc", DateTime.UtcNow.ToString("o"));

            var noteEl = review["adminNote"] ?? doc.CreateElement("adminNote");
            noteEl.InnerText = adminNote ?? string.Empty;
            if (noteEl.ParentNode == null) review.AppendChild(noteEl);

            doc.Save(HelperSessionReviewsXmlPath);
        }

        private List<AuditRow> GetFilteredAuditRows()
        {
            var helperEmail = HelperEmailQuery;
            var uni = UniversityValue.Value ?? string.Empty;
            var rows = new List<AuditRow>();

            try
            {
                if (!string.IsNullOrWhiteSpace(helperEmail) &&
                    !string.IsNullOrWhiteSpace(uni) &&
                    File.Exists(AuditXmlPath))
                {
                    var doc = new XmlDocument();
                    doc.Load(AuditXmlPath);
                    var nodes = doc.SelectNodes("/auditLog/entry");

                    foreach (XmlElement entry in nodes)
                    {
                        var entryUni = entry.GetAttribute("university");
                        if (!string.Equals(entryUni, uni, StringComparison.OrdinalIgnoreCase))
                            continue;

                        var emailAttr = entry.GetAttribute("email");
                        if (!string.Equals(emailAttr, helperEmail, StringComparison.OrdinalIgnoreCase))
                            continue;

                        var roleAttr = entry.GetAttribute("role") ?? string.Empty;
                        if (!string.Equals(roleAttr, "Helper", StringComparison.OrdinalIgnoreCase))
                            continue;

                        var timestampRaw = entry.GetAttribute("timestamp");
                        var type = entry.GetAttribute("type");
                        var firstName = entry.GetAttribute("firstName");
                        var details = entry["details"]?.InnerText ?? entry.GetAttribute("details");

                        DateTime tsUtc;
                        DateTime localTs = DateTime.MinValue;
                        string dateLabel = "";
                        string timeLabel = "";

                        if (DateTime.TryParse(timestampRaw, null, DateTimeStyles.AdjustToUniversal, out tsUtc))
                        {
                            localTs = tsUtc.ToLocalTime();
                            dateLabel = localTs.ToString("MMM d", CultureInfo.InvariantCulture);
                            timeLabel = localTs.ToString("h:mm tt", CultureInfo.InvariantCulture);
                        }

                        rows.Add(new AuditRow
                        {
                            TimestampDate = dateLabel,
                            TimestampTime = timeLabel,
                            TimestampLocal = localTs,
                            Role = roleAttr,
                            Type = type,
                            FirstName = firstName,
                            Email = emailAttr,
                            Details = details
                        });
                    }
                }
            }
            catch
            {
            }

            var search = (TxtSearch.Text ?? "").Trim().ToLowerInvariant();
            var typeFilter = (DdlTypeFilter.SelectedValue ?? "").Trim();
            var fromText = (TxtFromTime.Text ?? "").Trim();
            var toText = (TxtToTime.Text ?? "").Trim();

            var fromLocalOpt = ParseLocal(fromText);
            var toLocalOpt = ParseLocal(toText);
            var hasFrom = fromLocalOpt.HasValue;
            var hasTo = toLocalOpt.HasValue;

            IEnumerable<AuditRow> filtered = rows;

            if (!string.IsNullOrEmpty(typeFilter))
            {
                filtered = filtered.Where(r =>
                    string.Equals(r.Type, typeFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (hasFrom)
            {
                filtered = filtered.Where(r => r.TimestampLocal >= fromLocalOpt.Value);
            }

            if (hasTo)
            {
                filtered = filtered.Where(r => r.TimestampLocal <= toLocalOpt.Value);
            }

            if (!string.IsNullOrEmpty(search))
            {
                filtered = filtered.Where(r =>
                    (r.Type ?? "").ToLowerInvariant().Contains(search) ||
                    (r.Details ?? "").ToLowerInvariant().Contains(search));
            }

            return filtered
                .OrderByDescending(r => r.TimestampLocal)
                .ToList();
        }

        private void BindAuditLog()
        {
            var filteredList = GetFilteredAuditRows();

            var totalCount = filteredList.Count;
            var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)PageSize);

            if (CurrentPageIndex >= totalPages) CurrentPageIndex = totalPages - 1;
            if (CurrentPageIndex < 0) CurrentPageIndex = 0;

            var skip = CurrentPageIndex * PageSize;
            var pageRows = filteredList.Skip(skip).Take(PageSize).ToList();

            NoAuditPlaceholder.Visible = totalCount == 0;
            AuditRepeater.DataSource = pageRows;
            AuditRepeater.DataBind();

            BtnPrevPage.Enabled = CurrentPageIndex > 0;
            BtnNextPage.Enabled = CurrentPageIndex < totalPages - 1;

            if (totalCount == 0)
            {
                LblPageInfo.Text = "No entries yet";
            }
            else
            {
                LblPageInfo.Text = $"Page {CurrentPageIndex + 1} of {totalPages} • {totalCount} entr" +
                                   (totalCount == 1 ? "y" : "ies");
            }
        }

        private void BindVerificationCards()
        {
            var helperId = HelperIdValue.Value;
            if (string.IsNullOrWhiteSpace(helperId))
            {
                VerificationListPlaceholder.Visible = false;
                QuizFilteredEmptyPlaceholder.Visible = false;
                return;
            }

            EnsureHelperProgressXml();
            if (!File.Exists(HelperProgressXmlPath))
            {
                VerificationListPlaceholder.Visible = false;
                QuizFilteredEmptyPlaceholder.Visible = true;
                return;
            }

            var items = new List<VerificationItem>();

            var doc = new XmlDocument();
            doc.Load(HelperProgressXmlPath);

            var helperEl = (XmlElement)doc.SelectSingleNode($"/helperProgress/helper[@id='{helperId}']");
            if (helperEl == null)
            {
                VerificationListPlaceholder.Visible = false;
                QuizFilteredEmptyPlaceholder.Visible = true;
                return;
            }

            var displayName = helperEl["displayName"]?.InnerText ?? string.Empty;

            foreach (XmlElement c in helperEl.SelectNodes("./course"))
            {
                var status = (c["verificationStatus"]?.InnerText ?? "NotRequested").Trim();
                if (string.Equals(status, "NotRequested", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var courseId = c.GetAttribute("id");
                var title = c["title"]?.InnerText ?? courseId;
                var updatedText = c["verificationUpdatedUtc"]?.InnerText ?? c["lastUpdatedUtc"]?.InnerText ?? string.Empty;

                var adminNote = c["verificationAdminNote"]?.InnerText ?? string.Empty;
                var helperNote = c["verificationHelperNote"]?.InnerText ?? string.Empty;
                var submissionKind = (c["verificationSubmissionKind"]?.InnerText ?? "Initial").Trim();

                DateTime updated;
                string updatedLabel = string.Empty;
                if (DateTime.TryParse(updatedText, out updated))
                {
                    updatedLabel = updated.ToLocalTime().ToString("MMM d, yyyy h:mm tt");
                }

                string cssClass;
                string statusLabel;

                if (status.Equals("Verified", StringComparison.OrdinalIgnoreCase))
                {
                    cssClass = "verified";
                    statusLabel = "Verified";
                }
                else if (status.Equals("Questioned", StringComparison.OrdinalIgnoreCase))
                {
                    cssClass = "questioned";
                    statusLabel = "Questioned";
                }
                else
                {
                    cssClass = "pending";
                    statusLabel = "Pending review";
                }

                var isResub = submissionKind.Equals("Resubmission", StringComparison.OrdinalIgnoreCase);
                if (isResub)
                {
                    cssClass += " resubmission";
                }

                var submissionLabel = isResub ? "Resubmission" : "Initial submission";

                items.Add(new VerificationItem
                {
                    CourseId = courseId,
                    CourseTitle = title,
                    Status = status,
                    StatusLabel = statusLabel,
                    CssClass = cssClass,
                    LastUpdatedLabel = updatedLabel,
                    HelperDisplayName = displayName,
                    AdminNote = adminNote,
                    HelperNote = helperNote,
                    HasHelperNote = !string.IsNullOrWhiteSpace(helperNote),
                    HasAdminNote = !string.IsNullOrWhiteSpace(adminNote),
                    IsResubmission = isResub,
                    SubmissionLabel = submissionLabel
                });
            }

            var filteredItems = items
                .Where(i => MatchesStatusFilter(
                    i.Status,
                    ChkQuizShowVerified.Checked,
                    ChkQuizShowQuestioned.Checked,
                    ChkQuizShowPending.Checked))
                .OrderBy(i => i.CourseTitle)
                .ToList();

            VerificationListPlaceholder.Visible = filteredItems.Count > 0;
            QuizFilteredEmptyPlaceholder.Visible = filteredItems.Count == 0;

            VerificationRepeater.DataSource = filteredItems;
            VerificationRepeater.DataBind();
        }

        private void BindIndividualSessionReviewCards()
        {
            var helperEmail = HelperEmailQuery;
            var helperId = HelperIdValue.Value ?? string.Empty;
            var uni = UniversityValue.Value ?? string.Empty;

            if (string.IsNullOrWhiteSpace(helperEmail) ||
                string.IsNullOrWhiteSpace(helperId) ||
                string.IsNullOrWhiteSpace(uni))
            {
                TeachingLogReviewPlaceholder.Visible = false;
                HelpLogReviewPlaceholder.Visible = false;
                TeachingFilteredEmptyPlaceholder.Visible = false;
                HelpFilteredEmptyPlaceholder.Visible = false;
                return;
            }

            EnsureAuditXml();
            EnsureHelperSessionReviewsXml();

            var allLogs = LoadHelperSessionAuditLogs(helperEmail, helperId, uni);

            var teachingFiltered = allLogs
                .Where(x => x.Scope == "Teaching")
                .Where(x => MatchesStatusFilter(
                    x.Status,
                    ChkTeachingShowVerified.Checked,
                    ChkTeachingShowQuestioned.Checked,
                    ChkTeachingShowPending.Checked))
                .OrderByDescending(x => x.LoggedLocal)
                .ToList();

            var helpFiltered = allLogs
                .Where(x => x.Scope == "Help")
                .Where(x => MatchesStatusFilter(
                    x.Status,
                    ChkHelpShowVerified.Checked,
                    ChkHelpShowQuestioned.Checked,
                    ChkHelpShowPending.Checked))
                .OrderByDescending(x => x.LoggedLocal)
                .ToList();

            TeachingLogReviewPlaceholder.Visible = teachingFiltered.Count > 0;
            TeachingFilteredEmptyPlaceholder.Visible = teachingFiltered.Count == 0;
            TeachingLogReviewRepeater.DataSource = teachingFiltered;
            TeachingLogReviewRepeater.DataBind();

            HelpLogReviewPlaceholder.Visible = helpFiltered.Count > 0;
            HelpFilteredEmptyPlaceholder.Visible = helpFiltered.Count == 0;
            HelpLogReviewRepeater.DataSource = helpFiltered;
            HelpLogReviewRepeater.DataBind();
        }

        private List<SessionLogReviewItem> LoadHelperSessionAuditLogs(string helperEmail, string helperId, string uni)
        {
            var items = new List<SessionLogReviewItem>();

            if (!File.Exists(AuditXmlPath))
            {
                return items;
            }

            var reviewLookup = LoadSessionReviewLookup();

            var doc = new XmlDocument();
            doc.Load(AuditXmlPath);

            foreach (XmlElement entry in doc.SelectNodes("/auditLog/entry"))
            {
                var entryUni = entry.GetAttribute("university");
                var role = entry.GetAttribute("role");
                var email = entry.GetAttribute("email");
                var type = entry.GetAttribute("type");

                if (!string.Equals(entryUni, uni, StringComparison.OrdinalIgnoreCase)) continue;
                if (!string.Equals(role, "Helper", StringComparison.OrdinalIgnoreCase)) continue;
                if (!string.Equals(email, helperEmail, StringComparison.OrdinalIgnoreCase)) continue;

                var isTeaching = string.Equals(type, "Helper Delivered Session", StringComparison.OrdinalIgnoreCase);
                var isHelp = string.Equals(type, "Helper 1:1 Help Session", StringComparison.OrdinalIgnoreCase);

                if (!isTeaching && !isHelp) continue;

                var logId = entry.GetAttribute("id");
                var timestampRaw = entry.GetAttribute("timestamp");
                var details = entry["details"]?.InnerText ?? string.Empty;

                DateTime tsUtc;
                DateTime localTs = DateTime.MinValue;
                if (DateTime.TryParse(timestampRaw, out tsUtc))
                {
                    localTs = tsUtc.ToLocalTime();
                }

                var courseTitle = ExtractCourseTitleFromDetails(details);
                var courseId = isTeaching
                    ? ExtractCourseIdFromDetails(details)
                    : LookupCourseIdByTitle(helperId, courseTitle);

                SessionLogReviewItem existing;
                reviewLookup.TryGetValue(logId, out existing);

                var status = existing?.Status ?? "Pending";
                var adminNote = existing?.AdminNote ?? string.Empty;

                items.Add(new SessionLogReviewItem
                {
                    LogId = logId,
                    CourseId = courseId,
                    CourseTitle = string.IsNullOrWhiteSpace(courseTitle) ? "(unknown microcourse)" : courseTitle,
                    Scope = isTeaching ? "Teaching" : "Help",
                    LoggedLocal = localTs,
                    WhenLabel = localTs == DateTime.MinValue ? "" : localTs.ToString("MMM d, yyyy • h:mm tt"),
                    Details = details,
                    Status = status,
                    StatusLabel = status == "Questioned" ? "Questioned"
                               : status == "Verified" ? "Verified"
                               : "Pending review",
                    CssClass = status == "Questioned" ? "questioned"
                             : status == "Verified" ? "verified"
                             : "pending",
                    AdminNote = adminNote
                });
            }

            return items;
        }

        private static string ExtractCourseTitleFromDetails(string details)
        {
            if (string.IsNullOrWhiteSpace(details))
                return string.Empty;

            var firstQuote = details.IndexOf("\"", StringComparison.Ordinal);
            var secondQuote = firstQuote >= 0
                ? details.IndexOf("\"", firstQuote + 1, StringComparison.Ordinal)
                : -1;

            if (firstQuote >= 0 && secondQuote > firstQuote)
            {
                return details.Substring(firstQuote + 1, secondQuote - firstQuote - 1).Trim();
            }

            return string.Empty;
        }

        private static string ExtractCourseIdFromDetails(string details)
        {
            if (string.IsNullOrWhiteSpace(details))
                return string.Empty;

            var key = "(courseId=";
            var start = details.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            if (start < 0)
                return string.Empty;

            start += key.Length;
            var end = details.IndexOf(")", start, StringComparison.OrdinalIgnoreCase);
            if (end < 0) end = details.Length;

            return details.Substring(start, end - start).Trim();
        }

        private string LookupCourseIdByTitle(string helperId, string courseTitle)
        {
            if (string.IsNullOrWhiteSpace(helperId) ||
                string.IsNullOrWhiteSpace(courseTitle) ||
                !File.Exists(HelperProgressXmlPath))
            {
                return string.Empty;
            }

            var doc = new XmlDocument();
            doc.Load(HelperProgressXmlPath);

            foreach (XmlElement c in doc.SelectNodes($"/helperProgress/helper[@id='{helperId}']/course"))
            {
                var title = c["title"]?.InnerText ?? string.Empty;
                if (string.Equals(title.Trim(), courseTitle.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return c.GetAttribute("id") ?? string.Empty;
                }
            }

            return string.Empty;
        }

        private static int ParseIntSafe(string value, int fallback)
        {
            if (int.TryParse(value ?? string.Empty, out var v))
            {
                return v < 0 ? 0 : v;
            }

            return fallback;
        }

        private static DateTime? ParseLocal(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            if (DateTime.TryParse(s, out var dt)) return dt;
            return null;
        }

        private static bool MatchesStatusFilter(string status, bool showVerified, bool showQuestioned, bool showPending)
        {
            if (string.Equals(status, "Verified", StringComparison.OrdinalIgnoreCase))
                return showVerified;

            if (string.Equals(status, "Questioned", StringComparison.OrdinalIgnoreCase))
                return showQuestioned;

            return showPending;
        }

        private string LookupUniversityByEmail(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email) || !File.Exists(UsersXmlPath))
                    return "";

                var doc = new XmlDocument();
                doc.Load(UsersXmlPath);
                var emailLower = email.ToLowerInvariant();

                var node = doc.SelectSingleNode(
                    $"/users/user[translate(email,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='{emailLower}']");

                return node?["university"]?.InnerText ?? "";
            }
            catch
            {
                return "";
            }
        }

        private void UpdateVerificationStatus(string helperId, string courseId, string newStatus, string adminNote)
        {
            EnsureHelperProgressXml();

            var doc = new XmlDocument();
            doc.Load(HelperProgressXmlPath);

            var helperEl = (XmlElement)doc.SelectSingleNode($"/helperProgress/helper[@id='{helperId}']");
            if (helperEl == null) return;

            var courseEl = (XmlElement)helperEl.SelectSingleNode($"./course[@id='{courseId}']");
            if (courseEl == null) return;

            var statusEl = courseEl["verificationStatus"] ?? doc.CreateElement("verificationStatus");
            statusEl.InnerText = newStatus;
            if (statusEl.ParentNode == null) courseEl.AppendChild(statusEl);

            var updatedEl = courseEl["verificationUpdatedUtc"] ?? doc.CreateElement("verificationUpdatedUtc");
            updatedEl.InnerText = DateTime.UtcNow.ToString("o");
            if (updatedEl.ParentNode == null) courseEl.AppendChild(updatedEl);

            var adminNoteEl = courseEl["verificationAdminNote"] ?? doc.CreateElement("verificationAdminNote");
            adminNoteEl.InnerText = adminNote ?? string.Empty;
            if (adminNoteEl.ParentNode == null) courseEl.AppendChild(adminNoteEl);

            doc.Save(HelperProgressXmlPath);
        }

        private void RecalculateHelperProgressFromSessionReviews(string helperId)
        {
            EnsureHelperProgressXml();
            EnsureHelperSessionReviewsXml();
            EnsureAuditXml();

            var progressDoc = new XmlDocument();
            progressDoc.Load(HelperProgressXmlPath);

            var helperEl = progressDoc.SelectSingleNode($"/helperProgress/helper[@id='{helperId}']") as XmlElement;
            if (helperEl == null)
            {
                return;
            }

            var helperEmail = HelperEmailQuery;
            var uni = UniversityValue.Value ?? string.Empty;

            var rawLogs = LoadHelperSessionAuditLogs(helperEmail, helperId, uni);
            var reviewLookup = LoadSessionReviewLookup();
            var nowIso = DateTime.UtcNow.ToString("o");

            var totalTeaching = 0;
            var totalHelp = 0;

            foreach (XmlElement courseEl in helperEl.SelectNodes("./course"))
            {
                var courseId = courseEl.GetAttribute("id");

                var teachingRaw = rawLogs.Count(x => x.Scope == "Teaching" && x.CourseId == courseId);
                var teachingQuestioned = rawLogs.Count(x =>
                    x.Scope == "Teaching" &&
                    x.CourseId == courseId &&
                    reviewLookup.ContainsKey(x.LogId) &&
                    string.Equals(reviewLookup[x.LogId].Status, "Questioned", StringComparison.OrdinalIgnoreCase));

                var helpRaw = rawLogs.Count(x => x.Scope == "Help" && x.CourseId == courseId);
                var helpQuestioned = rawLogs.Count(x =>
                    x.Scope == "Help" &&
                    x.CourseId == courseId &&
                    reviewLookup.ContainsKey(x.LogId) &&
                    string.Equals(reviewLookup[x.LogId].Status, "Questioned", StringComparison.OrdinalIgnoreCase));

                var effectiveTeaching = Math.Max(0, teachingRaw - teachingQuestioned);
                var effectiveHelp = Math.Max(0, helpRaw - helpQuestioned);

                SetChildInnerText(progressDoc, courseEl, "teachingSessions", effectiveTeaching.ToString());
                SetChildInnerText(progressDoc, courseEl, "helpSessions", effectiveHelp.ToString());

                SetChildInnerText(progressDoc, courseEl, "teachingRevokedFlag", teachingQuestioned > 0 ? "true" : "false");
                SetChildInnerText(progressDoc, courseEl, "helpRevokedFlag", helpQuestioned > 0 ? "true" : "false");

                SetChildInnerText(progressDoc, courseEl, "teachingReviewStatus", teachingQuestioned > 0 ? "Questioned" : "Ok");
                SetChildInnerText(progressDoc, courseEl, "helpReviewStatus", helpQuestioned > 0 ? "Questioned" : "Ok");

                SetChildInnerText(progressDoc, courseEl, "teachingReviewAdminNote",
                    GetLatestQuestionedNoteForCourse(rawLogs, reviewLookup, courseId, "Teaching"));
                SetChildInnerText(progressDoc, courseEl, "helpReviewAdminNote",
                    GetLatestQuestionedNoteForCourse(rawLogs, reviewLookup, courseId, "Help"));

               

                SetChildInnerText(progressDoc, courseEl, "teachingReviewUpdatedUtc", nowIso);
                SetChildInnerText(progressDoc, courseEl, "helpReviewUpdatedUtc", nowIso);

                totalTeaching += effectiveTeaching;
                totalHelp += effectiveHelp;
            }

            var totalsEl = helperEl["totals"] ?? progressDoc.CreateElement("totals");
            if (totalsEl.ParentNode == null)
            {
                helperEl.AppendChild(totalsEl);
            }

            SetChildInnerText(progressDoc, totalsEl, "totalTeachingSessions", totalTeaching.ToString());
            SetChildInnerText(progressDoc, totalsEl, "totalHelpSessions", totalHelp.ToString());
            SetChildInnerText(progressDoc, totalsEl, "lastUpdatedUtc", nowIso);

            progressDoc.Save(HelperProgressXmlPath);
        }

        private static string GetLatestQuestionedNoteForCourse(
            List<SessionLogReviewItem> rawLogs,
            Dictionary<string, SessionLogReviewItem> reviewLookup,
            string courseId,
            string scope)
        {
            var latest = rawLogs
                .Where(x =>
                    string.Equals(x.CourseId, courseId, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(x.Scope, scope, StringComparison.OrdinalIgnoreCase) &&
                    reviewLookup.ContainsKey(x.LogId) &&
                    string.Equals(reviewLookup[x.LogId].Status, "Questioned", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.LoggedLocal)
                .Select(x => reviewLookup[x.LogId].AdminNote)
                .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

            return latest ?? string.Empty;
        }

        private static void SetChildInnerText(XmlDocument doc, XmlElement parent, string name, string value)
        {
            var node = parent[name] ?? doc.CreateElement(name);
            node.InnerText = value ?? string.Empty;
            if (node.ParentNode == null)
            {
                parent.AppendChild(node);
            }
        }
    }
}


