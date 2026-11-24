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
    /// "My Badges" page
    /// - Badges come from dbo.ParticipantBadges (UserRepository.GetBadgesForUser).
    /// - Badge descriptions still come from App_Data/badgeDescriptions.xml (course content, not user data).
    /// - A badge that has a certificate (dbo.Certificates, same user + course) shows a
    ///   "Download certificate" button. Badges without one show no button.
    /// - Test buttons (only while debugging, compilation debug="true"): add a test badge
    ///   with or without a certificate.
    /// </summary>
    public partial class Badges : SecurePage
    {
        private string BadgeDescriptionsXmlPath => Server.MapPath("~/App_Data/badgeDescriptions.xml");

        // Test badges (same course ids every time, so clicking twice doesn't add duplicates)
        private const string TestCourseId = "TEST-CERTIFICATE-COURSE";
        private const string TestCourseTitle = "Test Course (Certificate Demo)";
        private const string TestNoCertCourseId = "TEST-NO-CERTIFICATE-COURSE";
        private const string TestNoCertCourseTitle = "Test Course (No Certificate)";

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

            // The test buttons only exist on development builds (Web.config: compilation debug="true").
            BtnAddTestBadge.Visible = Context.IsDebuggingEnabled;
            BtnAddTestBadgeNoCert.Visible = Context.IsDebuggingEnabled;

            if (!IsPostBack)
            {
                BindBadges(userId);
            }
        }

        //Test badges

        /// <summary>Adds a test badge for the signed-in user, with its certificate.</summary>
        protected void BtnAddTestBadge_Click(object sender, EventArgs e)
        {
            AddTestBadge(TestCourseId, TestCourseTitle, withCertificate: true,
                successMessage: "Test badge added. Its certificate can be downloaded below.");
        }

        /// <summary>Adds a test badge WITHOUT a certificate (it should show no download button).</summary>
        protected void BtnAddTestBadgeNoCert_Click(object sender, EventArgs e)
        {
            AddTestBadge(TestNoCertCourseId, TestNoCertCourseTitle, withCertificate: false,
                successMessage: "Test badge added without a certificate. It should have no download button.");
        }

        private void AddTestBadge(string courseId, string courseTitle, bool withCertificate, string successMessage)
        {
            if (!Context.IsDebuggingEnabled) return;   // never on the live site

            var userId = Session["UserId"] as string;

            try
            {
                UserRepository.AwardBadge(userId, courseId, courseTitle,
                    imagePath: FirstExistingBadgeImage(userId),
                    issueCertificate: withCertificate);
                TestBadgeMessage.Text = successMessage;
            }
            catch (Exception ex)
            {
                TestBadgeMessage.Text = "Couldn't add the test badge: " + Server.HtmlEncode(ex.Message);
            }

            BindBadges(userId);
        }

        /// <summary>Reuses the image of one of the user's real badges for test badges, if any.</summary>
        private static string FirstExistingBadgeImage(string userId)
        {
            try
            {
                return UserRepository.GetBadgesForUser(userId)
                    .Select(b => b.ImagePath)
                    .FirstOrDefault(p => !string.IsNullOrWhiteSpace(p)) ?? "";
            }
            catch (SqlException)
            {
                return "";
            }
        }

        //Showing badges

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

            /// <summary>Verification number shown next to the button (empty if there's no certificate).</summary>
            public string CertificateCode { get; set; }

            /// <summary>Full link an employer can open to verify the certificate (empty if there's no certificate).</summary>
            public string VerifyUrl { get; set; }
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

            List<UserRepository.Badge> badges;
            Dictionary<string, UserRepository.Certificate> certsByCourseId;
            try
            {
                badges = UserRepository.GetBadgesForUser(userId);
                certsByCourseId = UserRepository.GetCertificatesForUser(userId)
                    .GroupBy(c => c.CourseId, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            }
            catch (SqlException)
            {
                // Database briefly unreachable: show the empty state rather than an error page.
                return rows;
            }

            var descByCourseId = LoadDescriptionsByCourseId();

            foreach (var b in badges)
            {
                // Description lookup by courseId (most stable)
                string desc = null;
                if (descByCourseId.TryGetValue(b.CourseId, out var d))
                    desc = d;

                if (string.IsNullOrWhiteSpace(desc))
                    desc = "Completed this FIA microcourse and earned the badge.";

                var local = b.AwardedAtUtc.ToLocalTime();

                // Certificate for this badge's course, if one was issued
                certsByCourseId.TryGetValue(b.CourseId, out var cert);

                rows.Add(new BadgeRow
                {
                    ImageUrl = string.IsNullOrWhiteSpace(b.ImagePath) ? "" : ResolveUrl(b.ImagePath),
                    Title = string.IsNullOrWhiteSpace(b.CourseTitle) ? "Badge" : b.CourseTitle,
                    Description = desc,
                    AwardedOnUtc = b.AwardedAtUtc,
                    EarnedOnLabel = local.ToString("MMM d, yyyy", CultureInfo.CurrentCulture),
                    CourseId = b.CourseId,
                    HasCertificate = cert != null,
                    CertificateCode = cert?.VerificationCode ?? "",
                    VerifyUrl = cert == null ? "" : VerifyLinkFor(cert.VerificationCode),
                    // Shared download page for participants and helpers (~/Account/CertificateDownload.aspx)
                    CertificateUrl = cert == null
                        ? ""
                        : ResolveUrl("~/Account/CertificateDownload.aspx?code=" + HttpUtility.UrlEncode(cert.VerificationCode))
                });
            }

            return rows;
        }

        /// <summary>
        /// Full verification link for sharing, ex. "https://site/VerifyCertificate.aspx?code=FIA-???"
        /// Built from the current site address, so it's right locally and on the live site.
        /// </summary>
        private string VerifyLinkFor(string verificationCode)
        {
            return Request.Url.GetLeftPart(UriPartial.Authority)
                   + ResolveUrl("~/VerifyCertificate.aspx")
                   + "?code=" + HttpUtility.UrlEncode(verificationCode);
        }
    }
}
