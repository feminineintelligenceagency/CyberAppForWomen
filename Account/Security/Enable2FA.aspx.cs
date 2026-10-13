using System;
using System.Text;
using System.Web.Security;
using System.Xml;
using OtpNet;
using QRCoder;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace CyberApp_FIA.Account.Security
{
    public partial class Enable2FA : System.Web.UI.Page
    {
        protected string QrImageUrl;
        protected string VerificationMessage;
        protected List<string> BackupCodes;

        private string XmlPath => Server.MapPath("~/App_Data/users.xml");

        protected void Page_Load(object sender, EventArgs e)
        {
            // User must already be logged in to enable MFA
            if (Session["UserId"] == null)
            {
                Response.Redirect("~/Account/Login.aspx");
                return;
            }
            // Do not allow a new MFA enrollment if MFA is already enabled
            if (IsMfaAlreadyEnabled())
            {
                Session.Remove("PendingMfaSecret");
                QrImageUrl = null;

                VerificationMessage =
                    "Two-factor authentication is already enabled.";

                return;
            }

            // Generate temporary secret
            if (Session["PendingMfaSecret"] == null)
            {
                byte[] secretBytes = KeyGeneration.GenerateRandomKey(20);
                Session["PendingMfaSecret"] =
                    Base32Encoding.ToString(secretBytes);
            }

            string secret = (string)Session["PendingMfaSecret"];
            string email = (string)Session["Email"] ?? "user@fia.local";

            string issuer = "FIA";
            string label = $"{issuer}:{email}";

            string otpUri =
                $"otpauth://totp/{Uri.EscapeDataString(label)}" +
                $"?secret={secret}" +
                $"&issuer={Uri.EscapeDataString(issuer)}" +
                $"&digits=6&period=30";

            using (QRCodeGenerator generator = new QRCodeGenerator())
            using (QRCodeData data = generator.CreateQrCode(
                otpUri,
                QRCodeGenerator.ECCLevel.Q))
            using (Base64QRCode qrCode = new Base64QRCode(data))
            {
                string base64 = qrCode.GetGraphic(20);
                QrImageUrl = "data:image/png;base64," + base64;
            }

            if (Request.HttpMethod != "POST")
                return;

            string enteredCode = Request.Form["totpCode"] ?? "";

            Totp totp =
                new Totp(Base32Encoding.ToBytes(secret));

            bool valid = totp.VerifyTotp(
                enteredCode,
                out long timeStepMatched,
                new VerificationWindow(previous: 1, future: 1));

            if (!valid)
            {
                VerificationMessage = "Invalid code. Please try again.";
                return;
            }

            SaveMfaSecret(secret);

            List<string> backupCodes = GenerateBackupCodes();
            SaveBackupCodes(backupCodes);

            Session.Remove("PendingMfaSecret");

            VerificationMessage =
                "Two-factor authentication is now enabled.";

            BackupCodes = backupCodes;
        }

        private List<string> GenerateBackupCodes()
        {
            List<string> codes = new List<string>();

            using (RandomNumberGenerator rng =
                RandomNumberGenerator.Create())
            {
                for (int i = 0; i < 8; i++)
                {
                    byte[] bytes = new byte[8];
                    rng.GetBytes(bytes);

                    string hex =
                        BitConverter.ToString(bytes)
                            .Replace("-", "");

                    string code =
                        hex.Substring(0, 4) + "-" +
                        hex.Substring(4, 4) + "-" +
                        hex.Substring(8, 4) + "-" +
                        hex.Substring(12, 4);

                    codes.Add(code);
                }
            }

            return codes;
        }

        private void SaveBackupCodes(List<string> backupCodes)
        {
            string userId = (string)Session["UserId"];

            XmlDocument doc = new XmlDocument();
            doc.Load(XmlPath);

            XmlElement user = doc.SelectSingleNode(
                $"/users/user[@id='{userId}']") as XmlElement;

            if (user == null)
                throw new Exception("User account could not be found.");

            XmlElement existing = user["backupCodes"];

            if (existing != null)
                user.RemoveChild(existing);

            XmlElement backupCodesElement =
                doc.CreateElement("backupCodes");

            foreach (string code in backupCodes)
            {
                string normalized =
                    code.Replace("-", "").ToUpperInvariant();

                byte[] hashBytes;

                using (SHA256 sha = SHA256.Create())
                {
                    hashBytes =
                        sha.ComputeHash(
                            Encoding.UTF8.GetBytes(normalized));
                }

                XmlElement codeElement =
                    doc.CreateElement("code");

                codeElement.InnerText =
                    Convert.ToBase64String(hashBytes);

                backupCodesElement.AppendChild(codeElement);
            }

            user.AppendChild(backupCodesElement);

            doc.Save(XmlPath);
        }

        private XmlElement FindUserById(XmlDocument doc, string userId)
        {
            XmlNodeList users = doc.SelectNodes("/users/user");

            foreach (XmlNode node in users)
            {
                XmlElement user = node as XmlElement;

                if (user != null &&
                    string.Equals(
                        user.GetAttribute("id"),
                        userId,
                        StringComparison.Ordinal))
                {
                    return user;
                }
            }

            return null;
        }

        private bool IsMfaAlreadyEnabled()
        {
            string userId = (string)Session["UserId"];

            XmlDocument doc = new XmlDocument();
            doc.Load(XmlPath);

            XmlElement user = doc.SelectSingleNode(
                $"/users/user[@id='{userId}']") as XmlElement;

            if (user == null)
                return false;

            return string.Equals(
                user["mfaEnabled"]?.InnerText,
                "true",
                StringComparison.OrdinalIgnoreCase);
        }

        private void SaveMfaSecret(string secret)
        {
            string userId = (string)Session["UserId"];

            XmlDocument doc = new XmlDocument();
            doc.Load(XmlPath);

            XmlElement user = doc.SelectSingleNode(
                $"/users/user[@id='{userId}']") as XmlElement;

            if (user == null)
                throw new Exception("User account could not be found.");

            // Encrypt secret before storing it
            byte[] protectedBytes = MachineKey.Protect(
                Encoding.UTF8.GetBytes(secret),
                "FIA",
                "MFA-TOTP-Secret");

            string encryptedSecret =
                Convert.ToBase64String(protectedBytes);

            SetValue(doc, user, "mfaEnabled", "true");
            SetValue(doc, user, "mfaSecret", encryptedSecret);

            doc.Save(XmlPath);
        }

        private void SetValue(
            XmlDocument doc,
            XmlElement user,
            string name,
            string value)
        {
            XmlElement element = user[name];

            if (element == null)
            {
                element = doc.CreateElement(name);
                user.AppendChild(element);
            }

            element.InnerText = value;
        }
    }
}