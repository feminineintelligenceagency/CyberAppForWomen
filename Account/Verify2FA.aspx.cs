using CyberApp_FIA.Services;
using OtpNet;
using System;
using System.Text;
using System.Web.Security;
using System.Xml;
using System.Security.Cryptography;

namespace CyberApp_FIA.Account
{
    public partial class Verify2FA : System.Web.UI.Page
    {
        protected string VerificationMessage;

        private string XmlPath => Server.MapPath("~/App_Data/users.xml");

        protected void Page_Load(object sender, EventArgs e)
        {
            // The password step must already have succeeded.
            string userId = Session["PendingMfaUserId"] as string;

            if (string.IsNullOrEmpty(userId))
            {
                Response.Redirect("~/Account/Login.aspx");
                return;
            }

            // Only verify when the form is submitted.
            if (Request.HttpMethod != "POST")
                return;

            XmlDocument doc = new XmlDocument();
            doc.Load(XmlPath);

            XmlElement user = doc.SelectSingleNode(
                $"/users/user[@id='{userId}']") as XmlElement;

            if (user == null)
            {
                VerificationMessage = "User could not be found.";
                return;
            }

            string encryptedSecret = user["mfaSecret"]?.InnerText;

            if (string.IsNullOrWhiteSpace(encryptedSecret))
            {
                VerificationMessage = "MFA secret could not be found.";
                return;
            }

            try
            {
                // Decrypt the TOTP secret stored during MFA enrollment.
                byte[] encryptedBytes =
                    Convert.FromBase64String(encryptedSecret);

                byte[] secretBytes =
                    MachineKey.Unprotect(
                        encryptedBytes,
                        "FIA",
                        "MFA-TOTP-Secret");

                if (secretBytes == null)
                {
                    VerificationMessage = "Unable to decrypt MFA secret.";
                    return;
                }

                string secret =
                    Encoding.UTF8.GetString(secretBytes);

                Totp totp =
                    new Totp(Base32Encoding.ToBytes(secret));

                string enteredCode =
                    (Request.Form["totpCode"] ?? "").Trim();

                bool validTotp = totp.VerifyTotp(
                enteredCode,
                out long timeStepMatched,
                new VerificationWindow(previous: 1, future: 1));

                bool validBackupCode = false;

                if (!validTotp)
                {
                    validBackupCode =
                        TryUseBackupCode(doc, user, enteredCode);
                }

                if (!validTotp && !validBackupCode)
                {
                    VerificationMessage =
                        "Invalid authentication or backup code. Please try again.";

                    return;
                }

                // MFA passed. The user is now fully authenticated.
                Session["UserId"] = user.GetAttribute("id");
                Session["Role"] = user.GetAttribute("role");
                Session["Email"] = user["email"]?.InnerText ?? "";
                Session["University"] =
                    user["university"]?.InnerText ?? "";

                Session.Remove("PendingMfaUserId");

                AuthSession.SignIn(Context);

                switch ((user.GetAttribute("role") ?? "")
                .Trim()
                .ToLowerInvariant())
                {
                    case "superadmin":
                        Response.Redirect(
                            "~/Account/SuperAdmin/SuperAdminHome.aspx", false);
                        Context.ApplicationInstance.CompleteRequest();
                        return;

                    case "universityadmin":
                        Response.Redirect(
                            "~/Account/UniversityAdmin/UniversityAdminHome.aspx", false);
                        Context.ApplicationInstance.CompleteRequest();
                        return;

                    case "helper":
                        Response.Redirect(
                            "~/Account/Helper/Home.aspx", false);
                        Context.ApplicationInstance.CompleteRequest();
                        return;

                    default:
                        Response.Redirect(
                            "~/Account/Participant/SelectEvent.aspx", false);
                        Context.ApplicationInstance.CompleteRequest();
                        return;
                }
            }
            catch (Exception)
            {
                VerificationMessage =
                    "Something went wrong while verifying your code. Please try again.";
            }
        }

        private bool TryUseBackupCode(
    XmlDocument doc,
    XmlElement user,
    string enteredCode)
        {
            XmlElement backupCodes =
                user["backupCodes"];

            if (backupCodes == null)
                return false;

            string normalized =
                enteredCode
                    .Replace("-", "")
                    .Replace(" ", "")
                    .ToUpperInvariant();

            byte[] enteredHash;

            using (SHA256 sha = SHA256.Create())
            {
                enteredHash = sha.ComputeHash(
                    Encoding.UTF8.GetBytes(normalized));
            }

            string enteredHashText =
                Convert.ToBase64String(enteredHash);

            foreach (XmlNode node in backupCodes.ChildNodes)
            {
                XmlElement codeElement =
                    node as XmlElement;

                if (codeElement == null)
                    continue;

                if (codeElement.InnerText == enteredHashText)
                {
                    // Remove it so it can never be used again
                    backupCodes.RemoveChild(codeElement);

                    doc.Save(XmlPath);

                    return true;
                }
            }

            return false;
        }
    }


}