using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Xml;

namespace CyberApp_FIA.Helper
{
    // Base class changed from Page to SecurePage
    public partial class Badges : SecurePage
    {
        private string HelperProgressXmlPath => Server.MapPath("~/App_Data/helperProgress.xml");
        private string BadgeDescriptionsXmlPath => Server.MapPath("~/App_Data/badgeDescriptions.xml");

        private const int Tier1Threshold = 5;
        private const int Tier2Threshold = 10;
        private const int Tier3Threshold = 20;

        private static readonly Dictionary<string, string> HelperBadgeImageByCourseId =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "f98b4b69dc334c9496ca948351e843b7", "SMSettingsHelper.png" },
            { "550051f55dad499c850dc2e94289a941", "PhishingHelper.png" },
            { "d1c1c3f5edd04cc4abed5927d16eff3f", "PopAppsHelper.png" },
            { "5138068e2bf6482fa1c3aaa0a287f096", "SpywareHelper.png" },
            { "154085e49eb7427b95e967c0af05c870", "2FAHelper.png" },
            { "83b7b2cadf3446d3a81d4e9836cd5982", "PassManagementHelper.png" },
            { "a0e70ced9b0649b68eb6128b2ed2ad7c", "FootprintHelper.png" },
            { "eb171af242e14d3c82db92678ed73b3d", "AIHelper.png" },
            { "d05390f150da4af599b3b66b7b37ef79", "VPNHelper.png" },
            { "ba9ef2af382e4f8e80e2c4357c7993b1", "PublicComputersHelper.png" },
            { "3d36f5f0f13d42b78653b71ba4089a37", "ElectronicScanningHelper.png" },
            { "caebb3c577994d098c5086f17c4f7464", "BankingHelper.png" },
            { "0c1e0fd46a0244ad8176b65ea44b05f1", "MaliciousAppsHelper.png" },
            { "2415376ae813427bb8fb61f2652fb6d3", "IdentityHelper.png" },
            { "97d38c5f9c0246fd9d3ba79014ab56cc", "HomeNetHelper.png" }
        };

        protected void Page_Load(object sender, EventArgs e)
        {
            var role = (string)Session["Role"];
            if (!string.Equals(role, "Helper", StringComparison.OrdinalIgnoreCase))
            {
                Response.Redirect("~/Account/Login.aspx");
                return;
            }

            var helperId = Session["UserId"] as string;
            if (string.IsNullOrWhiteSpace(helperId))
            {
                Response.Redirect("~/Account/Login.aspx");
                return;
            }

            if (!IsPostBack)
            {
                BindBadges(helperId);
            }
        }

        private sealed class BadgeRow
        {
            public string ImageUrl { get; set; }
            public string CourseTitle { get; set; }
            public string Description { get; set; }
            public string TierLabel { get; set; }
            public string ProgressLabel { get; set; }
            public int TierSortOrder { get; set; }
        }

        private sealed class CourseDescriptionInfo
        {
            public string Title { get; set; }
            public string Description { get; set; }
        }

        private void BindBadges(string helperId)
        {
            var rows = LoadHelperBadgesFromProgress(helperId);

            if (rows.Count == 0)
            {
                EmptyPH.Visible = true;
                BadgesRepeater.DataSource = null;
                BadgesRepeater.DataBind();
                return;
            }

            EmptyPH.Visible = false;
            BadgesRepeater.DataSource = rows
                .OrderBy(r => r.CourseTitle)
                .ToList();
            BadgesRepeater.DataBind();
        }

        private Dictionary<string, CourseDescriptionInfo> LoadDescriptionsByCourseId()
        {
            var map = new Dictionary<string, CourseDescriptionInfo>(StringComparer.OrdinalIgnoreCase);

            if (!File.Exists(BadgeDescriptionsXmlPath))
                return map;

            var doc = new XmlDocument();
            doc.Load(BadgeDescriptionsXmlPath);

            foreach (XmlElement b in doc.SelectNodes("/badgeDescriptions/badge"))
            {
                var courseId = (b.GetAttribute("courseId") ?? "").Trim();
                var title = (b.GetAttribute("title") ?? "").Trim();
                var desc = (b["description"]?.InnerText ?? "").Trim();

                if (!string.IsNullOrWhiteSpace(courseId))
                {
                    map[courseId] = new CourseDescriptionInfo
                    {
                        Title = title,
                        Description = desc
                    };
                }
            }

            return map;
        }

        private List<BadgeRow> LoadHelperBadgesFromProgress(string helperId)
        {
            var rows = new List<BadgeRow>();

            if (!File.Exists(HelperProgressXmlPath))
                return rows;

            var descByCourseId = LoadDescriptionsByCourseId();

            var doc = new XmlDocument();
            doc.Load(HelperProgressXmlPath);

            var helper = doc.SelectSingleNode($"/helperProgress/helper[@id='{helperId}']") as XmlElement;
            if (helper == null)
                return rows;

            foreach (XmlElement course in helper.SelectNodes("course"))
            {
                var courseId = (course.GetAttribute("id") ?? "").Trim();
                if (string.IsNullOrWhiteSpace(courseId))
                    continue;

                var teachingSessions = ParseIntSafe(course["teachingSessions"]?.InnerText);
                if (teachingSessions < Tier1Threshold)
                    continue;

                if (!HelperBadgeImageByCourseId.TryGetValue(courseId, out var baseFile) || string.IsNullOrWhiteSpace(baseFile))
                    continue;

                string courseTitle = (course["title"]?.InnerText ?? "").Trim();
                string description = null;

                if (descByCourseId.TryGetValue(courseId, out var info))
                {
                    if (string.IsNullOrWhiteSpace(courseTitle))
                        courseTitle = info.Title;

                    description = info.Description;
                }

                if (string.IsNullOrWhiteSpace(courseTitle))
                    courseTitle = "Microcourse badge";

                if (string.IsNullOrWhiteSpace(description))
                    description = "Earned by teaching this microcourse and supporting participants.";

                string tierLabel;
                string imageUrl;
                int tierSortOrder;

                if (teachingSessions >= Tier3Threshold)
                {
                    tierLabel = "Tier3 badge";
                    imageUrl = ResolveUrl("~/Tier3HelperBadges/Tier3" + baseFile);
                    tierSortOrder = 3;
                }
                else if (teachingSessions >= Tier2Threshold)
                {
                    tierLabel = "Tier2 badge";
                    imageUrl = ResolveUrl("~/Tier2HelperBadge/Tier2" + baseFile);
                    tierSortOrder = 2;
                }
                else
                {
                    tierLabel = "Tier1 badge";
                    imageUrl = ResolveUrl("~/Tier1HelperBadge/Tier1" + baseFile);
                    tierSortOrder = 1;
                }

                rows.Add(new BadgeRow
                {
                    ImageUrl = imageUrl,
                    CourseTitle = courseTitle,
                    Description = description,
                    TierLabel = tierLabel,
                    ProgressLabel = teachingSessions + " teaching sessions",
                    TierSortOrder = tierSortOrder
                });
            }

            return rows;
        }

        private static int ParseIntSafe(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return 0;

            int value;
            return int.TryParse(raw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value)
                ? value
                : 0;
        }
    }
}