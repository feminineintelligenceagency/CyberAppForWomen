using CyberApp_FIA.Services;
using System;
using System.Data.SqlClient;
using System.IO;
using System.Web.UI;
using System.Xml;

namespace CyberApp_FIA.Account
{
    /// <summary>
    /// Login page code-behind
    /// - Looks the user up in dbo.Users / dbo.UserCredentials via UserRepository
    /// - Verifies the password with passHasher (Argon2, or legacy PBKDF2 for old accounts)
    /// - Upgrades legacy/outdated hashes to current Argon2 settings on successful login
    /// - Rate limiting: MaxFailedAttempts / LockoutDuration from LoginRateLimiting, enforced in the database
    /// - Audit logging of failed and successful sign-ins
    /// - Role-based redirect
    /// </summary>
    public partial class Login : Page
    {
        private const string GenericLoginError =
            "<span style='color:#c21d1d'>Invalid email or password.</span>";

        private const string ServiceUnavailableError =
            "<span style='color:#c21d1d'>Sign-in is temporarily unavailable. Please try again in a moment.</span>";

        private static readonly string LockoutError =
            "<span style='color:#c21d1d'>Too many failed attempts. Your account is locked for "
            + (int)LoginRateLimiting.LockoutDuration.TotalMinutes + " minutes.</span>";

        private string AuditXmlPath => Server.MapPath("~/App_Data/Audit_Log/UnvAdminAudit.xml");

        protected void BtnLogin_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) { FormMessage.Text = GenericLoginError; return; }

            var emailLower = (Email.Text ?? "").Trim().ToLowerInvariant();
            var password = Password.Text ?? "";
            var now = DateTime.UtcNow;

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

            // Unknown email 
            if (user == null)
            {
                passHasher.dummyVerify(password);

                WriteFailedSignInAudit(emailLower, "Unknown", "", "",
                    "Sign In Failed (Unknown Email)",
                    "Sign-in attempt with an email address that is not registered.");

                FormMessage.Text = GenericLoginError;
                return;
            }

            //refuse before checking the password if user is locked out
            if (user.IsLockedOut(now))
            {
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

                WriteFailedSignInAudit(emailLower, user.Role, user.University ?? "", user.FirstName ?? "",
                    lockedUntil.HasValue ? "Account Locked (Too Many Failed Sign Ins)" : "Sign In Failed (Bad Password)",
                    lockedUntil.HasValue
                        ? "Account locked until " + lockedUntil.Value.ToString("o") + " UTC after repeated failed sign-ins."
                        : "Incorrect password for existing account during sign in.");

                FormMessage.Text = lockedUntil.HasValue ? LockoutError : GenericLoginError;
                return;
            }

            // Success: clear failures, upgrade the hash if needed ---
            try
            {
                UserRepository.ResetFailedLogins(user.UserId);

                if (needsRehash)
                {
                    UserRepository.UpdatePasswordHash(user.UserId, passHasher.HashNewPassword(password));
                }
            }
            catch (SqlException)
            {
                // Best-effort: the user is authenticated. The reset/upgrade retries next login.
            }

            //Session
            Session["UserId"] = user.UserId;
            Session["Role"] = user.Role;
            Session["Email"] = user.Email;
            Session["University"] = user.University ?? "";

            // Issue the auth token that Global.asax checks on every request.
            AuthSession.SignIn(Context);

            try { UniversityAuditLogger.AppendForCurrentUser(this, "Sign In", user.Role + " signed in."); } catch { }

            Response.Redirect(GetLandingPage(user.Role));
        }

        /// <summary>
        /// Role-based landing page. Unknown roles fall back to the Participant flow.
        /// </summary>
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
        /// Appends a failed sign-in entry to the University Admin audit log (still XML for now;
        /// moves to dbo.AuditLog when we convert the audit logger). Best-effort only.
        /// </summary>
        private void WriteFailedSignInAudit(string email, string role, string university,
                                            string firstName, string type, string details)
        {
            try
            {
                var auditDir = Path.GetDirectoryName(AuditXmlPath);
                if (!string.IsNullOrEmpty(auditDir) && !Directory.Exists(auditDir))
                {
                    Directory.CreateDirectory(auditDir);
                }

                var auditDoc = new XmlDocument();
                if (File.Exists(AuditXmlPath))
                {
                    auditDoc.Load(AuditXmlPath);
                }
                else
                {
                    auditDoc.LoadXml("<?xml version='1.0' encoding='utf-8'?><auditLog version='1'></auditLog>");
                }

                var entry = auditDoc.CreateElement("entry");
                entry.SetAttribute("id", "log-" + Guid.NewGuid().ToString("N"));
                entry.SetAttribute("university", university ?? "");
                entry.SetAttribute("role", string.IsNullOrWhiteSpace(role) ? "Unknown" : role);
                entry.SetAttribute("type", type);
                entry.SetAttribute("timestamp", DateTime.UtcNow.ToString("o"));
                entry.SetAttribute("email", email ?? "");
                entry.SetAttribute("firstName", firstName ?? "");

                var detailsEl = auditDoc.CreateElement("details");
                detailsEl.InnerText = details;
                entry.AppendChild(detailsEl);

                auditDoc.DocumentElement.AppendChild(entry);
                auditDoc.Save(AuditXmlPath);
            }
            catch
            {
                // Best-effort only.
            }
        }
    }
}