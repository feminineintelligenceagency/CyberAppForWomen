using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Xml;
using CyberApp_FIA.Services;

namespace CyberApp_FIA.Participant
{
    /// <summary>
    /// "My Badges" page.
    /// - Badges come from App_Data/participantBadges.xml (moves to the database later).
    /// - A badge that has a certificate (dbo.Certificates, same user + course) shows a
    ///   "Download certificate" button. Badges without one show no button.
    /// - Test button (only while debugging, compilation debug="true"): adds a test badge
    ///   with a certificate, so the download can be tried without finishing a course.
    /// </summary>
    public partial class Badges : SecurePage
    {
        private string ParticipantBadgesXmlPath => Server.MapPath("~/App_Data/participantBadges.xml");
        private string BadgeDescriptionsXmlPath => Server.MapPath("~/App_Data/badgeDescriptions.xml");

        // Test badges (same course ids every time, so clicking twice doesn't add duplicates)
        private const string TestCourseId = "TEST-CERTIFICATE-COURSE";
        private const string TestCourseTitle = "Test Course (Certificate Demo)";
        private const string TestNoCertCourseId = "TEST-NO-CERTIFICATE-COURSE";
        private const string TestNoCertCourseTitle = "Test Course (No Certificate)";

        // Guards writes to participantBadges.xml
        private static readonly object BadgesFileLock = new object();

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

            // The test button only exists on development builds (Web.config: compilation debug="true").
            // It disappears on the live site, which is built with debug="false".
            BtnAddTestBadge.Visible = Context.IsDebuggingEnabled;
            BtnAddTestBadgeNoCert.Visible = Context.IsDebuggingEnabled;

            if (!IsPostBack)
            {
                BindBadges(userId);
            }
        }

        // ===================== Test badge =====================

        /// <summary>Adds a test badge for the signed-in user and issues its certificate.</summary>
        protected void BtnAddTestBadge_Click(object sender, EventArgs e)
        {
            if (!Context.IsDebuggingEnabled) return;   // never on the live site

            var userId = Session["UserId"] as string;

            try
            {
                AddBadgeToXml(userId, TestCourseId, TestCourseTitle, FirstExistingBadgeImage(userId));
                UserRepository.IssueCertificate(userId, TestCourseId, TestCourseTitle);
                TestBadgeMessage.Text = "Test badge added. Its certificate can be downloaded below.";
            }
            catch (Exception ex)
            {
                TestBadgeMessage.Text = "Couldn't add the test badge: " + Server.HtmlEncode(ex.Message);
            }

            BindBadges(userId);
        }

        /// <summary>Adds a test badge WITHOUT a certificate (it should show no download button).</summary>
        protected void BtnAddTestBadgeNoCert_Click(object sender, EventArgs e)
        {
            if (!Context.IsDebuggingEnabled) return;   // never on the live site

            var userId = Session["UserId"] as string;

            try
            {
                AddBadgeToXml(userId, TestNoCertCourseId, TestNoCertCourseTitle, FirstExistingBadgeImage(userId));
                TestBadgeMessage.Text = "Test badge added without a certificate. It should have no download button.";
            }
            catch (Exception ex)
            {
                TestBadgeMessage.Text = "Couldn't add the test badge: " + Server.HtmlEncode(ex.Message);
            }

            BindBadges(userId);
        }

        /// <summary>
        /// Adds a badge to participantBadges.xml (or does nothing if the user already has a
        /// badge for that course). Creates the file / user element if needed.
        /// </summary>
        private void AddBadgeToXml(string userId, string courseId, string courseTitle, string image)
        {
            lock (BadgesFileLock)
            {
                var doc = new XmlDocument();
                if (File.Exists(ParticipantBadgesXmlPath))
                    doc.Load(ParticipantBadgesXmlPath);
                else
                    doc.LoadXml("<?xml version='1.0' encoding='utf-8'?><participantBadges></participantBadges>");

                var user = doc.SelectSingleNode($"/participantBadges/user[@id={XPathSafe.Lit(userId)}]") as XmlElement;
                if (user == null)
                {
                    user = doc.CreateElement("user");
                    user.SetAttribute("id", userId);
                    doc.DocumentElement.AppendChild(user);
                }

                foreach (XmlElement existing in user.SelectNodes("badge"))
                {
                    if (string.Equals(existing.GetAttribute("courseId"), courseId, StringComparison.OrdinalIgnoreCase))
                        return;   // already has this badge
                }

                var badge = doc.CreateElement("badge");
                badge.SetAttribute("courseId", courseId);
                badge.SetAttribute("courseTitle", courseTitle);
                badge.SetAttribute("image", image ?? "");
                badge.SetAttribute("awardedOnUtc", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
                user.AppendChild(badge);

                doc.Save(ParticipantBadgesXmlPath);
            }
        }

        /// <summary>Reuses the image of one of the user's real badges for the test badge, if any.</summary>
        private string FirstExistingBadgeImage(string userId)
        {
            if (!File.Exists(ParticipantBadgesXmlPath)) return "";
            var doc = new XmlDocument();
            doc.Load(ParticipantBadgesXmlPath);
            var img = doc.SelectSingleNode(
                $"/participantBadges/user[@id={XPathSafe.Lit(userId)}]/badge[@image!='']/@image");
            if (img != null) return img.Value;

            // Otherwise any badge image in the file
            img = doc.SelectSingleNode("/participantBadges/user/badge[@image!='']/@image");
            return img?.Value ?? "";
        }

        // ===================== Showing badges =====================

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

        /// <summary>One badge on the page. Public so the repeater can bind to it.</summary>
        public sealed class BadgeRow
        {
            public string ImageUrl { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public DateTime AwardedOnUtc { get; set; }
            public string EarnedOnLabel { get; set; }

            public string CourseId { get; set; }

            /// <summary>True if this badge has a certificate (shows the download button).</summary>
            public bool HasCertificate { get; set; }

            /// <summary>Download link for the certificate PDF (empty if there's no certificate).</summary>
            public string CertificateUrl { get; set; }

            /// <summary>Verification number shown under the button (empty if there's no certificate).</summary>
            public string CertificateCode { get; set; }
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

        /// <summary>The user's certificates keyed by course id. Empty if the database can't be reached.</summary>
        private Dictionary<string, UserRepository.Certificate> LoadCertificatesByCourseId(string userId)
        {
            var map = new Dictionary<string, UserRepository.Certificate>(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (var c in UserRepository.GetCertificatesForUser(userId))
                    map[c.CourseId] = c;
            }
            catch (SqlException)
            {
                // Badges still show; download buttons just won't appear until the database is back.
            }
            return map;
        }

        private List<BadgeRow> LoadEarnedBadges(string userId)
        {
            var rows = new List<BadgeRow>();

            if (!File.Exists(ParticipantBadgesXmlPath))
                return rows;

            var descByCourseId = LoadDescriptionsByCourseId();
            var certsByCourseId = LoadCertificatesByCourseId(userId);

            var doc = new XmlDocument();
            doc.Load(ParticipantBadgesXmlPath);

            var user = doc.SelectSingleNode($"/participantBadges/user[@id={XPathSafe.Lit(userId)}]") as XmlElement;
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

                // Certificate for this badge's course, if one was issued
                UserRepository.Certificate cert = null;
                if (!string.IsNullOrWhiteSpace(courseId))
                    certsByCourseId.TryGetValue(courseId, out cert);

                rows.Add(new BadgeRow
                {
                    ImageUrl = string.IsNullOrWhiteSpace(image) ? "" : ResolveUrl(image),
                    Title = string.IsNullOrWhiteSpace(title) ? "Badge" : title,
                    Description = desc,
                    AwardedOnUtc = awardedUtc,
                    EarnedOnLabel = local.ToString("MMM d, yyyy", CultureInfo.CurrentCulture),
                    CourseId = courseId,
                    HasCertificate = cert != null,
                    CertificateCode = cert?.VerificationCode ?? "",
                    // Shared download page for participants and helpers (~/Account/CertificateDownload.aspx)
                    CertificateUrl = cert == null
                        ? ""
                        : ResolveUrl("~/Account/CertificateDownload.aspx?code=" + HttpUtility.UrlEncode(cert.VerificationCode))
                });
            }

            return rows;
        }
    }
}