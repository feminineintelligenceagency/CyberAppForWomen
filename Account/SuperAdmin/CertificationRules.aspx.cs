using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml;

namespace CyberApp_FIA.Account
{
    /// <summary>
    /// CertificationRules admin page (SuperAdmin only).
    /// - Lists existing certification rules (right-side table)
    /// - Creates/updates/deletes rules
    /// - Simplified fields:
    ///     Rule ID, Name, Description,
    ///     RequireQuiz (toggle), PassScore%,
    ///     Teaching sessions, 1:1 help sessions, Expiry days
    /// - Persists to ~/App_Data/certificationRules.xml
    /// </summary>
    public partial class CertificationRules : SecurePage
    {
        /// <summary>
        /// Physical path to the certification rules datastore.
        /// </summary>
        private string RulesXmlPath => Server.MapPath("~/App_Data/certificationRules.xml");

        /// <summary>
        /// Auth gate (SuperAdmin only) and first-load bindings.
        /// </summary>
        protected void Page_Load(object sender, EventArgs e)
        {
            // ---- Access Gate: only SuperAdmin can manage certification rules ----
            var role = (string)Session["Role"];
            if (!string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
            {
                Response.Redirect("~/Account/Login.aspx");
                return;
            }

            if (!IsPostBack)
            {
                BindRules();
                EnsureQuizPanelVisibility();
            }
        }

        /// <summary>
        /// Ensures the rules XML datastore exists with a <certRules> root.
        /// </summary>
        private void EnsureXml()
        {
            if (File.Exists(RulesXmlPath)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(RulesXmlPath));
            File.WriteAllText(RulesXmlPath, "<?xml version='1.0' encoding='utf-8'?><certRules version='1'></certRules>");
        }

        /// <summary>
        /// Reads all rules and binds them to the repeater on the right.
        /// Shows id, name, requireQuiz, passScore, minSessionsTaught, and minHelpSessions.
        /// </summary>
        private void BindRules()
        {
            EnsureXml();

            var rows = new List<object>();
            var doc = new XmlDocument();
            doc.Load(RulesXmlPath);

            foreach (XmlElement r in doc.SelectNodes("/certRules/rule"))
            {
                var id = r.GetAttribute("id");
                var name = r["name"]?.InnerText ?? string.Empty;
                var passScore = r["passScore"]?.InnerText ?? "0";
                var minSessions = r["minSessionsTaught"]?.InnerText ?? "0";
                var minHelpSessions = r["minHelpSessions"]?.InnerText ?? "0";
                var requireQuiz = (r["requireQuiz"]?.InnerText ?? "false")
                    .Equals("true", StringComparison.OrdinalIgnoreCase);

                rows.Add(new
                {
                    id,
                    name,
                    passScore,
                    minSessions,
                    minHelpSessions,
                    requireQuizText = requireQuiz ? "Yes" : "No"
                });
            }

            NoRulesPH.Visible = rows.Count == 0;
            RulesRepeater.DataSource = rows;
            RulesRepeater.DataBind();
        }

        /// <summary>
        /// Handles command events from the rules repeater (e.g., Edit button).
        /// </summary>
        protected void RulesRepeater_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName == "edit")
            {
                LoadRuleToForm(e.CommandArgument as string);
            }
        }

        /// <summary>
        /// Loads a rule into the left-side edit form by id.
        /// </summary>
        private void LoadRuleToForm(string id)
        {
            EnsureXml();

            var doc = new XmlDocument();
            doc.Load(RulesXmlPath);
            var r = (XmlElement)doc.SelectSingleNode($"/certRules/rule[@id='{id}']");
            if (r == null) return;

            RuleId.Text = r.GetAttribute("id");
            RuleName.Text = r["name"]?.InnerText ?? string.Empty;
            RuleDesc.Text = r["description"]?.InnerText ?? string.Empty;
            PassScore.Text = r["passScore"]?.InnerText ?? "0";
            MinSessions.Text = r["minSessionsTaught"]?.InnerText ?? "0";
            HelpSessions.Text = r["minHelpSessions"]?.InnerText ?? "0";
            ExpiryDays.Text = r["expiryDays"]?.InnerText ?? "0";

            var requireQuizVal = r["requireQuiz"]?.InnerText ?? "false";
            RequireQuiz.Checked = requireQuizVal.Equals("true", StringComparison.OrdinalIgnoreCase);

            EnsureQuizPanelVisibility();

            FormMessage.Text = $"Loaded rule <strong>{RuleId.Text}</strong>.";
        }

        /// <summary>
        /// Save (upsert) the current rule from the edit form.
        /// </summary>
        protected void BtnSave_Click(object sender, EventArgs e)
        {
            EnsureXml();

            var doc = new XmlDocument();
            doc.Load(RulesXmlPath);
            var root = doc.DocumentElement;

            if (string.IsNullOrWhiteSpace(RuleId.Text))
            {
                FormMessage.Text = "<span style='color:#c21d1d'>Rule ID is required.</span>";
                return;
            }

            var trimmedId = RuleId.Text.Trim();

            // Upsert rule
            var r = (XmlElement)doc.SelectSingleNode($"/certRules/rule[@id='{trimmedId}']");
            if (r == null)
            {
                r = doc.CreateElement("rule");
                r.SetAttribute("id", trimmedId);
                r.SetAttribute("version", "1");
                root.AppendChild(r);
            }

            SetOrCreate(doc, r, "name", RuleName.Text);
            SetOrCreate(doc, r, "description", RuleDesc.Text);
            SetOrCreate(doc, r, "passScore", PassScore.Text);
            SetOrCreate(doc, r, "minSessionsTaught", MinSessions.Text);
            SetOrCreate(doc, r, "minHelpSessions", HelpSessions.Text);
            SetOrCreate(doc, r, "expiryDays", ExpiryDays.Text);
            SetOrCreate(doc, r, "requireQuiz", RequireQuiz.Checked ? "true" : "false");

            doc.Save(RulesXmlPath);

            FormMessage.Text = "<span style='color:#0a7a3c'>Rule saved.</span>";
            BindRules();
            EnsureQuizPanelVisibility();
        }

        /// <summary>
        /// Deletes the current rule (if it exists) and refreshes UI.
        /// </summary>
        protected void BtnDelete_Click(object sender, EventArgs e)
        {
            EnsureXml();

            var doc = new XmlDocument();
            doc.Load(RulesXmlPath);
            var trimmedId = RuleId.Text.Trim();
            var r = (XmlElement)doc.SelectSingleNode($"/certRules/rule[@id='{trimmedId}']");
            if (r != null)
            {
                r.ParentNode.RemoveChild(r);
                doc.Save(RulesXmlPath);
            }

            ClearForm();
            BindRules();
            FormMessage.Text = "<span style='color:#0a7a3c'>Rule deleted.</span>";
        }

        /// <summary>
        /// Resets the form without saving.
        /// </summary>
        protected void BtnClear_Click(object sender, EventArgs e)
        {
            ClearForm();
            EnsureQuizPanelVisibility();
            FormMessage.Text = string.Empty;
        }

        /// <summary>
        /// Toggle handler for RequireQuiz checkbox.
        /// Shows or hides the PassScore panel.
        /// </summary>
        protected void RequireQuiz_CheckedChanged(object sender, EventArgs e)
        {
            EnsureQuizPanelVisibility();
        }

        /// <summary>
        /// Ensures PassScorePanel visibility matches RequireQuiz state.
        /// </summary>
        private void EnsureQuizPanelVisibility()
        {
            PassScorePanel.Visible = RequireQuiz.Checked;
        }

        /// <summary>
        /// Helper: set an element's InnerText or create the element if missing.
        /// </summary>
        private static void SetOrCreate(XmlDocument d, XmlElement parent, string name, string value)
        {
            var node = parent[name] ?? d.CreateElement(name);
            node.InnerText = value?.Trim() ?? string.Empty;
            if (node.ParentNode == null) parent.AppendChild(node);
        }

        /// <summary>
        /// Clears all form fields to defaults.
        /// </summary>
        private void ClearForm()
        {
            RuleId.Text = string.Empty;
            RuleName.Text = string.Empty;
            RuleDesc.Text = string.Empty;
            PassScore.Text = "0";
            MinSessions.Text = "0";
            HelpSessions.Text = "0";
            ExpiryDays.Text = "0";
            RequireQuiz.Checked = false;
        }
    }
}

