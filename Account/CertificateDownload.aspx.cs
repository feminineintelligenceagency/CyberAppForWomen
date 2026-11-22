using System;
using System.Data.SqlClient;
using CyberApp_FIA.Services;

namespace CyberApp_FIA.Account
{
    /// <summary>
    /// Downloads one of the signed-in user's certificates as a PDF (participants and helpers).
    ///   ~/Account/CertificateDownload.aspx?code=FIA-XXXX-XXXX-XXXX
    /// </summary>
    public partial class CertificateDownload : SecurePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var userId = Session["UserId"] as string;
            var code = Request.QueryString["code"];

            UserRepository.Certificate cert;
            try
            {
                cert = UserRepository.GetCertificateByCode(code);
            }
            catch (SqlException)
            {
                SendError(503, "Certificates are temporarily unavailable. Please try again in a moment.");
                return;
            }

            if (cert == null || !string.Equals(cert.UserId, userId, StringComparison.OrdinalIgnoreCase))
            {
                SendError(404, "Certificate not found.");
                return;
            }

            var pdf = UserRepository.CreateCertificatePdf(cert);

            Response.Clear();
            Response.ContentType = "application/pdf";
            Response.AddHeader("Content-Disposition",
                "attachment; filename=\"" + UserRepository.CertificatePdfFileName(cert) + "\"");
            Response.AddHeader("Content-Length", pdf.Length.ToString());
            Response.BinaryWrite(pdf);
            Response.Flush();
            Context.ApplicationInstance.CompleteRequest();   // end cleanly 
        }

        private void SendError(int status, string message)
        {
            Response.Clear();
            Response.StatusCode = status;
            Response.ContentType = "text/plain";
            Response.Write(message);
            Context.ApplicationInstance.CompleteRequest();
        }
    }
}
