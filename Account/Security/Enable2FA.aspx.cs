using CyberApp_FIA.Services;
using OtpNet;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;

namespace CyberApp_FIA.Account.Security
{
    /// <summary>
    /// Turn on two-factor authentication
    /// - Generates a TOTP secret (kept in Session until confirmed) and shows it as a QR code.
    /// - The user scans it and enters a 6-digit code to prove the app is set up.
    /// - On success, saves the encrypted secret and hashed backup codes in one query to the database (UserRepository.EnableMfa) and shows the backup codes once.
    /// </summary>
    public partial class Enable2FA : System.Web.UI.Page
    {
        protected string QrImageUrl;
        protected string VerificationMessage;
        protected List<string> BackupCodes;

        protected void Page_Load(object sender, EventArgs e)
        {
            // User must already be logged in to enable MFA
            var userId = Session["UserId"] as string;
            if (string.IsNullOrWhiteSpace(userId))
            {
                Response.Redirect("~/Account/Login.aspx");
                return;
            }

            try
            {
                // Do not allow a new enrollment if MFA is already enabled
                if (UserRepository.IsMfaEnabled(userId))
                {
                    Session.Remove("PendingMfaSecret");
                    QrImageUrl = null;
                    VerificationMessage = "Two-factor authentication is already enabled.";
                    return;
                }
            }
            catch (SqlException)
            {
                VerificationMessage = "Two-factor setup is temporarily unavailable. Please try again in a moment.";
                return;
            }

            // Generate a temporary secret
            if (Session["PendingMfaSecret"] == null)
            {
                byte[] secretBytes = KeyGeneration.GenerateRandomKey(20);
                Session["PendingMfaSecret"] = Base32Encoding.ToString(secretBytes);
            }

            string secret = (string)Session["PendingMfaSecret"];
            string email = Session["Email"] as string ?? "user@fia.local";

            string issuer = "FIA";
            string label = $"{issuer}:{email}";

            string otpUri =
                $"otpauth://totp/{Uri.EscapeDataString(label)}" +
                $"?secret={secret}" +
                $"&issuer={Uri.EscapeDataString(issuer)}" +
                $"&digits=6&period=30";

            using (QRCodeGenerator generator = new QRCodeGenerator())
            using (QRCodeData data = generator.CreateQrCode(otpUri, QRCodeGenerator.ECCLevel.Q))
            using (Base64QRCode qrCode = new Base64QRCode(data))
            {
                QrImageUrl = "data:image/png;base64," + qrCode.GetGraphic(20);
            }

            if (Request.HttpMethod != "POST")
                return;

            string enteredCode = (Request.Form["totpCode"] ?? "").Replace(" ", "").Trim();

            var totp = new Totp(Base32Encoding.ToBytes(secret));
            bool valid = totp.VerifyTotp(
                enteredCode,
                out long timeStepMatched,
                new VerificationWindow(previous: 1, future: 1));

            if (!valid)
            {
                VerificationMessage = "Invalid code. Please try again.";
                return;
            }

            List<string> backupCodes = GenerateBackupCodes();

            try
            {
                UserRepository.EnableMfa(
                    userId,
                    UserRepository.ProtectMfaSecret(secret),
                    backupCodes.Select(UserRepository.HashBackupCode));

                // The code used to confirm setup can't be reused to sign in.
                UserRepository.TryAcceptTotpTimeStep(userId, timeStepMatched);
            }
            catch (SqlException)
            {
                VerificationMessage = "We couldn't turn on two-factor authentication right now. Please try again.";
                return;
            }

            Session.Remove("PendingMfaSecret");
            QrImageUrl = null;   // hide the QR code once enrollment is complete

            try { UniversityAuditLogger.AppendForCurrentUser(this, "MFA Enabled", "Two-factor authentication was enabled."); } catch { }

            VerificationMessage = "Two-factor authentication is now enabled.";
            BackupCodes = backupCodes;   // shown once; only hashes are stored
        }

        /// <summary>
        /// 8 random backup codes in the form XXXX-XXXX-XXXX-XXXX.
        /// </summary>
        private static List<string> GenerateBackupCodes()
        {
            var codes = new List<string>();

            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                for (int i = 0; i < 8; i++)
                {
                    byte[] bytes = new byte[8];
                    rng.GetBytes(bytes);

                    string hex = BitConverter.ToString(bytes).Replace("-", "");
                    codes.Add(hex.Substring(0, 4) + "-" + hex.Substring(4, 4) + "-" +
                              hex.Substring(8, 4) + "-" + hex.Substring(12, 4));
                }
            }

            return codes;
        }
    }
}