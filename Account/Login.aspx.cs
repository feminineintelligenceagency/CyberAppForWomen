using CyberApp_FIA.Services;
using System;
using System.Data.SqlClient;
using System.IO;
using System.Web.UI;

namespace CyberApp_FIA.Account
{
    /// <summary>
    /// Login page code-behind 
    /// - Looks the user up in dbo.Users / dbo.UserCredentials via UserRepository
    /// - Verifies the password with passHasher (Argon2, or legacy PBKDF2 for old accounts)
    /// - Upgrades legacy/outdated hashes to current Argon2 settings on successful login
    /// - Rate limiting: MaxFailedAttempts / LockoutDuration from LoginRateLimiting, enforced in the database
    /// - MFA: accounts with two-factor enabled go to Verify2FA.aspx before getting a session
    /// - Session timeout message (?expired=1) and password stamp 
    /// - Audit logging of failed and successful sign-ins (dbo.AuditLog)
    /// - Role-based redirect
    /// </summary>
    public partial class Login : Page
    {
        // One message for every failed sign-in (unknown email, wrong password, locked account, too many
        // attempts from this address). Different messages would let someone tell which emails have accounts.
        private static readonly string GenericLoginError =
            "<span style='color:#c21d1d'>Invalid email or password. If you have tried several times, "
            + "your account may be temporarily locked: wait "
            + (int)LoginRateLimiting.LockoutDuration.TotalMinutes
            + " minutes or reset your password.</span>";

        private const string ServiceUnavailableError =
            "<span style='color:#c21d1d'>Sign-in is temporarily unavailable. Please try again in a moment.</span>";

        // Same text as every other failure on purpose (see GenericLoginError).
        private static string LockoutError { get { return GenericLoginError; } }

        /// <summary>
        /// when SecurePage sends the user here with ?expired=1, end the old session on the server and show a message.
        /// </summary>
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack && Request.QueryString["expired"] == "1")
            {
                AuthSession.SignOut(Context);
                FormMessage.Text = "<span style='color:#c21d1d'>Your session has ended. Please log in again.</span>";
            }
        }

        protected void BtnLogin_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) { FormMessage.Text = GenericLoginError; return; }

            var emailLower = (Email.Text ?? "").Trim().ToLowerInvariant();
            var password = Password.Text ?? "";
            var now = DateTime.UtcNow;
            var clientIp = Request.UserHostAddress;

            // Too many failed sign-ins from this address recently: refuse before doing any work.
            if (LoginIpThrottle.IsBlocked(clientIp, now))
            {
                FormMessage.Text = GenericLoginError;
                return;
            }

            //Look up the user
            UserRepository.LoginRecord user;
            try
            {
                user = UserRepository.GetLoginByEmail(emailLower);
            }
            catch (SqlException)
            {
                // Database unreachable
                FormMessage.Text = ServiceUnavailableError;
                return;
            }

            //Unknown email
            if (user == null)
            {
                // Take about as long as a real check so timing doesn't reveal registered emails.
                passHasher.dummyVerify(password);
                LoginIpThrottle.RecordFailure(clientIp, now);

                WriteFailedSignInAudit(null, emailLower, "Unknown", "", "",
                    "Sign In Failed (Unknown Email)",
                    "Sign-in attempt with an email address that is not registered.");

                FormMessage.Text = GenericLoginError;
                return;
            }

            //refuse before checking the password if user is locked out
            if (user.IsLockedOut(now))
            {
                // Take as long as a real check, so a locked account can't be told apart by speed.
                passHasher.dummyVerify(password);
                LoginIpThrottle.RecordFailure(clientIp, now);
                FormMessage.Text = LockoutError;
                return;
            }

            //Password check
            bool needsRehash;
            bool valid = passHasher.VerifyStored(
                password,
                user.Algorithm,
                user.PasswordHash,
                user.PasswordSalt,
                user.Argon2MemoryKiB,
                user.Argon2Iterations,
                user.Argon2Parallelism,
                out needsRehash);

            if (!valid)
            {
                LoginIpThrottle.RecordFailure(clientIp, now);

                //count the failure; lock after repeated invalid attempts.
                DateTime? lockedUntil = null;
                try
                {
                    lockedUntil = UserRepository.RecordFailedLogin(
                        user.UserId,
                        LoginRateLimiting.MaxFailedAttempts,
                        LoginRateLimiting.LockoutDuration);
                }
                catch (SqlException)
                {
                    // Still refuse the login
                }

                WriteFailedSignInAudit(user.UserId, emailLower, user.Role, user.University ?? "", user.FirstName ?? "",
                    lockedUntil.HasValue ? "Account Locked (Too Many Failed Sign Ins)" : "Sign In Failed (Bad Password)",
                    lockedUntil.HasValue
                        ? "Account locked until " + lockedUntil.Value.ToString("o") + " UTC after repeated failed sign-ins."
                        : "Incorrect password for existing account during sign in.");

                FormMessage.Text = lockedUntil.HasValue ? LockoutError : GenericLoginError;
                return;
            }

            //Password correct: upgrade the hash if needed, clear failures
            try
            {
                // With MFA on, failures are only cleared after the code is verified in
                // Verify2FA. Otherwise re-entering the password would reset the counter
                // and let someone keep guessing 6-digit codes.
                if (!user.MfaEnabled)
                {
                    UserRepository.ResetFailedLogins(user.UserId);
                }

                if (needsRehash)
                {
                    //rehashes old passwords to new standard (Argon2) and stores in the database.
                    UserRepository.UpdatePasswordHash(user.UserId, passHasher.HashNewPassword(password),
                                                      passwordChanged: false);
                }
            }
            catch (SqlException)
            {
                // Best-effort: the reset/upgrade retries next login.
            }

            // 2FA check 
            if (user.MfaEnabled)
            {
                Session["PendingMfaUserId"] = user.UserId;
                Session["PendingMfaAtUtc"] = DateTime.UtcNow;   // Verify2FA expires this after X minutes
                Response.Redirect("~/Account/Verify2FA.aspx");
                return;
            }

            // Accounts without MFA: create the session now
            Session["UserId"] = user.UserId;
            Session["Role"] = user.Role;
            Session["Email"] = user.Email;
            Session["University"] = user.University ?? "";
            Session["PasswordStamp"] = user.PasswordStamp;

            // Issue the auth token that Global.asax checks on every request.
            AuthSession.SignIn(Context);

            try { UniversityAuditLogger.AppendForCurrentUser(this, "Sign In", user.Role + " signed in."); }
            catch (Exception ex) { System.Diagnostics.Trace.TraceWarning("Sign-in audit entry failed: " + ex.Message); }

            Response.Redirect(GetLandingPage(user.Role));
        }

        /// <summary>Role-based landing page. Unknown roles fall back to the Participant flow.</summary>
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

        /// <summary>
        /// Adds a failed sign-in entry to the audit log (dbo.AuditLog).
        /// userId is null when the email isn't registered. Never throws.
        /// </summary>
        private static void WriteFailedSignInAudit(string userId, string email, string role, string university,
                                                   string firstName, string type, string details)
        {
            UserRepository.AppendAudit(type, details, userId, email, firstName,
                                   string.IsNullOrWhiteSpace(role) ? "Unknown" : role, university);
        }
    }
}