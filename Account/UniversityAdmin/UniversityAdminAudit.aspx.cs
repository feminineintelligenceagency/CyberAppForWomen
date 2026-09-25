using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml;

namespace CyberApp_FIA.Account
{
    public partial class UniversityAdminAudit : SecurePage
    {
        private const int PageSize = 25;

        private string AuditXmlPath => Server.MapPath("~/App_Data/Audit_Log/UnvAdminAudit.xml");
        private string UsersXmlPath => Server.MapPath("~/App_Data/users.xml");

        private int CurrentPageIndex
        {
            get => ViewState["AuditPageIndex"] is int i ? i : 0;
            set => ViewState["AuditPageIndex"] = value < 0 ? 0 : value;
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

                var email = (string)Session["Email"] ?? "";
                WelcomeName.Text = email.Length > 0 ? email : "University Admin";

                var uni = (string)Session["University"];
                if (string.IsNullOrWhiteSpace(uni))
                {
                    uni = LookupUniversityByEmail(email);
                }

                UniversityValue.Value = uni ?? "";

                EnsureAuditXml();
                InitializeFilters();
                BindHelpers(uni);
                BindAuditLog();
            }
        }

        protected void BtnBackHome_Click(object sender, EventArgs e)
        {
            // Point back into the UniversityAdmin folder
            Response.Redirect("~/Account/UniversityAdmin/UniversityAdminHome.aspx");
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

            if (DdlRoleFilter.Items.Count > 0) DdlRoleFilter.SelectedIndex = 0;
            if (DdlTypeFilter.Items.Count > 0) DdlTypeFilter.SelectedIndex = 0;

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

        // NEW: export all currently filtered rows to CSV (ignores paging)
        protected void BtnExportCsv_Click(object sender, EventArgs e)
        {
            var rows = GetFilteredAuditRows();

            Response.Clear();
            Response.ContentType = "text/csv";
            var fileName = $"FIA_UniversityAudit_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
            Response.AddHeader("Content-Disposition", "attachment; filename=" + fileName);

            using (var writer = new StringWriter())
            {
                // header
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
            DdlRoleFilter.Items.Clear();
            DdlRoleFilter.Items.Add(new ListItem("All roles", ""));
            DdlRoleFilter.Items.Add(new ListItem("Participant", "Participant"));
            DdlRoleFilter.Items.Add(new ListItem("Helper", "Helper"));
            DdlRoleFilter.Items.Add(new ListItem("University Admin", "UniversityAdmin"));
            DdlRoleFilter.Items.Add(new ListItem("Super Admin", "SuperAdmin"));

            DdlTypeFilter.Items.Clear();
            DdlTypeFilter.Items.Add(new ListItem("All types", ""));

            var types = GetAuditTypes();
            foreach (var t in types.OrderBy(x => x))
            {
                DdlTypeFilter.Items.Add(new ListItem(t, t));
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

        private void BindHelpers(string uni)
        {
            var helpers = new List<HelperRow>();

            try
            {
                if (!string.IsNullOrWhiteSpace(uni) && File.Exists(UsersXmlPath))
                {
                    var doc = new XmlDocument();
                    doc.Load(UsersXmlPath);
                    var nodes = doc.SelectNodes("/users/user");

                    foreach (XmlElement user in nodes)
                    {
                        var roleAttr = user.GetAttribute("role") ?? string.Empty;
                        if (!string.Equals(roleAttr, "Helper", StringComparison.OrdinalIgnoreCase))
                            continue;

                        var userUni = user["university"]?.InnerText ?? "";
                        if (!string.Equals(userUni, uni, StringComparison.OrdinalIgnoreCase))
                            continue;

                        var firstName = user["firstName"]?.InnerText ?? "";
                        var lastName = user["lastName"]?.InnerText ?? "";
                        var email = user["email"]?.InnerText ?? "";

                        var displayName = (firstName + " " + lastName).Trim();
                        if (string.IsNullOrWhiteSpace(displayName))
                        {
                            displayName = email.Length > 0 ? email : "(helper)";
                        }

                        helpers.Add(new HelperRow
                        {
                            DisplayName = displayName,
                            Email = email
                        });
                    }
                }
            }
            catch
            {
                // fall through to empty state
            }

            NoHelpersPlaceholder.Visible = helpers.Count == 0;
            HelpersRepeater.DataSource = helpers;
            HelpersRepeater.DataBind();
        }

        protected void HelpersRepeater_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (!string.Equals(e.CommandName, "viewHelper", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var helperEmail = Convert.ToString(e.CommandArgument ?? string.Empty);
            if (string.IsNullOrWhiteSpace(helperEmail))
            {
                return;
            }

            // Navigate to the helper-scoped audit view
            Response.Redirect(
                "~/Account/UniversityAdmin/UniversityAdminHelperAudit.aspx?helperEmail=" +
                Server.UrlEncode(helperEmail));
        }


        // NEW: central place to load + filter + sort audit rows (used by UI + CSV export)
        private List<AuditRow> GetFilteredAuditRows()
        {
            var uni = UniversityValue.Value ?? "";
            var rows = new List<AuditRow>();

            try
            {
                if (!string.IsNullOrWhiteSpace(uni) && File.Exists(AuditXmlPath))
                {
                    var doc = new XmlDocument();
                    doc.Load(AuditXmlPath);
                    var nodes = doc.SelectNodes("/auditLog/entry");

                    foreach (XmlElement entry in nodes)
                    {
                        var entryUni = entry.GetAttribute("university");
                        if (!string.Equals(entryUni, uni, StringComparison.OrdinalIgnoreCase)) continue;

                        var timestampRaw = entry.GetAttribute("timestamp");
                        var role = entry.GetAttribute("role");
                        var type = entry.GetAttribute("type");
                        var email = entry.GetAttribute("email");
                        var firstName = entry.GetAttribute("firstName");
                        var details = entry["details"]?.InnerText ?? entry.GetAttribute("details");

                        DateTime tsUtc;
                        DateTime localTs;
                        string dateLabel = "";
                        string timeLabel = "";

                        if (DateTime.TryParse(timestampRaw, null, DateTimeStyles.AdjustToUniversal, out tsUtc))
                        {
                            // Convert stored UTC timestamps into the viewer's local time,
                            // then render as two lines: "Nov 27" and "3:45 PM"
                            localTs = tsUtc.ToLocalTime();
                            dateLabel = localTs.ToString("MMM d", CultureInfo.InvariantCulture);
                            timeLabel = localTs.ToString("h:mm tt", CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            // fallback if parse fails
                            localTs = DateTime.MinValue;
                        }

                        rows.Add(new AuditRow
                        {
                            TimestampDate = dateLabel,
                            TimestampTime = timeLabel,
                            TimestampLocal = localTs,
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
                // safe fail to empty log
            }

            var search = (TxtSearch.Text ?? "").Trim().ToLowerInvariant();
            var roleFilter = (DdlRoleFilter.SelectedValue ?? "").Trim();
            var typeFilter = (DdlTypeFilter.SelectedValue ?? "").Trim();
            var fromText = (TxtFromTime.Text ?? "").Trim();
            var toText = (TxtToTime.Text ?? "").Trim();

            var fromLocalOpt = ParseLocal(fromText);
            var toLocalOpt = ParseLocal(toText);
            var hasFrom = fromLocalOpt.HasValue;
            var hasTo = toLocalOpt.HasValue;

            IEnumerable<AuditRow> filtered = rows;

            if (!string.IsNullOrEmpty(roleFilter))
            {
                filtered = filtered.Where(r => string.Equals(r.Role, roleFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(typeFilter))
            {
                filtered = filtered.Where(r => string.Equals(r.Type, typeFilter, StringComparison.OrdinalIgnoreCase));
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
                    (r.Details ?? "").ToLowerInvariant().Contains(search));
            }

            // Newest entries first (simple sort by local timestamp)
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

        // Same ParseLocal pattern as Participant Home (simple local DateTime.TryParse)
        private static DateTime? ParseLocal(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            if (DateTime.TryParse(s, out var dt)) return dt;
            return null;
        }

        private string LookupUniversityByEmail(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email) || !File.Exists(UsersXmlPath)) return "";
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

        private class AuditRow
        {
            // NEW: split timestamp into date + time for 2-line display
            public string TimestampDate { get; set; }   // e.g., "Nov 27"
            public string TimestampTime { get; set; }   // e.g., "3:45 PM"
            public DateTime TimestampLocal { get; set; }

            public string Role { get; set; }
            public string Type { get; set; }
            public string FirstName { get; set; }
            public string Email { get; set; }
            public string Details { get; set; }
        }

        private class HelperRow
        {
            public string DisplayName { get; set; }
            public string Email { get; set; }
        }
    }
}

