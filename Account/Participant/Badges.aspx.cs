using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Xml;

namespace CyberApp_FIA.Participant
{
    public partial class Badges : SecurePage
    {
        private string ParticipantBadgesXmlPath => Server.MapPath("~/App_Data/participantBadges.xml");
        private string BadgeDescriptionsXmlPath => Server.MapPath("~/App_Data/badgeDescriptions.xml");

        protected void Page_Load(object sender, EventArgs e)
        {
            var role = (string)Session["Role"];
            if (!string.Equals(role, "Participant", StringComparison.OrdinalIgnoreCase))
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

            if (!IsPostBack)
            {
                BindBadges(userId);
            }
        }

        private void BindBadges(string userId)
        {
            var earned = LoadEarnedBadges(userId);
            if (earned.Count == 0)
            {
                EmptyPH.Visible = true;
                BadgesRepeater.DataSource = null;
                BadgesRepeater.DataBind();
                return;
            }

            EmptyPH.Visible = false;
            BadgesRepeater.DataSource = earned
                .OrderByDescending(x => x.AwardedOnUtc)
                .ToList();
            BadgesRepeater.DataBind();
        }

        private sealed class BadgeRow
        {
            public string ImageUrl { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public DateTime AwardedOnUtc { get; set; }
            public string EarnedOnLabel { get; set; }
        }

        private Dictionary<string, string> LoadDescriptionsByCourseId()
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (!File.Exists(BadgeDescriptionsXmlPath))
                return map;

            var doc = new XmlDocument();
            doc.Load(BadgeDescriptionsXmlPath);

            foreach (XmlElement b in doc.SelectNodes("/badgeDescriptions/badge"))
            {
                var courseId = (b.GetAttribute("courseId") ?? "").Trim();
                var desc = (b["description"]?.InnerText ?? "").Trim();

                if (!string.IsNullOrWhiteSpace(courseId) && !string.IsNullOrWhiteSpace(desc))
                    map[courseId] = desc;
            }

            return map;
        }

        private List<BadgeRow> LoadEarnedBadges(string userId)
        {
            var rows = new List<BadgeRow>();

            if (!File.Exists(ParticipantBadgesXmlPath))
                return rows;

            var descByCourseId = LoadDescriptionsByCourseId();

            var doc = new XmlDocument();
            doc.Load(ParticipantBadgesXmlPath);

            var user = doc.SelectSingleNode($"/participantBadges/user[@id='{userId}']") as XmlElement;
            if (user == null)
                return rows;

            foreach (XmlElement b in user.SelectNodes("badge"))
            {
                var courseId = (b.GetAttribute("courseId") ?? "").Trim();
                var title = (b.GetAttribute("courseTitle") ?? "").Trim();
                var image = (b.GetAttribute("image") ?? "").Trim();
                var awardedRaw = (b.GetAttribute("awardedOnUtc") ?? "").Trim();

                if (!DateTime.TryParse(awardedRaw, CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var awardedUtc))
                {
                    awardedUtc = DateTime.UtcNow;
                }

                // Description lookup by courseId (most stable)
                string desc = null;
                if (!string.IsNullOrWhiteSpace(courseId) && descByCourseId.TryGetValue(courseId, out var d))
                    desc = d;

                if (string.IsNullOrWhiteSpace(desc))
                    desc = "Completed this FIA microcourse and earned the badge.";

                var local = DateTime.SpecifyKind(awardedUtc, DateTimeKind.Utc).ToLocalTime();

                rows.Add(new BadgeRow
                {
                    ImageUrl = ResolveUrl(image),
                    Title = string.IsNullOrWhiteSpace(title) ? "Badge" : title,
                    Description = desc,
                    AwardedOnUtc = awardedUtc,
                    EarnedOnLabel = local.ToString("MMM d, yyyy", CultureInfo.CurrentCulture)
                });
            }

            return rows;
        }
    }
}