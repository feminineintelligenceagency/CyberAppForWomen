using System;
using System.Data.SqlClient;
using System.Globalization;
using System.Web;
using System.Web.Caching;
using System.Web.UI;
using CyberApp_FIA.Services;

namespace CyberApp_FIA
{
    /// <summary>
    /// Public certificate verification page for employers.
    ///   ~/VerifyCertificate.aspx                          -> type the number in
    ///   ~/VerifyCertificate.aspx?code=FIA-XXXX-XXXX-XXXX  -> checks it straight away (for links)
    ///
    /// only shows what's printed on the certificate itself (name, course, date, verification number). not the email, account or anything else.
    ///
    /// Abuse protection: codes are random (~10^17 possibilities), and each visitor is limited to MaxLookups checks per LookupWindow.
    /// </summary>
    public partial class VerifyCertificate : Page
    {
        private const int MaxLookups = 20;
        private static readonly TimeSpan LookupWindow = TimeSpan.FromMinutes(10);

        protected void Page_Load(object sender, EventArgs e)
        {
            // Don't cache results
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();

            if (!IsPostBack)
            {
                var code = Request.QueryString["code"];
                if (!string.IsNullOrWhiteSpace(code))
                {
                    CodeInput.Text = code.Trim();
                    Verify(code);
                }
            }
        }

        protected void BtnVerify_Click(object sender, EventArgs e)
        {
            Verify(CodeInput.Text);
        }

        private void Verify(string input)
        {
            ValidPH.Visible = false;
            InvalidPH.Visible = false;

            //input null
            if (string.IsNullOrWhiteSpace(input))
            {
                ShowInvalid("Enter a verification number",
                    "Type the number printed on the certificate, for example FIA-7KQ2-M9XD-P4TR.");
                return;
            }

            //too many attempts from this IP
            if (!AllowLookup())
            {
                ShowInvalid("Too many attempts",
                    "Please wait a few minutes and try again.");
                return;
            }

            // Wrong format can't be a real certificate: same answer as "not found".
            var code = UserRepository.NormalizeCertificateCode(input);
            if (code == null)
            {
                ShowNotFound();
                return;
            }

            UserRepository.Certificate cert;
            try
            {
                cert = UserRepository.GetCertificateByCode(code);
            }
            catch (SqlException)
            {
                ShowInvalid("Verification is temporarily unavailable",
                    "We couldn't check this certificate right now. Please try again in a few minutes.");
                return;
            }

            if (cert == null)
            {
                ShowNotFound();
                return;
            }

            CodeInput.Text = cert.VerificationCode;
            NameLit.Text = cert.RecipientName;
            CourseLit.Text = cert.CourseTitle;
            DateLit.Text = cert.IssuedAtUtc.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture);
            CodeLit.Text = cert.VerificationCode;
            ValidPH.Visible = true;
        }

        private void ShowNotFound()
        {
            ShowInvalid("No certificate found",
                "We couldn't find a certificate with that verification number. " +
                "Check that it was typed exactly as printed. If it still isn't found, the certificate may not be genuine.");
        }

        private void ShowInvalid(string title, string text)
        {
            InvalidTitleLit.Text = title;
            InvalidTextLit.Text = text;
            InvalidPH.Visible = true;
        }

        /// <summary>
        /// IP rate limit: at most MaxLookups checks per LookupWindow.
        /// </summary>
        private bool AllowLookup()
        {
            var ip = Request.UserHostAddress ?? "unknown";
            var key = "verify-cert:" + ip;

            lock (LookupLock)
            {
                var counter = HttpRuntime.Cache[key] as LookupCounter;
                if (counter == null)
                {
                    counter = new LookupCounter();
                    HttpRuntime.Cache.Insert(key, counter, null,
                        DateTime.UtcNow.Add(LookupWindow), Cache.NoSlidingExpiration);
                }

                counter.Count++;
                return counter.Count <= MaxLookups;
            }
        }

        private static readonly object LookupLock = new object();

        private sealed class LookupCounter
        {
            public int Count;
        }
    }
}
