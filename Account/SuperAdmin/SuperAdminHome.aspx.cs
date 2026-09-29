using CyberApp_FIA.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml;


namespace CyberApp_FIA.Account
{
    /// <summary>
    /// SuperAdmin dashboard:
    /// - Creates and saves microcourses to ~/App_Data/microcourses.xml
    /// - Creates and saves microcourses to ~/App_Data/microcourses.xml
    /// - Binds selectable certification rules from ~/App_Data/certificationRules.xml
    /// - NEW: Allows selecting existing microcourses as prerequisites
    /// </summary>
    public partial class SuperAdminHome : Page
    {
        // Path to microcourses datastore (XML with <microcourses><course .../></microcourses>)
        private string MicrocoursesXmlPath => Server.MapPath("~/App_Data/microcourses.xml");

        // Path to certification rules datastore (XML with <certRules><rule .../></certRules>)
        private string RulesXmlPath => Server.MapPath("~/App_Data/certificationRules.xml");

        // Path to the main university audit log used for critical activity
        private string AuditXmlPath => Server.MapPath("~/App_Data/Audit_Log/UnvAdminAudit.xml");

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // ---- Access Gate: only SuperAdmin role ----
                var role = (string)Session["Role"];
                if (!string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
                {
                    Response.Redirect("~/Account/Login.aspx");
                    return;
                }

                // Friendly header text
                WelcomeName.Text = (string)Session["Email"] ?? "Super Admin";

                // Populate choices
                BindRuleChoices();
                BindPrereqChoices();

                // Check for new critical activity since last time they opened the audit log
                UpdateSecurityAlert();
            }
        }

        protected void BtnLogout_Click(object sender, EventArgs e)
        {
            AuthSession.SignOut(Context);   // Epic #7: fully end the session and delete session + token cookies
            Response.Redirect("~/Welcome_Page.aspx");
        }

        protected void BtnSaveMicrocourse_Click(object sender, EventArgs e)
        {
            var title = Title.Text?.Trim();
            var summary = Summary.Text?.Trim();
            var duration = Duration.Text?.Trim();
            var externalLink = ExternalLink.Text?.Trim();
            var tagsCsv = Tags.Text?.Trim();
            var status = Status.SelectedValue?.Trim();

            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(summary) || string.IsNullOrEmpty(duration))
            {
                FormMessage.Text = "<span style='color:#c21d1d'>Please fill in Title, Summary, and Duration.</span>";
                return;
            }

            EnsureMicrocourseXml();

            var doc = new XmlDocument();
            doc.Load(MicrocoursesXmlPath);

            var course = doc.CreateElement("course");
            course.SetAttribute("id", Guid.NewGuid().ToString("N"));
            course.SetAttribute("status", string.IsNullOrEmpty(status) ? "Draft" : status);
            course.SetAttribute("createdAt", DateTime.UtcNow.ToString("o"));
            course.SetAttribute("createdBy", (Session["Email"] as string) ?? "superadmin@unknown");

            course.AppendChild(Mk(doc, "title", title));
            course.AppendChild(Mk(doc, "summary", summary));
            course.AppendChild(Mk(doc, "duration", duration));
            course.AppendChild(Mk(doc, "externalLink", externalLink ?? ""));
            course.AppendChild(MkTags(doc, tagsCsv));

            // Selected certification rules -> <requiredRules><rule id="..."/></requiredRules>
            var selectedRuleIds = RulesList.Items.Cast<ListItem>()
                                      .Where(i => i.Selected)
                                      .Select(i => i.Value)
                                      .Distinct()
                                      .ToList();
            course.AppendChild(MkRequiredRules(doc, selectedRuleIds));

            // NEW: Selected prerequisites -> <prerequisites><course id="..."/></prerequisites>
            var selectedPrereqIds = PrereqList.Items.Cast<ListItem>()
                                        .Where(i => i.Selected)
                                        .Select(i => i.Value)
                                        .Distinct()
                                        .ToList();
            course.AppendChild(MkPrerequisites(doc, selectedPrereqIds));

            // Placeholders for University Admin later (left unchanged)
            course.AppendChild(Mk(doc, "startTime", ""));
            course.AppendChild(Mk(doc, "endTime", ""));
            course.AppendChild(Mk(doc, "maxParticipants", ""));

            doc.DocumentElement.AppendChild(course);
            doc.Save(MicrocoursesXmlPath);

            FormMessage.Text = "<span style='color:#0a7a3c'>Microcourse saved.</span>";
            ClearForm();
            BindRuleChoices();
            BindPrereqChoices(); // refresh to clear selections
        }

        protected void BtnClear_Click(object sender, EventArgs e)
        {
            ClearForm();
            FormMessage.Text = "";
            BindRuleChoices();
            BindPrereqChoices();
        }

        private void ClearForm()
        {
            Title.Text = "";
            Summary.Text = "";
            Duration.Text = "";
            ExternalLink.Text = "";
            Tags.Text = "";
            Status.SelectedValue = "Draft";

            foreach (ListItem li in RulesList.Items) li.Selected = false;
            foreach (ListItem li in PrereqList.Items) li.Selected = false; // NEW
        }

        // =========================
        // Helpers: Datastore setup & binding
        // =========================

        private void EnsureMicrocourseXml()
        {
            if (File.Exists(MicrocoursesXmlPath)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(MicrocoursesXmlPath));
            var init = "<?xml version='1.0' encoding='utf-8'?><microcourses version='1'></microcourses>";
            File.WriteAllText(MicrocoursesXmlPath, init);
        }

        private void EnsureRulesXml()
        {
            if (File.Exists(RulesXmlPath)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(RulesXmlPath));
            File.WriteAllText(RulesXmlPath, "<?xml version='1.0' encoding='utf-8'?><certRules version='1'></certRules>");
        }

        private void BindRuleChoices()
        {
            EnsureRulesXml();

            var doc = new XmlDocument();
            doc.Load(RulesXmlPath);

            var items = new List<ListItem>();
            foreach (XmlElement r in doc.SelectNodes("/certRules/rule"))
            {
                var id = r.GetAttribute("id");
                var name = r["name"]?.InnerText ?? id;
                if (string.IsNullOrWhiteSpace(id)) continue;

                items.Add(new ListItem($"{name}"));
            }

            RulesList.DataSource = items;
            RulesList.DataTextField = "Text";
            RulesList.DataValueField = "Value";
            RulesList.DataBind();
        }

        // NEW: load published microcourses for prerequisites
        private void BindPrereqChoices()
        {
            EnsureMicrocourseXml();

            var doc = new XmlDocument();
            doc.Load(MicrocoursesXmlPath);

            var items = new List<ListItem>();
            foreach (XmlElement c in doc.SelectNodes("/microcourses/course[@status='Published' or @status='Draft']"))
            {
                var id = c.GetAttribute("id");
                if (string.IsNullOrWhiteSpace(id)) continue;

                var title = c["title"]?.InnerText ?? "(untitled)";
                // Show Title (ID) for clarity to admins
                items.Add(new ListItem($"{title}"));
            }

            PrereqList.DataSource = items
                .OrderBy(i => i.Text, StringComparer.OrdinalIgnoreCase) // nicer alphabetical display
                .ToList();
            PrereqList.DataTextField = "Text";
            PrereqList.DataValueField = "Value";
            PrereqList.DataBind();
        }

        // =========================
        // Security alert helpers (critical activity)
        // =========================

        /// <summary>
        /// Show/hide the big security banner depending on whether there are
        /// critical-ish audit entries newer than the last time the admin
        /// opened the System Audit Log. Uses a cookie so it survives logout/login.
        /// </summary>
        private void UpdateSecurityAlert()
        {
            var latestCriticalUtc = GetLatestCriticalEventUtc();
            if (!latestCriticalUtc.HasValue)
            {
                SecurityAlertPanel.Visible = false;
                return;
            }

            // Default: they've never seen anything
            DateTime lastSeenUtc = DateTime.MinValue;

            // Read last-seen value from cookie instead of Session
            var cookie = Request.Cookies["FIA_LastCriticalSeenUtc"];
            if (cookie != null)
            {
                if (DateTime.TryParse(
                        cookie.Value,
                        null,
                        DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                        out var parsed))
                {
                    lastSeenUtc = parsed;
                }
            }

            // Only show if there's something newer than what they've already reviewed
            SecurityAlertPanel.Visible = latestCriticalUtc.Value > lastSeenUtc;
        }


        /// <summary>
        /// Lightweight definition of "critical activity" for the banner.
        /// We treat certain audit types (session changes, sign-in failures, etc.)
        /// as critical-ish and return the newest timestamp among them.
        /// </summary>
        private DateTime? GetLatestCriticalEventUtc()
        {
            if (!File.Exists(AuditXmlPath))
            {
                return null;
            }

            // Base audit types we consider "critical-ish"
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
                // Fail-safe: if anything goes wrong, treat as "no critical events"
                return null;
            }

            return latest;
        }


        // =========================
        // Helpers: XML element builders
        // =========================

        private static XmlElement Mk(XmlDocument d, string name, string val)
        {
            var el = d.CreateElement(name);
            el.InnerText = val ?? "";
            return el;
        }

        private static XmlElement MkTags(XmlDocument d, string tagsCsv)
        {
            var tags = d.CreateElement("tags");
            if (!string.IsNullOrWhiteSpace(tagsCsv))
            {
                var parts = tagsCsv.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var raw in parts)
                {
                    var t = raw.Trim();
                    if (t.Length == 0) continue;
                    var tag = d.CreateElement("tag");
                    tag.InnerText = t;
                    tags.AppendChild(tag);
                }
            }
            return tags;
        }

        private static XmlElement MkRequiredRules(XmlDocument d, IEnumerable<string> ruleIds)
        {
            var container = d.CreateElement("requiredRules");
            if (ruleIds == null) return container;

            foreach (var id in ruleIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct())
            {
                var el = d.CreateElement("rule");
                el.SetAttribute("id", id.Trim());
                container.AppendChild(el);
            }
            return container;
        }

        // NEW: prerequisites block
        private static XmlElement MkPrerequisites(XmlDocument d, IEnumerable<string> courseIds)
        {
            var container = d.CreateElement("prerequisites");
            if (courseIds == null) return container;

            foreach (var id in courseIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct())
            {
                var el = d.CreateElement("course");
                el.SetAttribute("id", id.Trim());
                container.AppendChild(el);
            }
            return container;
        }
    }
}

