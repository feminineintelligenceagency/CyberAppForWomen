using CyberApp_FIA.Services;
using OtpNet;
using System;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Security;
using System.Xml;

namespace CyberApp_FIA.Account
{
    public partial class Verify2FA : System.Web.UI.Page
    {
        protected string VerificationMessage;

        //private string XmlPath => Server.MapPath("~/App_Data/users.xml");
        private static readonly TimeSpan PendingMfaLifetime = TimeSpan.FromMinutes(5);

        private static readonly string LockoutMessage =
            "Too many failed attempts. Your account is locked for "
            + (int)LoginRateLimiting.LockoutDuration.TotalMinutes + " minutes.";


        protected void Page_Load(object sender, EventArgs e)
        {
            // The password step must already have succeeded.
            string userId = Session["PendingMfaUserId"] as string;

            if (string.IsNullOrEmpty(userId))
            {
                Response.Redirect("~/Account/Login.aspx");
                return;
            }

            var started = Session["PendingMfaAtUtc"] as DateTime?;
            if (!started.HasValue || DateTime.UtcNow - started.Value > PendingMfaLifetime)
            {
                ClearPending();
                Response.Redirect("~/Account/Login.aspx?expired=1");
                return;
            }


            // Only verify when the form is submitted.
            if (Request.HttpMethod != "POST")
                return;

            var now = DateTime.UtcNow;

            try
            {
                var user = UserRepository.GetLoginById(userId);
                if (user == null || !user.MfaEnabled)
                {
                    // Account deleted, or MFA turned off since the password step: start over.
                    ClearPending();
                    Response.Redirect("~/Account/Login.aspx");
                    return;
                }

                if (user.IsLockedOut(now))
                {
                    ClearPending();
                    VerificationMessage = LockoutMessage;
                    return;
                }

                string entered = (Request.Form["totpCode"] ?? "").Trim();

                bool accepted = TryAuthenticatorCode(user, entered)
                             || UserRepository.TryUseBackupCode(user.UserId, entered);

                if (!accepted)
                {
                    // Wrong codes count toward the same lockout as wrong passwords, so a
                    // 6-digit code can't be brute-forced.
                    var lockedUntil = UserRepository.RecordFailedLogin(
                        user.UserId, LoginRateLimiting.MaxFailedAttempts, LoginRateLimiting.LockoutDuration);

                    if (lockedUntil.HasValue)
                    {
                        ClearPending();
                        VerificationMessage = LockoutMessage;
                    }
                    else
                    {
                        VerificationMessage = "Invalid authentication or backup code. Please try again.";
                    }
                    return;
                }

                // --- MFA passed: the user is now fully authenticated ---
                UserRepository.ResetFailedLogins(user.UserId);

                ClearPending();
                Session["UserId"] = user.UserId;
                Session["Role"] = user.Role;
                Session["Email"] = user.Email;
                Session["University"] = user.University ?? "";
                Session["PasswordStamp"] = user.PasswordStamp;   // SecurePage checks this on every page

                AuthSession.SignIn(Context);

                try { UniversityAuditLogger.AppendForCurrentUser(this, "Sign In", user.Role + " signed in (two-factor)."); } catch { }

                Response.Redirect(GetLandingPage(user.Role), false);
                Context.ApplicationInstance.CompleteRequest();
            }
            catch (SqlException)
            {
                VerificationMessage = "Verification is temporarily unavailable. Please try again in a moment.";
            }
        }
        private static bool TryAuthenticatorCode(UserRepository.LoginRecord user, string entered)
        {
            var digits = new string((entered ?? "").Where(char.IsDigit).ToArray());
            if (digits.Length != 6) return false;

            var secret = UserRepository.UnprotectMfaSecret(user.MfaSecretProtected);
            if (secret == null) return false;   // can't decrypt here; a backup code still works

            var totp = new Totp(Base32Encoding.ToBytes(secret));
            if (!totp.VerifyTotp(digits, out long timeStep, new VerificationWindow(previous: 1, future: 1)))
                return false;

            // Same code (or an older one) already used? Reject it.
            return UserRepository.TryAcceptTotpTimeStep(user.UserId, timeStep);
        }

        private void ClearPending()
        {
            Session.Remove("PendingMfaUserId");
            Session.Remove("PendingMfaAtUtc");
        }

        private static string GetLandingPage(string role)
        {
            switch ((role ?? "").Trim().ToLowerInvariant())
            {
                case "superadmin": return "~/Account/SuperAdmin/SuperAdminHome.aspx";
                case "universityadmin": return "~/Account/UniversityAdmin/UniversityAdminHome.aspx";
                case "helper": return "~/Account/Helper/Home.aspx";
                default: return "~/Account/Participant/SelectEvent.aspx";
            }
        }
    }
}