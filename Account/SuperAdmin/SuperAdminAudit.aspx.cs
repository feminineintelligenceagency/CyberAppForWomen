using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml;
using System.Web;


namespace CyberApp_FIA.Account
{
    public partial class SuperAdminAudit : SecurePage
    {
        private const int PageSize = 25;
        private const int CriticalPageSize = 25;

        private string AuditXmlPath => Server.MapPath("~/App_Data/Audit_Log/UnvAdminAudit.xml");

        private int CurrentPageIndex
        {
            get => ViewState["AuditPageIndex"] is int i ? i : 0;
            set => ViewState["AuditPageIndex"] = value < 0 ? 0 : value;
        }

        private int CurrentCriticalPageIndex
        {
            get => ViewState["CriticalPageIndex"] is int i ? i : 0;
            set => ViewState["CriticalPageIndex"] = value < 0 ? 0 : value;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                var role = (string)Session["Role"];
                if (!string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
                {
                    Response.Redirect("~/Account/Login.aspx");
                    return;
                }

                var email = (string)Session["Email"] ?? "";
                WelcomeName.Text = email.Length > 0 ? email : "Super Admin";

                EnsureAuditXml();
                InitializeFilters();
                InitializeCriticalFilters();
                BindAuditLog();
                BindCriticalLog();

                // Mark all current critical-ish events as "seen"
                MarkCriticalEventsAsSeen();
            }
        }

        protected void BtnBackHome_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/Account/SuperAdmin/SuperAdminHome.aspx");
        }

        protected void BtnApplyFilters_Click(object sender, EventArgs e)
        {
            CurrentPageIndex = 0;
            BindAuditLog();
            // Critical events respect the same time window, so rebind too.
            CurrentCriticalPageIndex = 0;
            BindCriticalLog();
        }

        protected void BtnClearFilters_Click(object sender, EventArgs e)
        {
            TxtSearch.Text = string.Empty;
            TxtFromTime.Text = string.Empty;
            TxtToTime.Text = string.Empty;

            if (DdlRoleFilter.Items.Count > 0) DdlRoleFilter.SelectedIndex = 0;
            if (DdlTypeFilter.Items.Count > 0) DdlTypeFilter.SelectedIndex = 0;
            if (DdlUniversityFilter.Items.Count > 0) DdlUniversityFilter.SelectedIndex = 0;

            CurrentPageIndex = 0;
            BindAuditLog();

            // Also reset and rebind critical events.
            TxtCriticalSearch.Text = string.Empty;
            TxtCriticalFromTime.Text = string.Empty;
            TxtCriticalToTime.Text = string.Empty;

            if (DdlCriticalTypeFilter.Items.Count > 0) DdlCriticalTypeFilter.SelectedIndex = 0;
            if (DdlCriticalUniversityFilter.Items.Count > 0) DdlCriticalUniversityFilter.SelectedIndex = 0;

            CurrentCriticalPageIndex = 0;
            BindCriticalLog();

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

        // Export all currently filtered rows to CSV (ignores paging)
        protected void BtnExportCsv_Click(object sender, EventArgs e)
        {
            var rows = GetFilteredAuditRows();

            Response.Clear();
            Response.ContentType = "text/csv";
            var fileName = $"FIA_SystemAudit_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
            Response.AddHeader("Content-Disposition", "attachment; filename=" + fileName);

            using (var writer = new StringWriter())
            {
                // header
                writer.WriteLine("LocalTimestamp,University,Role,Type,FirstName,Email,Details");

                foreach (var row in rows)
                {
                    var ts = row.TimestampLocal != DateTime.MinValue
                        ? row.TimestampLocal.ToString("yyyy-MM-dd HH:mm:ss")
                        : "";

                    writer.WriteLine(string.Join(",",
                        CsvEscape(ts),
                        CsvEscape(row.University),
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

        private static string CsvEscape(string s)
        {
            if (s == null) return "";
            if (s.Contains("\"") || s.Contains(",") || s.Contains("\r") || s.Contains("\n"))
            {
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            }
            return s;
        }

        private void InitializeFilters()
        {
            // Role filter (values are display labels; underlying role attribute uses no spaces,
            // so filtering below is string-based on the row.Role).
            DdlRoleFilter.Items.Clear();
            DdlRoleFilter.Items.Add(new ListItem("All roles", ""));
            DdlRoleFilter.Items.Add(new ListItem("Participant", "Participant"));
            DdlRoleFilter.Items.Add(new ListItem("Helper", "Helper"));
            DdlRoleFilter.Items.Add(new ListItem("University Admin", "UniversityAdmin"));
            DdlRoleFilter.Items.Add(new ListItem("Super Admin", "SuperAdmin"));

            // Type filter
            DdlTypeFilter.Items.Clear();
            DdlTypeFilter.Items.Add(new ListItem("All types", ""));

            var types = GetAuditTypes();
            foreach (var t in types.OrderBy(x => x))
            {
                DdlTypeFilter.Items.Add(new ListItem(t, t));
            }

            // University filter
            DdlUniversityFilter.Items.Clear();
            DdlUniversityFilter.Items.Add(new ListItem("All universities", ""));

            var unis = GetAuditUniversities();
            foreach (var u in unis.OrderBy(x => x))
            {
                DdlUniversityFilter.Items.Add(new ListItem(u, u));
            }
        }

        private void InitializeCriticalFilters()
        {
            // Critical type filter: only the derived pattern labels
            DdlCriticalTypeFilter.Items.Clear();
            DdlCriticalTypeFilter.Items.Add(new ListItem("All critical types", ""));
            DdlCriticalTypeFilter.Items.Add(new ListItem("Session Updated Near Start", "Session Updated Near Start"));
            DdlCriticalTypeFilter.Items.Add(new ListItem("Session Deleted Near Start", "Session Deleted Near Start"));
            DdlCriticalTypeFilter.Items.Add(new ListItem("Cyberfair Event Burst", "Cyberfair Event Burst"));
            DdlCriticalTypeFilter.Items.Add(new ListItem("Microcourse Change Burst", "Microcourse Change Burst"));
            DdlCriticalTypeFilter.Items.Add(new ListItem("Helper Quiz Burst", "Helper Quiz Burst"));
            DdlCriticalTypeFilter.Items.Add(new ListItem("Helper Delivered Session Burst", "Helper Delivered Session Burst"));
            DdlCriticalTypeFilter.Items.Add(new ListItem("Helper 1:1 Session Burst", "Helper 1:1 Session Burst"));
            DdlCriticalTypeFilter.Items.Add(new ListItem("Late Night University Admin Sign-In", "Late Night University Admin Sign-In"));
            DdlCriticalTypeFilter.Items.Add(new ListItem("Late Night Super Admin Sign-In", "Late Night Super Admin Sign-In"));
            DdlCriticalTypeFilter.Items.Add(new ListItem(
                "Repeated Failed Sign-Ins (Participant/Helper)",
                "Repeated Failed Sign-Ins (Participant/Helper)")
            );
            DdlCriticalTypeFilter.Items.Add(new ListItem(
                "Repeated Failed Admin Sign-Ins",
                "Repeated Failed Admin Sign-Ins")
            );

            DdlCriticalTypeFilter.Items.Add(new ListItem(
                "Repeated Unknown Email Sign-Ins",
                "Repeated Unknown Email Sign-Ins")
            );



            // Critical university filter: reuse the base set of known universities
            DdlCriticalUniversityFilter.Items.Clear();
            DdlCriticalUniversityFilter.Items.Add(new ListItem("All universities", ""));
            var unis = GetAuditUniversities();
            foreach (var u in unis.OrderBy(x => x))
            {
                DdlCriticalUniversityFilter.Items.Add(new ListItem(u, u));
            }
        }

        private HashSet<string> GetAuditTypes()
        {
            var types = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (File.Exists(AuditXmlPath))
            {
                var doc = new XmlDocument();
                doc.Load(AuditXmlPath);
                var nodes = doc.SelectNodes("/auditLog/entry");
                foreach (XmlElement entry in nodes)
                {
                    var typeAttr = entry.GetAttribute("type");
                    if (!string.IsNullOrWhiteSpace(typeAttr))
                    {
                        types.Add(typeAttr.Trim());
                    }
                }
            }

            if (types.Count == 0)
            {
                types.Add("Sign In");
                types.Add("Participant Enroll");
                types.Add("Helper Quiz Completion");
                types.Add("Session Created");
                types.Add("Session Updated");
                types.Add("Consent Updated");
            }

            return types;
        }

        private HashSet<string> GetAuditUniversities()
        {
            var unis = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (File.Exists(AuditXmlPath))
            {
                var doc = new XmlDocument();
                doc.Load(AuditXmlPath);
                var nodes = doc.SelectNodes("/auditLog/entry");
                foreach (XmlElement entry in nodes)
                {
                    var uniAttr = entry.GetAttribute("university");
                    if (!string.IsNullOrWhiteSpace(uniAttr))
                    {
                        unis.Add(uniAttr.Trim());
                    }
                }
            }

            return unis;
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

        /// <summary>
        /// Load all audit entries from XML into in-memory rows, with both UTC and local timestamps.
        /// This is the shared source for both the main audit log and the critical patterns.
        /// </summary>
        private List<AuditRow> LoadAllAuditRows()
        {
            var rows = new List<AuditRow>();

            try
            {
                if (File.Exists(AuditXmlPath))
                {
                    var doc = new XmlDocument();
                    doc.Load(AuditXmlPath);
                    var nodes = doc.SelectNodes("/auditLog/entry");

                    foreach (XmlElement entry in nodes)
                    {
                        var uni = entry.GetAttribute("university");
                        var timestampRaw = entry.GetAttribute("timestamp");
                        var role = entry.GetAttribute("role");
                        var type = entry.GetAttribute("type");
                        var email = entry.GetAttribute("email");
                        var firstName = entry.GetAttribute("firstName");
                        var details = entry["details"]?.InnerText ?? entry.GetAttribute("details");
                        var id = entry.GetAttribute("id");

                        DateTime tsUtc = DateTime.MinValue;
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
                            EntryId = id,
                            University = uni,
                            TimestampDate = dateLabel,
                            TimestampTime = timeLabel,
                            TimestampLocal = localTs,
                            TimestampUtc = tsUtc,
                            Role = role,
                            Type = type,
                            Email = email,
                            FirstName = firstName,
                            Details = details
                        });
                    }
                }
            }
            catch
            {
                // Fail-safe: return whatever we have so far; UI will show an empty log if needed.
            }

            return rows;
        }

        /// <summary>
        /// Central place to load + filter + sort audit rows for the main log.
        /// Uses the UI filters (search, role, type, university, time range).
        /// </summary>
        private List<AuditRow> GetFilteredAuditRows()
        {
            var rows = LoadAllAuditRows();

            var search = (TxtSearch.Text ?? "").Trim().ToLowerInvariant();
            var roleFilter = (DdlRoleFilter.SelectedValue ?? "").Trim();
            var typeFilter = (DdlTypeFilter.SelectedValue ?? "").Trim();
            var uniFilter = (DdlUniversityFilter.SelectedValue ?? "").Trim();
            var fromText = (TxtFromTime.Text ?? "").Trim();
            var toText = (TxtToTime.Text ?? "").Trim();

            var fromLocalOpt = ParseLocal(fromText);
            var toLocalOpt = ParseLocal(toText);
            var hasFrom = fromLocalOpt.HasValue;
            var hasTo = toLocalOpt.HasValue;

            IEnumerable<AuditRow> filtered = rows;

            if (!string.IsNullOrEmpty(roleFilter))
            {
                // role attribute uses values like "UniversityAdmin", "SuperAdmin", etc.
                filtered = filtered.Where(r => string.Equals(r.Role, roleFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(typeFilter))
            {
                filtered = filtered.Where(r => string.Equals(r.Type, typeFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(uniFilter))
            {
                filtered = filtered.Where(r => string.Equals(r.University, uniFilter, StringComparison.OrdinalIgnoreCase));
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
                    (r.Email ?? "").ToLowerInvariant().Contains(search) ||
                    (r.FirstName ?? "").ToLowerInvariant().Contains(search) ||
                    (r.Type ?? "").ToLowerInvariant().Contains(search) ||
                    (r.University ?? "").ToLowerInvariant().Contains(search) ||
                    (r.Details ?? "").ToLowerInvariant().Contains(search));
            }

            var filteredList = filtered
                .OrderByDescending(r => r.TimestampLocal)
                .ToList();

            return filteredList;
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

        // Simple local DateTime.TryParse (same pattern as UniversityAdminAudit)
        private static DateTime? ParseLocal(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            if (DateTime.TryParse(s, out var dt)) return dt;
            return null;
        }

        // ---------- Critical events UI handlers ----------

        protected void BtnCriticalApplyFilters_Click(object sender, EventArgs e)
        {
            CurrentCriticalPageIndex = 0;
            BindCriticalLog();
        }

        protected void BtnCriticalExportCsv_Click(object sender, EventArgs e)
        {
            // Start from the derived critical rows (already respects time window).
            var critical = GetCriticalRows();

            var search = (TxtCriticalSearch.Text ?? "").Trim().ToLowerInvariant();
            var typeFilter = (DdlCriticalTypeFilter.SelectedValue ?? "").Trim();
            var uniFilter = (DdlCriticalUniversityFilter.SelectedValue ?? "").Trim();

            IEnumerable<AuditRow> filtered = critical;

            if (!string.IsNullOrEmpty(typeFilter))
            {
                filtered = filtered.Where(r => string.Equals(r.Type, typeFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(uniFilter))
            {
                filtered = filtered.Where(r => string.Equals(r.University, uniFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(search))
            {
                filtered = filtered.Where(r =>
                    (r.Email ?? "").ToLowerInvariant().Contains(search) ||
                    (r.FirstName ?? "").ToLowerInvariant().Contains(search) ||
                    (r.Type ?? "").ToLowerInvariant().Contains(search) ||
                    (r.University ?? "").ToLowerInvariant().Contains(search) ||
                    (r.Details ?? "").ToLowerInvariant().Contains(search));
            }

            var rows = filtered.OrderByDescending(r => r.TimestampLocal).ToList();

            Response.Clear();
            Response.ContentType = "text/csv";
            var fileName = $"FIA_CriticalAudit_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
            Response.AddHeader("Content-Disposition", "attachment; filename=" + fileName);

            using (var writer = new StringWriter())
            {
                writer.WriteLine("LocalTimestamp,University,Role,Type,FirstName,Email,Details");

                foreach (var row in rows)
                {
                    var ts = row.TimestampLocal != DateTime.MinValue
                        ? row.TimestampLocal.ToString("yyyy-MM-dd HH:mm:ss")
                        : "";

                    writer.WriteLine(string.Join(",",
                        CsvEscape(ts),
                        CsvEscape(row.University),
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


        protected void BtnCriticalClearFilters_Click(object sender, EventArgs e)
        {
            TxtCriticalSearch.Text = string.Empty;
            TxtCriticalFromTime.Text = string.Empty;
            TxtCriticalToTime.Text = string.Empty;

            if (DdlCriticalTypeFilter.Items.Count > 0) DdlCriticalTypeFilter.SelectedIndex = 0;
            if (DdlCriticalUniversityFilter.Items.Count > 0) DdlCriticalUniversityFilter.SelectedIndex = 0;

            CurrentCriticalPageIndex = 0;
            BindCriticalLog();
        }


        protected void BtnCriticalPrevPage_Click(object sender, EventArgs e)
        {
            if (CurrentCriticalPageIndex > 0)
            {
                CurrentCriticalPageIndex--;
                BindCriticalLog();
            }
        }

        protected void BtnCriticalNextPage_Click(object sender, EventArgs e)
        {
            CurrentCriticalPageIndex++;
            BindCriticalLog();
        }

        /// <summary>
        /// Build the derived list of critical events based on the rules:
        /// - Session Updated near start (&lt; 1 hour)
        /// - Session Deleted near start (&lt; 1 hour)
        /// - UA 3+ cyberfair event edits in &lt; 5 minutes
        /// - SA 3+ microcourse edits in &lt; 5 minutes
        /// - Helper 3+ quiz completions in &lt; 1 hour
        /// - Helper 5+ delivered sessions in &lt; 1 hour
        /// - Helper 5+ 1:1 sessions in &lt; 1 hour
        /// - UA sign-in between 00:00–04:00
        /// - SA sign-in between 00:00–04:00
        /// </summary>
        private List<AuditRow> GetCriticalRows()
        {
            var all = LoadAllAuditRows();

            // Respect critical-specific time window if provided; otherwise fall back to main.
            var critFromText = (TxtCriticalFromTime.Text ?? "").Trim();
            var critToText = (TxtCriticalToTime.Text ?? "").Trim();

            var baseFromText = (TxtFromTime.Text ?? "").Trim();
            var baseToText = (TxtToTime.Text ?? "").Trim();

            var fromLocalOpt = ParseLocal(string.IsNullOrWhiteSpace(critFromText) ? baseFromText : critFromText);
            var toLocalOpt = ParseLocal(string.IsNullOrWhiteSpace(critToText) ? baseToText : critToText);

            if (fromLocalOpt.HasValue)
            {
                all = all.Where(r => r.TimestampLocal >= fromLocalOpt.Value).ToList();
            }
            if (toLocalOpt.HasValue)
            {
                all = all.Where(r => r.TimestampLocal <= toLocalOpt.Value).ToList();
            }


            var critical = new List<AuditRow>();

            // 1) Session Updated near start (< 1 hour)
            foreach (var row in all.Where(r =>
                         string.Equals(r.Type, "Session Updated", StringComparison.OrdinalIgnoreCase)))
            {
                var sessionStartUtc = TryExtractUpdatedSessionStartUtc(row.Details);
                if (!sessionStartUtc.HasValue || row.TimestampUtc == DateTime.MinValue) continue;

                var diff = sessionStartUtc.Value - row.TimestampUtc;
                if (diff.TotalMinutes <= 60 && diff.TotalMinutes >= 0)
                {
                    critical.Add(new AuditRow
                    {
                        University = row.University,
                        TimestampLocal = row.TimestampLocal,
                        TimestampDate = row.TimestampDate,
                        TimestampTime = row.TimestampTime,
                        TimestampUtc = row.TimestampUtc,
                        Role = row.Role,
                        Type = "Session Updated Near Start",
                        FirstName = row.FirstName,
                        Email = row.Email,
                        Details = $"Session updated within {Math.Round(diff.TotalMinutes)} minutes of start time. {row.Details}"
                    });
                }
            }

            // 2) Session Deleted near start (< 1 hour)
            foreach (var row in all.Where(r =>
                         string.Equals(r.Type, "Session Deleted", StringComparison.OrdinalIgnoreCase)))
            {
                var sessionStartUtc = TryExtractDeletedSessionStartUtc(row.Details);
                if (!sessionStartUtc.HasValue || row.TimestampUtc == DateTime.MinValue) continue;

                var diff = sessionStartUtc.Value - row.TimestampUtc;
                if (diff.TotalMinutes <= 60 && diff.TotalMinutes >= 0)
                {
                    critical.Add(new AuditRow
                    {
                        University = row.University,
                        TimestampLocal = row.TimestampLocal,
                        TimestampDate = row.TimestampDate,
                        TimestampTime = row.TimestampTime,
                        TimestampUtc = row.TimestampUtc,
                        Role = row.Role,
                        Type = "Session Deleted Near Start",
                        FirstName = row.FirstName,
                        Email = row.Email,
                        Details = $"Session deleted within {Math.Round(diff.TotalMinutes)} minutes of start time. {row.Details}"
                    });
                }
            }

            // Helper to detect bursts per user.
            void DetectBurst(
                IEnumerable<AuditRow> source,
                TimeSpan window,
                int threshold,
                string label,
                List<AuditRow> output)
            {
                var grouped = source
                    .Where(r => r.TimestampUtc != DateTime.MinValue)
                    .GroupBy(r => (r.Email ?? "") + "|" + (r.University ?? ""));

                foreach (var g in grouped)
                {
                    var list = g.OrderBy(r => r.TimestampUtc).ToList();
                    int n = list.Count;
                    int i = 0;

                    while (i < n)
                    {
                        int j = i;
                        while (j + 1 < n && list[j + 1].TimestampUtc - list[i].TimestampUtc <= window)
                        {
                            j++;
                        }

                        int count = j - i + 1;
                        if (count >= threshold)
                        {
                            var first = list[i];
                            var last = list[j];

                            output.Add(new AuditRow
                            {
                                University = last.University,
                                TimestampLocal = last.TimestampLocal,
                                TimestampDate = last.TimestampDate,
                                TimestampTime = last.TimestampTime,
                                TimestampUtc = last.TimestampUtc,
                                Role = last.Role,
                                Type = label,
                                FirstName = last.FirstName,
                                Email = last.Email,
                                Details = $"{count} actions within {window.TotalMinutes} minutes for {last.Role} {last.Email} at {last.University}. Window {first.TimestampLocal:G} – {last.TimestampLocal:G}."
                            });

                            // Move past this burst window to avoid emitting many overlapping rows.
                            i = j + 1;
                        }
                        else
                        {
                            i++;
                        }
                    }
                }
            }

            // 3) UA: 3+ cyberfair event create/edit in < 5 minutes
            var uaEventTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Event Created",
                "Event Title Updated",
                "Event Description Updated",
                "Event Deleted",
                "Event Session Updated"
            };

            var uaEventSource = all.Where(r =>
                string.Equals(r.Role, "UniversityAdmin", StringComparison.OrdinalIgnoreCase) &&
                uaEventTypes.Contains(r.Type));

            DetectBurst(uaEventSource, TimeSpan.FromMinutes(5), 3, "Cyberfair Event Burst", critical);

            // 4) Super Admin: 3+ microcourse changes in < 5 minutes
            var saMicroSource = all.Where(r =>
                string.Equals(r.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase) &&
                (r.Type ?? "").IndexOf("Microcourse", StringComparison.OrdinalIgnoreCase) >= 0);

            DetectBurst(saMicroSource, TimeSpan.FromMinutes(5), 3, "Microcourse Change Burst", critical);

            // 5) Helper: 3 Helper Quiz Completions in < 1 hour
            var helperQuizSource = all.Where(r =>
                string.Equals(r.Role, "Helper", StringComparison.OrdinalIgnoreCase) &&
                (r.Type ?? "").StartsWith("Helper Quiz Completion", StringComparison.OrdinalIgnoreCase));

            DetectBurst(helperQuizSource, TimeSpan.FromHours(1), 3, "Helper Quiz Burst", critical);

            // 6) Helper: 5 Helper Delivered Sessions in < 1 hour
            var helperDeliveredSource = all.Where(r =>
                string.Equals(r.Role, "Helper", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(r.Type, "Helper Delivered Session", StringComparison.OrdinalIgnoreCase));

            DetectBurst(helperDeliveredSource, TimeSpan.FromHours(1), 5, "Helper Delivered Session Burst", critical);

            // 7) Helper: 5 One on One sessions in < 1 hour
            var helperOneOnOneSource = all.Where(r =>
                string.Equals(r.Role, "Helper", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(r.Type, "Helper 1:1 Help Session", StringComparison.OrdinalIgnoreCase));

            DetectBurst(helperOneOnOneSource, TimeSpan.FromHours(1), 5, "Helper 1:1 Session Burst", critical);

            // Failed sign-in bursts (wrong passwords)

            // Participant / Helper: >5 wrong-password attempts in 15 minutes (threshold = 6)
            var phFailedSource = all.Where(r =>
                string.Equals(r.Type, "Sign In Failed (Bad Password)", StringComparison.OrdinalIgnoreCase) &&
                (string.Equals(r.Role, "Participant", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(r.Role, "Helper", StringComparison.OrdinalIgnoreCase)));

            DetectBurst(
                phFailedSource,
                TimeSpan.FromMinutes(15),   // window
                6,                          // >5 attempts => 6+
                "Repeated Failed Sign-Ins (Participant/Helper)",
                critical
            );

            // University / Super Admin: >3 wrong-password attempts in 10 minutes (threshold = 4)
            var adminFailedSource = all.Where(r =>
                string.Equals(r.Type, "Sign In Failed (Bad Password)", StringComparison.OrdinalIgnoreCase) &&
                (string.Equals(r.Role, "UniversityAdmin", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(r.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase)));

            DetectBurst(
                adminFailedSource,
                TimeSpan.FromMinutes(10),   // window
                4,                          // >3 attempts => 4+
                "Repeated Failed Admin Sign-Ins",
                critical
            );

            // Unknown-email brute force: >5 attempts in 15 minutes (threshold = 6)
            // These are entries with type="Sign In Failed (Unknown Email)" and role "Unknown".
            var unknownEmailSource = all.Where(r =>
                string.Equals(r.Type, "Sign In Failed (Unknown Email)", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(r.Email));

            DetectBurst(
                unknownEmailSource,
                TimeSpan.FromMinutes(15),   // window
                6,                          // >5 attempts => 6+
                "Repeated Unknown Email Sign-Ins",
                critical
            );



            // 8) University Admin sign-in between 00:00–04:00
            foreach (var row in all.Where(r =>
                         string.Equals(r.Role, "UniversityAdmin", StringComparison.OrdinalIgnoreCase) &&
                         string.Equals(r.Type, "Sign In", StringComparison.OrdinalIgnoreCase)))
            {
                if (row.TimestampLocal == DateTime.MinValue) continue;
                var hour = row.TimestampLocal.Hour;
                if (hour >= 0 && hour < 4)
                {
                    critical.Add(new AuditRow
                    {
                        University = row.University,
                        TimestampLocal = row.TimestampLocal,
                        TimestampDate = row.TimestampDate,
                        TimestampTime = row.TimestampTime,
                        TimestampUtc = row.TimestampUtc,
                        Role = row.Role,
                        Type = "Late Night University Admin Sign-In",
                        FirstName = row.FirstName,
                        Email = row.Email,
                        Details = $"University Admin sign-in between 12:00–4:00 AM. {row.Details}"
                    });
                }
            }

            // 9) Super Admin sign-in between 00:00–04:00
            foreach (var row in all.Where(r =>
                         string.Equals(r.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase) &&
                         string.Equals(r.Type, "Sign In", StringComparison.OrdinalIgnoreCase)))
            {
                if (row.TimestampLocal == DateTime.MinValue) continue;
                var hour = row.TimestampLocal.Hour;
                if (hour >= 0 && hour < 4)
                {
                    critical.Add(new AuditRow
                    {
                        University = row.University,
                        TimestampLocal = row.TimestampLocal,
                        TimestampDate = row.TimestampDate,
                        TimestampTime = row.TimestampTime,
                        TimestampUtc = row.TimestampUtc,
                        Role = row.Role,
                        Type = "Late Night Super Admin Sign-In",
                        FirstName = row.FirstName,
                        Email = row.Email,
                        Details = $"Super Admin sign-in between 12:00–4:00 AM. {row.Details}"
                    });
                }
            }

            // Sort by newest first so the most recent potential risks are visible.
            return critical
                .OrderByDescending(r => r.TimestampLocal)
                .ToList();
        }

        /// <summary>
        /// When the Super Admin opens the System Audit page, mark the latest
        /// critical-ish event time as "seen" in a cookie so the home-page
        /// banner only appears again when newer critical activity is logged.
        /// </summary>
        private void MarkCriticalEventsAsSeen()
        {
            var latestCriticalUtc = GetLatestCriticalEventUtc();
            if (!latestCriticalUtc.HasValue)
            {
                return;
            }

            var cookie = new HttpCookie("FIA_LastCriticalSeenUtc", latestCriticalUtc.Value.ToString("o"))
            {
                HttpOnly = true,
                Secure = Request.IsSecureConnection,
                Expires = DateTime.UtcNow.AddDays(7) // keep for a week; tweak if you want
            };

            Response.Cookies.Add(cookie);
        }


        /// <summary>
        /// Same definition of "critical-ish" as on SuperAdminHome, used to
        /// compute the latest critical timestamp we consider "seen" here.
        /// </summary>
        private DateTime? GetLatestCriticalEventUtc()
        {
            if (!File.Exists(AuditXmlPath))
            {
                return null;
            }

            var criticalTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Session Updated",
                "Session Deleted",
                "Session Created",
                "Event Created",
                "Event Deleted",
                "Sign In Failed (Bad Password)",
                "Sign In Failed (Unknown Email)",
                "Helper Delivered Session",
                "Helper 1:1 Help Session"
            };

            DateTime? latest = null;

            try
            {
                var doc = new XmlDocument();
                doc.Load(AuditXmlPath);

                var nodes = doc.SelectNodes("/auditLog/entry");
                foreach (XmlElement entry in nodes)
                {
                    var type = entry.GetAttribute("type");
                    if (string.IsNullOrWhiteSpace(type) || !criticalTypes.Contains(type))
                    {
                        continue;
                    }

                    var tsRaw = entry.GetAttribute("timestamp");
                    if (!DateTime.TryParse(
                            tsRaw,
                            null,
                            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                            out var tsUtc))
                    {
                        continue;
                    }

                    if (!latest.HasValue || tsUtc > latest.Value)
                    {
                        latest = tsUtc;
                    }
                }
            }
            catch
            {
                return null;
            }

            return latest;
        }


        private void BindCriticalLog()
        {
            var critical = GetCriticalRows();

            var search = (TxtCriticalSearch.Text ?? "").Trim().ToLowerInvariant();
            var typeFilter = (DdlCriticalTypeFilter.SelectedValue ?? "").Trim();
            var uniFilter = (DdlCriticalUniversityFilter.SelectedValue ?? "").Trim();

            IEnumerable<AuditRow> filtered = critical;

            if (!string.IsNullOrEmpty(typeFilter))
            {
                filtered = filtered.Where(r => string.Equals(r.Type, typeFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(uniFilter))
            {
                filtered = filtered.Where(r => string.Equals(r.University, uniFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(search))
            {
                filtered = filtered.Where(r =>
                    (r.Email ?? "").ToLowerInvariant().Contains(search) ||
                    (r.FirstName ?? "").ToLowerInvariant().Contains(search) ||
                    (r.Type ?? "").ToLowerInvariant().Contains(search) ||
                    (r.University ?? "").ToLowerInvariant().Contains(search) ||
                    (r.Details ?? "").ToLowerInvariant().Contains(search));
            }

            var list = filtered.OrderByDescending(r => r.TimestampLocal).ToList();

            var totalCount = list.Count;
            var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)CriticalPageSize);

            if (CurrentCriticalPageIndex >= totalPages) CurrentCriticalPageIndex = totalPages - 1;
            if (CurrentCriticalPageIndex < 0) CurrentCriticalPageIndex = 0;

            var skip = CurrentCriticalPageIndex * CriticalPageSize;
            var pageRows = list.Skip(skip).Take(CriticalPageSize).ToList();

            NoCriticalPlaceholder.Visible = totalCount == 0;
            CriticalRepeater.DataSource = pageRows;
            CriticalRepeater.DataBind();

            BtnCriticalPrevPage.Enabled = CurrentCriticalPageIndex > 0;
            BtnCriticalNextPage.Enabled = CurrentCriticalPageIndex < totalPages - 1;

            if (totalCount == 0)
            {
                LblCriticalPageInfo.Text = "No critical patterns detected in this range.";
            }
            else
            {
                LblCriticalPageInfo.Text = $"Page {CurrentCriticalPageIndex + 1} of {totalPages} • {totalCount} critical event" +
                                           (totalCount == 1 ? "" : "s");
            }
        }

        /// <summary>
        /// Extract the new session start time from a "Session Updated" details string.
        /// Expects a segment like:
        /// "Start: 2025-11-26T08:50:00.0000000Z → 2025-11-28T22:15:00.0000000Z;"
        /// </summary>
        private static DateTime? TryExtractUpdatedSessionStartUtc(string details)
        {
            if (string.IsNullOrWhiteSpace(details)) return null;

            var marker = "Start:";
            var idx = details.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;

            idx += marker.Length;
            var endIdx = details.IndexOf(';', idx);
            if (endIdx < 0) endIdx = details.Length;

            var segment = details.Substring(idx, endIdx - idx).Trim();
            if (string.IsNullOrEmpty(segment)) return null;

            // If we have an arrow, take the right side (new start), otherwise the left.
            var parts = segment.Split(new[] { '→' }, StringSplitOptions.RemoveEmptyEntries);
            var candidate = parts.Length >= 2 ? parts[1].Trim() : parts[0].Trim();

            if (DateTime.TryParse(candidate, null,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                    out var parsed))
            {
                return parsed;
            }

            return null;
        }

        /// <summary>
        /// Extract the session start time from a "Session Deleted" details string.
        /// Expects "start=2026-01-02T06:36:00.0000000Z."
        /// </summary>
        private static DateTime? TryExtractDeletedSessionStartUtc(string details)
        {
            if (string.IsNullOrWhiteSpace(details)) return null;

            var marker = "start=";
            var idx = details.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;

            idx += marker.Length;
            var endIdx = details.IndexOfAny(new[] { ' ', ',', ';', '.' }, idx);
            if (endIdx < 0) endIdx = details.Length;

            var candidate = details.Substring(idx, endIdx - idx).Trim();
            if (string.IsNullOrEmpty(candidate)) return null;

            if (DateTime.TryParse(candidate, null,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                    out var parsed))
            {
                return parsed;
            }

            return null;
        }

        private class AuditRow
        {
            public string EntryId { get; set; }

            public string University { get; set; }

            public string TimestampDate { get; set; }   // e.g., "Nov 27"
            public string TimestampTime { get; set; }   // e.g., "3:45 PM"
            public DateTime TimestampLocal { get; set; }
            public DateTime TimestampUtc { get; set; }

            public string Role { get; set; }
            public string Type { get; set; }
            public string FirstName { get; set; }
            public string Email { get; set; }
            public string Details { get; set; }
        }
    }
}
