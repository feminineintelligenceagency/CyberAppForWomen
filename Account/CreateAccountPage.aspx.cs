using CyberApp_FIA.Services;
using System;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Web.UI;
using System.Xml;

namespace CyberApp_FIA.Account
{
    /// <summary>
    /// Sign-up page code-behind (database version).
    /// - Validates input and consent
    /// - Only allows self-registration as Participant or Helper
    /// - Hashes the password with Argon2 (passHasher.HashNewPassword)
    /// - Creates the user and credentials in one transaction (UserRepository.CreateUser)
    /// - Writes an "Account Created" audit entry
    /// </summary>
    public partial class CreateAccountPage : Page
    {
        private string AuditXmlPath => Server.MapPath("~/App_Data/Audit_Log/UnvAdminAudit.xml");

        // Roles a visitor may choose for themselves. Admin roles must be granted by an
        // existing admin, never picked on the public sign-up form (the form value can be
        // edited in the browser, so it can't be trusted).
        private static readonly string[] SelfRegistrationRoles = { "Participant", "Helper" };

        protected void BtnSignUp_Click(object sender, EventArgs e)
        {
            // Honor ASP.NET validators (required fields, email format, consent, etc.).
            if (!Page.IsValid) return;

            // Defense in depth: re-check consent server-side.
            if (!Consent.Checked)
            {
                ShowError("Please accept the consent.");
                return;
            }

            var role = NormalizeRole(Role.Value);
            if (role == null)
            {
                ShowError("Please choose a valid account type.");
                return;
            }

            var emailLower = (Email.Text ?? "").Trim().ToLowerInvariant();
            var firstName = (FirstName.Text ?? "").Trim();
            var lastName = (LastName.Text ?? "").Trim();
            var consentIp = Request.UserHostAddress ?? "";
            var now = DateTime.UtcNow;

            try
            {
                // Quick check first so we don't spend Argon2 time on a duplicate.
                // CreateUser below still catches a duplicate that slips in between.
                if (UserRepository.EmailExists(emailLower))
                {
                    ShowError("That email is already registered.");
                    return;
                }

                var newUser = new UserRepository.NewUser
                {
                    UserId = Guid.NewGuid().ToString("N"),
                    Email = emailLower,
                    FirstName = firstName,
                    LastName = lastName,
                    Role = role,
                    University = null,              // not collected at sign-up yet
                    ConsentAcceptedAtUtc = now,
                    ConsentIp = consentIp
                };

                var password = passHasher.HashNewPassword(Password.Text);

                if (!UserRepository.CreateUser(newUser, password))
                {
                    ShowError("That email is already registered.");
                    return;
                }

                WriteAccountCreatedAuditEntry(newUser.UserId, emailLower, firstName, role, consentIp, now.ToString("o"));
            }
            catch (SqlException)
            {
                // Database unreachable or rejected the data. Don't show raw SQL errors.
                ShowError("We couldn't create your account right now. Please try again in a moment.");
                return;
            }

            // Redirect outside the try block (Response.Redirect ends the request).
            Response.Redirect("~/Account/Login.aspx");
        }

        /// <summary>CustomValidator server-side hook for the consent checkbox.</summary>
        protected void ConsentValidator_ServerValidate(object source, System.Web.UI.WebControls.ServerValidateEventArgs args)
            => args.IsValid = Consent.Checked;

        // -------------------------
        // Helpers
        // -------------------------

        /// <summary>
        /// Returns the canonical spelling of an allowed self-registration role,
        /// or null if the submitted value isn't allowed.
        /// </summary>
        private static string NormalizeRole(string value)
        {
            var v = (value ?? "").Trim();
            if (v.Length == 0) return "Participant";   // default if the form doesn't send one
            foreach (var allowed in SelfRegistrationRoles)
            {
                if (string.Equals(v, allowed, StringComparison.OrdinalIgnoreCase)) return allowed;
            }
            return null;
        }

        private void ShowError(string message)
        {
            FormMessage.Text = "<span style='color:#c21d1d'>" + Server.HtmlEncode(message) + "</span>";
        }

        /// <summary>
        /// Writes an "Account Created" audit entry (still XML for now; moves to dbo.AuditLog
        /// when the audit logger is converted). Best-effort: never blocks sign-up.
        /// </summary>
        private void WriteAccountCreatedAuditEntry(string userId, string email, string firstName,
                                                   string role, string consentIp, string consentAtUtc)
        {
            try
            {
                var dir = Path.GetDirectoryName(AuditXmlPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                if (!File.Exists(AuditXmlPath))
                {
                    File.WriteAllText(AuditXmlPath,
                        "<?xml version='1.0' encoding='utf-8'?><auditLog version='1'></auditLog>", Encoding.UTF8);
                }

                var doc = new XmlDocument();
                doc.Load(AuditXmlPath);

                var entry = doc.CreateElement("entry");
                entry.SetAttribute("id", "log-" + Guid.NewGuid().ToString("N"));
                entry.SetAttribute("university", "Arizona State University"); // adjust once university is collected
                entry.SetAttribute("role", role);
                entry.SetAttribute("type", "Account Created");
                entry.SetAttribute("timestamp", DateTime.UtcNow.ToString("o"));
                entry.SetAttribute("email", email ?? "");
                entry.SetAttribute("firstName", firstName ?? "");

                var details = doc.CreateElement("details");
                details.InnerText =
                    $"New {role} account created (userId={userId}). " +
                    $"Consent accepted at {consentAtUtc} UTC from IP {consentIp}.";
                entry.AppendChild(details);

                doc.DocumentElement.AppendChild(entry);
                doc.Save(AuditXmlPath);
            }
            catch
            {
                // Fail-safe: never block sign-up on audit logging.
            }
        }
    }
}