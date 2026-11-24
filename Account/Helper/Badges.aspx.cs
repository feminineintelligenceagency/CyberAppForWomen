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

namespace CyberApp_FIA.Helper
{
    /// <summary>
    /// Helper "Your badges" page.
    /// - Badges are worked out from App_Data/helperProgress.xml (teaching sessions per course):
    ///   Tier1 at 5 sessions, Tier2 at 10, Tier3 at 20.
    /// - Each course + tier can have its own certificate in dbo.Certificates
    ///   (CourseId stored as "{courseId}-T{tier}", e.g. "550051f5...-T2").
    ///   A badge shows a "Download certificate" button only if its current tier has a certificate.
    /// - Test button (only while debugging, compilation debug="true"): adds a Tier1 test badge
    ///   with a certificate, so the download can be tried without teaching 5 sessions.
    /// </summary>
    // Base class changed from Page to SecurePage
    public partial class Badges : SecurePage
    {
        private string HelperProgressXmlPath => Server.MapPath("~/App_Data/helperProgress.xml");
        private string BadgeDescriptionsXmlPath => Server.MapPath("~/App_Data/badgeDescriptions.xml");

        private const int Tier1Threshold = 5;
        private const int Tier2Threshold = 10;
        private const int Tier3Threshold = 20;

        // Test badge: uses the Phishing helper image so it has a picture
        private const string TestCourseId = "TEST-HELPER-COURSE";
        private const string TestCourseTitle = "Test Course (Helper Certificate Demo)";
        private const string TestBadgeImage = "PhishingHelper.png";
        private const string TestNoCertCourseId = "TEST-HELPER-NO-CERT-COURSE";
        private const string TestNoCertCourseTitle = "Test Course (Helper, No Certificate)";

        // Guards writes to helperProgress.xml
        private static readonly object ProgressFileLock = new object();

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
            { "97d38c5f9c0246fd9d3ba79014ab56cc", "HomeNetHelper.png" },
            { TestCourseId, TestBadgeImage },        // development test badges
            { TestNoCertCourseId, TestBadgeImage }
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

            // The test button only exists on development builds (Web.config: compilation debug="true").
            BtnAddTestBadge.Visible = Context.IsDebuggingEnabled;
            BtnAddTestBadgeNoCert.Visible = Context.IsDebuggingEnabled;

            if (!IsPostBack)
            {
                BindBadges(helperId);
            }
        }

        // ===================== Certificates =====================

        /// <summary>
        /// Certificate key for a helper badge: one certificate per course per tier,
        /// so reaching Tier2 can come with a new certificate.
        /// </summary>
        public static string HelperCertificateCourseId(string courseId, int tier) => courseId + "-T" + tier;

        /// <summary>Title printed on a helper certificate, e.g. "Helper Tier 2: Phishing Basics".</summary>
        public static string HelperCertificateTitle(string courseTitle, int tier) => "Helper Tier " + tier + ": " + courseTitle;

        // ===================== Test badge =====================

        /// <summary>Adds a Tier1 test badge for the signed-in helper and issues its certificate.</summary>
        protected void BtnAddTestBadge_Click(object sender, EventArgs e)
        {
            if (!Context.IsDebuggingEnabled) return;   // never on the live site

            var helperId = Session["UserId"] as string;

            try
            {
                AddTestCourseToProgress(helperId, TestCourseId, TestCourseTitle);
                UserRepository.IssueCertificate(helperId,
                    HelperCertificateCourseId(TestCourseId, 1),
                    HelperCertificateTitle(TestCourseTitle, 1));
                TestBadgeMessage.Text = "Test badge added. Its certificate can be downloaded below.";
            }
            catch (Exception ex)
            {
                TestBadgeMessage.Text = "Couldn't add the test badge: " + Server.HtmlEncode(ex.Message);
            }

            BindBadges(helperId);
        }

        /// <summary>Adds a Tier1 test badge WITHOUT a certificate (it should show no download button).</summary>
        protected void BtnAddTestBadgeNoCert_Click(object sender, EventArgs e)
        {
            if (!Context.IsDebuggingEnabled) return;   // never on the live site

            var helperId = Session["UserId"] as string;

            try
            {
                AddTestCourseToProgress(helperId, TestNoCertCourseId, TestNoCertCourseTitle);
                TestBadgeMessage.Text = "Test badge added without a certificate. It should have no download button.";
            }
            catch (Exception ex)
            {
                TestBadgeMessage.Text = "Couldn't add the test badge: " + Server.HtmlEncode(ex.Message);
            }

            BindBadges(helperId);
        }

        /// <summary>
        /// Adds a test course to helperProgress.xml with enough teaching sessions for Tier1
        /// (does nothing if it's already there). Creates the file / helper element if needed.
        /// </summary>
        private void AddTestCourseToProgress(string helperId, string courseId, string courseTitle)
        {
            lock (ProgressFileLock)
            {
                var doc = new XmlDocument();
                if (File.Exists(HelperProgressXmlPath))
                    doc.Load(HelperProgressXmlPath);
                else
                    doc.LoadXml("<?xml version='1.0' encoding='utf-8'?><helperProgress></helperProgress>");

                var helper = doc.SelectSingleNode($"/helperProgress/helper[@id={XPathSafe.Lit(helperId)}]") as XmlElement;
                if (helper == null)
                {
                    helper = doc.CreateElement("helper");
                    helper.SetAttribute("id", helperId);
                    doc.DocumentElement.AppendChild(helper);
                }

                foreach (XmlElement existing in helper.SelectNodes("course"))
                {
                    if (string.Equals(existing.GetAttribute("id"), courseId, StringComparison.OrdinalIgnoreCase))
                        return;   // already added
                }

                var course = doc.CreateElement("course");
                course.SetAttribute("id", courseId);

                var title = doc.CreateElement("title");
                title.InnerText = courseTitle;
                course.AppendChild(title);

                var sessions = doc.CreateElement("teachingSessions");
                sessions.InnerText = Tier1Threshold.ToString(CultureInfo.InvariantCulture);
                course.AppendChild(sessions);

                helper.AppendChild(course);
                doc.Save(HelperProgressXmlPath);
            }
        }

        // ===================== Showing badges =====================

        /// <summary>One badge on the page. Public so the repeater can bind to it.</summary>
        public sealed class BadgeRow
        {
            public string ImageUrl { get; set; }
            public string CourseTitle { get; set; }
            public string Description { get; set; }
            public string TierLabel { get; set; }
            public string ProgressLabel { get; set; }
            public int TierSortOrder { get; set; }

            /// <summary>True if this badge's current tier has a certificate (shows the download button).</summary>
            public bool HasCertificate { get; set; }

            /// <summary>Download link for the certificate PDF (empty if there's no certificate).</summary>
            public string CertificateUrl { get; set; }

            /// <summary>Verification number (empty if there's no certificate).</summary>
            public string CertificateCode { get; set; }

            /// <summary>Full link an employer can open to verify the certificate (empty if there's no certificate).</summary>
            public string VerifyUrl { get; set; }
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

        /// <summary>The helper's certificates keyed by certificate course id. Empty if the database can't be reached.</summary>
        private Dictionary<string, UserRepository.Certificate> LoadCertificatesByCourseId(string helperId)
        {
            var map = new Dictionary<string, UserRepository.Certificate>(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (var c in UserRepository.GetCertificatesForUser(helperId))
                    map[c.CourseId] = c;
            }
            catch (SqlException)
            {
                // Badges still show; download buttons just won't appear until the database is back.
            }
            return map;
        }

        private List<BadgeRow> LoadHelperBadgesFromProgress(string helperId)
        {
            var rows = new List<BadgeRow>();

            if (!File.Exists(HelperProgressXmlPath))
                return rows;

            var descByCourseId = LoadDescriptionsByCourseId();
            var certsByCourseId = LoadCertificatesByCourseId(helperId);

            var doc = new XmlDocument();
            doc.Load(HelperProgressXmlPath);

            var helper = doc.SelectSingleNode($"/helperProgress/helper[@id={XPathSafe.Lit(helperId)}]") as XmlElement;
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

                // Certificate for this course at the helper's current tier, if one was issued
                certsByCourseId.TryGetValue(HelperCertificateCourseId(courseId, tierSortOrder), out var cert);

                rows.Add(new BadgeRow
                {
                    ImageUrl = imageUrl,
                    CourseTitle = courseTitle,
                    Description = description,
                    TierLabel = tierLabel,
                    ProgressLabel = teachingSessions + " teaching sessions",
                    TierSortOrder = tierSortOrder,
                    HasCertificate = cert != null,
                    CertificateCode = cert?.VerificationCode ?? "",
                    VerifyUrl = cert == null ? "" : VerifyLinkFor(cert.VerificationCode),
                    CertificateUrl = cert == null
                        ? ""
                        : ResolveUrl("~/Account/CertificateDownload.aspx?code="
                                     + HttpUtility.UrlEncode(cert.VerificationCode))
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

        /// <summary>
        /// Full verification link for sharing, e.g. https://site/VerifyCertificate.aspx?code=FIA-...
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
