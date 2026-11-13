using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using CyberApp_FIA.Services;
using System.IO;
using System.Xml;

namespace CyberApp_FIA.Account.Participant
{
    public partial class ParticipantScoreWidget : UserControl
    {
        private string UserKey => Page.Session["UserId"] as string ?? "guest";

        private string CompletionsXmlPath => Page.Server.MapPath("~/App_Data/completions.xml");
        private string MicrocoursesXmlPath => Page.Server.MapPath("~/App_Data/microcourses.xml");

        private HashSet<string> _completedCourseIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, string> _domainToCourseId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        protected void Page_Load(object sender, EventArgs e)
        {
            _completedCourseIds = LoadCompletedCourseIds(UserKey);
            _domainToCourseId = BuildDomainToCourseIdMap();

            if (!IsPostBack)
            {
                var svc = new QuizService(Page.Server);
                var res = svc.LoadLatestResult(UserKey);

                if (res == null)
                {
                    // No score yet — keep things minimal, disable share
                    PillScore.InnerText = "Take the quiz";
                    PillScore.Attributes["class"] = "pill"; // neutral pill
                    RptDomainMini.DataSource = Enumerable.Empty<object>();
                    RptDomainMini.DataBind();
                    
                    return;
                }

                // Show overall with banded color (supports v1 0–100 or v2 0–10)
                var overallRaw = res.OverallScore;
                var overall10 = NormalizeToTen(overallRaw);
                PillScore.InnerText = overall10.ToString("0.0", CultureInfo.InvariantCulture);
                PillScore.Attributes["class"] = "pill " + BandClass(overall10); // pill low/med/high

                // Domains: List<KeyValuePair<string,double>>
                // The repeater ItemDataBound handles coloring and chip text
                RptDomainMini.DataSource = res.DomainScores.ToList();
                RptDomainMini.DataBind();

                
            }
        }

        protected void RptDomainMini_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != System.Web.UI.WebControls.ListItemType.Item &&
                e.Item.ItemType != System.Web.UI.WebControls.ListItemType.AlternatingItem)
                return;

            var kv = (KeyValuePair<string, double>)e.Item.DataItem;
            var cell = (HtmlGenericControl)e.Item.FindControl("Cell");
            var chip = (HtmlGenericControl)e.Item.FindControl("Chip");
            var completedBadge = (HtmlGenericControl)e.Item.FindControl("CompletedBadge");

            var score10 = NormalizeToTen(kv.Value);
            chip.InnerText = score10.ToString("0.0", CultureInfo.InvariantCulture);

            var band = BandClass(score10);
            cell.Attributes["class"] = "cell " + band;
            chip.Attributes["class"] = "chip " + band;

            var courseId = ResolveCourseIdForDomain(kv.Key);
            completedBadge.Visible =
                !string.IsNullOrWhiteSpace(courseId) &&
                _completedCourseIds.Contains(courseId);
        }

        

        // --- Helpers ---

        /// <summary>
        /// Converts either a 0–10 or 0–100 score into 0–10 for consistent display.
        /// </summary>
        private static double NormalizeToTen(double raw)
        {
            // Heuristic: values > 10 are assumed legacy 0–100
            return raw > 10.0 ? Math.Max(0.0, Math.Min(10.0, raw / 10.0)) : Math.Max(0.0, Math.Min(10.0, raw));
        }

        /// <summary>
        /// Returns "low" (0–3.9), "med" (4.0–6.9), or "high" (7.0–10.0) for styling.
        /// Higher = higher priority (pink).
        /// </summary>
        private static string BandClass(double tenScale)
        {
            if (tenScale >= 7.0) return "high";    // pink (highest priority)
            if (tenScale >= 4.0) return "med";     // blue (moderate)
            return "low";                           // teal (lower priority)
        }

        private HashSet<string> LoadCompletedCourseIds(string userId)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(userId) || !File.Exists(CompletionsXmlPath))
                return set;

            var doc = new XmlDocument();
            doc.Load(CompletionsXmlPath);

            foreach (XmlElement c in doc.SelectNodes($"/completions/user[@id={XPathSafe.Lit(userId)}]/course"))
            {
                var id = c.GetAttribute("id");
                if (!string.IsNullOrWhiteSpace(id))
                    set.Add(id);
            }

            return set;
        }

        private Dictionary<string, string> BuildDomainToCourseIdMap()
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (!File.Exists(MicrocoursesXmlPath))
                return map;

            var doc = new XmlDocument();
            doc.Load(MicrocoursesXmlPath);

            foreach (XmlElement c in doc.SelectNodes("/microcourses/course"))
            {
                var id = c.GetAttribute("id");
                var title = (c["title"]?.InnerText ?? "").Trim();

                if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(title))
                    map[NormalizeKey(title)] = id;
            }

            TryAddAlias(map, "Two-Factor Authentication Setup and Management", "2FA Setup And Management");
            TryAddAlias(map, "Detecting Spyware Infections on Devices", "Detecting Spyware Infection on Devices");
            TryAddAlias(map, "Managing Your Digital Footprint", "Managing Digital Footprint");
            TryAddAlias(map, "Identifying Hidden Surveillance Devices (Electronic Scanning)", "Identifying Hidden-Surveilance Devices");

            return map;
        }

        private void TryAddAlias(Dictionary<string, string> map, string domainTitle, string courseTitle)
        {
            var normalizedCourseTitle = NormalizeKey(courseTitle);
            if (!map.TryGetValue(normalizedCourseTitle, out var courseId))
                return;

            map[NormalizeKey(domainTitle)] = courseId;
        }

        private string ResolveCourseIdForDomain(string domainTitle)
        {
            if (string.IsNullOrWhiteSpace(domainTitle))
                return null;

            _domainToCourseId.TryGetValue(NormalizeKey(domainTitle), out var courseId);
            return courseId;
        }

        private static string NormalizeKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            value = value.Replace("&", " and ")
                         .Replace("-", " ")
                         .Replace("/", " ")
                         .Replace("(", " ")
                         .Replace(")", " ");

            var chars = value.ToLowerInvariant()
                             .Select(ch => char.IsLetterOrDigit(ch) ? ch : ' ')
                             .ToArray();

            return string.Join(
                " ",
                new string(chars).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        }
    }
}

