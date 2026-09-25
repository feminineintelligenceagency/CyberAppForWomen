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
    public partial class SuperAdminMicrocourses : SecurePage
    {
        // Paths to datastores
        private string MicrocoursesXmlPath => Server.MapPath("~/App_Data/microcourses.xml");
        private string RulesXmlPath => Server.MapPath("~/App_Data/certificationRules.xml");

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // Access gate: only SuperAdmin role
                var role = (string)Session["Role"];
                if (!string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
                {
                    Response.Redirect("~/Account/Login.aspx");
                    return;
                }

                WelcomeName.Text = (string)Session["Email"] ?? "Super Admin";

                EnsureMicrocourseXml();
                EnsureRulesXml();

                BindCoursesList();
                BindRuleChoices();
                BindPrereqChoices();
            }
        }

        protected void BtnBackHome_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/Account/SuperAdmin/SuperAdminHome.aspx");
        }

        protected void BtnLogout_Click(object sender, EventArgs e)
        {
            SessionHelper.SignOut(Context);
            Response.Redirect("~/Welcome_Page.aspx");
        }

        // ===================== Binding: list of courses =====================

        private class CourseRow
        {
            public string Id { get; set; }
            public string Title { get; set; }
            public string Status { get; set; }
            public string CreatedAtDisplay { get; set; }
            public string CreatedBy { get; set; }
        }

        private void BindCoursesList()
        {
            var rows = new List<CourseRow>();

            if (File.Exists(MicrocoursesXmlPath))
            {
                var doc = new XmlDocument();
                doc.Load(MicrocoursesXmlPath);

                foreach (XmlElement course in doc.SelectNodes("/microcourses/course"))
                {
                    var id = course.GetAttribute("id");
                    if (string.IsNullOrWhiteSpace(id)) continue;

                    var title = course["title"]?.InnerText ?? "(untitled)";
                    var status = course.GetAttribute("status");
                    var createdBy = course.GetAttribute("createdBy");

                    var createdAtRaw = course.GetAttribute("createdAt");
                    string createdDisplay = createdAtRaw;

                    if (DateTime.TryParse(createdAtRaw, null, DateTimeStyles.AdjustToUniversal, out var createdUtc))
                    {
                        var local = createdUtc.ToLocalTime();
                        createdDisplay = local.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
                    }

                    rows.Add(new CourseRow
                    {
                        Id = id,
                        Title = title,
                        Status = string.IsNullOrWhiteSpace(status) ? "Draft" : status,
                        CreatedAtDisplay = createdDisplay,
                        CreatedBy = createdBy
                    });
                }
            }

            // Sort by created date descending when possible (fall back to title)
            var ordered = rows
                .OrderByDescending(r => r.CreatedAtDisplay)
                .ThenBy(r => r.Title, StringComparer.OrdinalIgnoreCase)
                .ToList();

            NoCoursesPlaceholder.Visible = ordered.Count == 0;
            CoursesRepeater.DataSource = ordered;
            CoursesRepeater.DataBind();
        }

        protected void CoursesRepeater_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (!string.Equals(e.CommandName, "editCourse", StringComparison.OrdinalIgnoreCase))
                return;

            var courseId = Convert.ToString(e.CommandArgument ?? string.Empty);
            if (string.IsNullOrWhiteSpace(courseId))
                return;

            LoadCourseForEdit(courseId);
        }

        // ===================== Binding: rules & prerequisites =====================

        private void BindRuleChoices()
        {
            var items = new List<ListItem>();

            if (File.Exists(RulesXmlPath))
            {
                var doc = new XmlDocument();
                doc.Load(RulesXmlPath);

                foreach (XmlElement r in doc.SelectNodes("/certRules/rule"))
                {
                    var id = r.GetAttribute("id");
                    if (string.IsNullOrWhiteSpace(id)) continue;

                    var name = r["name"]?.InnerText ?? id;
                    items.Add(new ListItem(name, id));
                }
            }

            RulesList.Items.Clear();
            foreach (var li in items)
                RulesList.Items.Add(li);
        }

        private void BindPrereqChoices()
        {
            EnsureMicrocourseXml();

            var items = new List<ListItem>();
            var doc = new XmlDocument();
            doc.Load(MicrocoursesXmlPath);

            foreach (XmlElement c in doc.SelectNodes("/microcourses/course"))
            {
                var id = c.GetAttribute("id");
                if (string.IsNullOrWhiteSpace(id)) continue;

                var title = c["title"]?.InnerText ?? "(untitled)";
                items.Add(new ListItem(title, id));
            }

            PrereqList.Items.Clear();
            foreach (var li in items.OrderBy(i => i.Text, StringComparer.OrdinalIgnoreCase))
                PrereqList.Items.Add(li);
        }

        // ===================== Load course into editor =====================

        private void LoadCourseForEdit(string courseId)
        {
            EnsureMicrocourseXml();

            var doc = new XmlDocument();
            doc.Load(MicrocoursesXmlPath);

            var course = doc.SelectSingleNode($"/microcourses/course[@id='{courseId}']") as XmlElement;
            if (course == null)
            {
                // Nothing to edit; reset editor
                CurrentCourseId.Value = string.Empty;
                NoCourseSelectedPlaceholder.Visible = true;
                EditorPlaceholder.Visible = false;
                return;
            }

            CurrentCourseId.Value = courseId;

            var title = course["title"]?.InnerText ?? "";
            var summary = course["summary"]?.InnerText ?? "";
            var duration = course["duration"]?.InnerText ?? "";
            var externalLink = course["externalLink"]?.InnerText ?? "";
            var status = course.GetAttribute("status");

            // Tags
            var tagsNode = course["tags"];
            string tagsCsv = "";
            if (tagsNode != null)
            {
                var tags = new List<string>();
                foreach (XmlElement t in tagsNode.SelectNodes("tag"))
                {
                    var txt = (t.InnerText ?? "").Trim();
                    if (txt.Length > 0) tags.Add(txt);
                }
                tagsCsv = string.Join(", ", tags);
            }

            TxtTitle.Text = title;
            TxtSummary.Text = summary;
            TxtDuration.Text = duration;
            TxtExternalLink.Text = externalLink;
            TxtTags.Text = tagsCsv;
            if (!string.IsNullOrWhiteSpace(status))
            {
                var item = DdlStatus.Items.FindByValue(status);
                if (item != null)
                {
                    DdlStatus.ClearSelection();
                    item.Selected = true;
                }
            }

            CurrentCourseTitle.Text = title;

            // Clear all selections first
            foreach (ListItem li in RulesList.Items) li.Selected = false;
            foreach (ListItem li in PrereqList.Items) li.Selected = false;

            // Required rules
            var reqNode = course["requiredRules"];
            if (reqNode != null)
            {
                var ruleIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (XmlElement r in reqNode.SelectNodes("rule"))
                {
                    var id = r.GetAttribute("id");
                    if (!string.IsNullOrWhiteSpace(id))
                        ruleIds.Add(id.Trim());
                }

                foreach (ListItem li in RulesList.Items)
                {
                    if (ruleIds.Contains(li.Value))
                        li.Selected = true;
                }
            }

            // Prerequisites
            var prereqNode = course["prerequisites"];
            if (prereqNode != null)
            {
                var prereqIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (XmlElement p in prereqNode.SelectNodes("course"))
                {
                    var id = p.GetAttribute("id");
                    if (!string.IsNullOrWhiteSpace(id))
                        prereqIds.Add(id.Trim());
                }

                foreach (ListItem li in PrereqList.Items)
                {
                    if (prereqIds.Contains(li.Value))
                        li.Selected = true;
                }
            }

            NoCourseSelectedPlaceholder.Visible = false;
            EditorPlaceholder.Visible = true;
            FormMessage.Text = string.Empty;
        }

        // ===================== Save changes =====================

        protected void BtnSaveChanges_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            var courseId = CurrentCourseId.Value;
            if (string.IsNullOrWhiteSpace(courseId))
            {
                FormMessage.Text = "<span style='color:#c21d1d'>No microcourse is selected.</span>";
                return;
            }

            EnsureMicrocourseXml();
            var doc = new XmlDocument();
            doc.Load(MicrocoursesXmlPath);

            var course = doc.SelectSingleNode($"/microcourses/course[@id='{courseId}']") as XmlElement;
            if (course == null)
            {
                FormMessage.Text = "<span style='color:#c21d1d'>Selected microcourse was not found.</span>";
                return;
            }

            // Update fields (create child elements as needed)
            SetOrCreateElement(course, "title", TxtTitle.Text.Trim());
            SetOrCreateElement(course, "summary", TxtSummary.Text.Trim());
            SetOrCreateElement(course, "duration", TxtDuration.Text.Trim());
            SetOrCreateElement(course, "externalLink", TxtExternalLink.Text.Trim());

            // Tags
            var tagsRoot = course["tags"];
            if (tagsRoot == null)
            {
                tagsRoot = doc.CreateElement("tags");
                course.AppendChild(tagsRoot);
            }
            // Clear old tags
            tagsRoot.RemoveAll();

            var tagsCsv = TxtTags.Text ?? "";
            if (!string.IsNullOrWhiteSpace(tagsCsv))
            {
                var parts = tagsCsv.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var raw in parts)
                {
                    var t = raw.Trim();
                    if (t.Length == 0) continue;
                    var tagEl = doc.CreateElement("tag");
                    tagEl.InnerText = t;
                    tagsRoot.AppendChild(tagEl);
                }
            }

            // Status attribute
            var status = (DdlStatus.SelectedValue ?? "").Trim();
            if (string.IsNullOrWhiteSpace(status))
                status = "Draft";
            course.SetAttribute("status", status);

            // Required rules
            var existingReq = course["requiredRules"];
            if (existingReq != null)
            {
                course.RemoveChild(existingReq);
            }
            var newReq = doc.CreateElement("requiredRules");
            var selectedRuleIds = RulesList.Items.Cast<ListItem>()
                .Where(li => li.Selected)
                .Select(li => li.Value)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            foreach (var id in selectedRuleIds)
            {
                var rEl = doc.CreateElement("rule");
                rEl.SetAttribute("id", id.Trim());
                newReq.AppendChild(rEl);
            }
            course.AppendChild(newReq);

            // Prerequisites
            var existingPrereq = course["prerequisites"];
            if (existingPrereq != null)
            {
                course.RemoveChild(existingPrereq);
            }
            var newPrereq = doc.CreateElement("prerequisites");
            var selectedPrereqIds = PrereqList.Items.Cast<ListItem>()
                .Where(li => li.Selected)
                .Select(li => li.Value)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            foreach (var id in selectedPrereqIds)
            {
                var cEl = doc.CreateElement("course");
                cEl.SetAttribute("id", id.Trim());
                newPrereq.AppendChild(cEl);
            }
            course.AppendChild(newPrereq);

            // Save back to disk
            doc.Save(MicrocoursesXmlPath);

            // AUDIT: SuperAdmin edited a microcourse
            try
            {
                var updatedTitle = TxtTitle.Text.Trim();
                UniversityAuditLogger.AppendForCurrentUser(
                    this,
                    "Microcourse Updated",
                    $"SuperAdmin updated microcourse '{updatedTitle}' (id={courseId})."
                );
            }
            catch
            {
                // Audit logging is best-effort; never block the update.
            }

            // Refresh list and editor title (in case title changed)
            BindCoursesList();
            CurrentCourseTitle.Text = TxtTitle.Text.Trim();
            FormMessage.Text = "<span style='color:#0a7a3c'>Microcourse updated.</span>";

        }

        // ===================== Clear editor =====================

        protected void BtnClearEditor_Click(object sender, EventArgs e)
        {
            CurrentCourseId.Value = string.Empty;
            TxtTitle.Text = string.Empty;
            TxtSummary.Text = string.Empty;
            TxtDuration.Text = string.Empty;
            TxtExternalLink.Text = string.Empty;
            TxtTags.Text = string.Empty;
            DdlStatus.ClearSelection();

            foreach (ListItem li in RulesList.Items) li.Selected = false;
            foreach (ListItem li in PrereqList.Items) li.Selected = false;

            CurrentCourseTitle.Text = string.Empty;
            FormMessage.Text = string.Empty;

            NoCourseSelectedPlaceholder.Visible = true;
            EditorPlaceholder.Visible = false;
        }

        // ===================== Delete course =====================

        protected void BtnDeleteCourse_Click(object sender, EventArgs e)
        {
            var courseId = CurrentCourseId.Value;
            if (string.IsNullOrWhiteSpace(courseId))
            {
                FormMessage.Text = "<span style='color:#c21d1d'>No microcourse is selected.</span>";
                return;
            }

            EnsureMicrocourseXml();
            var doc = new XmlDocument();
            doc.Load(MicrocoursesXmlPath);

            var course = doc.SelectSingleNode($"/microcourses/course[@id='{courseId}']") as XmlElement;
            if (course == null)
            {
                FormMessage.Text = "<span style='color:#c21d1d'>Selected microcourse was not found.</span>";
                return;
            }

            // Capture title before removal for logging
            var courseTitle = course["title"]?.InnerText ?? "(untitled)";

            var root = doc.DocumentElement;
            root.RemoveChild(course);
            doc.Save(MicrocoursesXmlPath);

            // AUDIT: SuperAdmin deleted a microcourse
            try
            {
                UniversityAuditLogger.AppendForCurrentUser(
                    this,
                    "Microcourse Deleted",
                    $"SuperAdmin deleted microcourse '{courseTitle}' (id={courseId})."
                );
            }
            catch
            {
                // Audit logging is best-effort; never block deletion.
            }

            // Clear editor and refresh list
            BtnClearEditor_Click(sender, e);
            BindCoursesList();
            FormMessage.Text = "<span style='color:#0a7a3c'>Microcourse deleted.</span>";

        }

        // ===================== Helpers =====================

        private void EnsureMicrocourseXml()
        {
            if (File.Exists(MicrocoursesXmlPath)) return;

            var dir = Path.GetDirectoryName(MicrocoursesXmlPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var init = "<?xml version='1.0' encoding='utf-8'?><microcourses version='1'></microcourses>";
            File.WriteAllText(MicrocoursesXmlPath, init);
        }

        private void EnsureRulesXml()
        {
            if (File.Exists(RulesXmlPath)) return;

            var dir = Path.GetDirectoryName(RulesXmlPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var init = "<?xml version='1.0' encoding='utf-8'?><certRules version='1'></certRules>";
            File.WriteAllText(RulesXmlPath, init);
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
    }
}
