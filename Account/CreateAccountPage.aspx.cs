using CyberApp_FIA.Services;
using System;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Web.UI;

namespace CyberApp_FIA.Account
{
    /// <summary>
    /// Sign-up page code
    /// - Validates input and consent
    /// - Only allows self-registration as Participant or Helper
    /// - Hashes the password with Argon2 (passHasher.HashNewPassword)
    /// - Creates the user and credentials in one transaction (UserRepository.CreateUser)
    /// - Writes an "Account Created" audit entry (dbo.AuditLog)
    /// </summary>
    public partial class CreateAccountPage : Page
    {
        // Roles a visitor may choose for themselves. Admin roles must be granted by an existing admin
        private static readonly string[] SelfRegistrationRoles = { "Participant", "Helper" };

        protected void BtnSignUp_Click(object sender, EventArgs e)
        {
            // Honor ASP.NET validators (required fields, email format, consent, etc.).
            if (!Page.IsValid) return;

            //re-check consent server-side.
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
                    University = null,
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
                // Database unreachable or rejected the data
                ShowError("We couldn't create your account right now. Please try again in a moment.");
                return;
            }

            // Redirect outside the try block
            Response.Redirect("~/Account/Login.aspx");
        }

        /// <summary>CustomValidator server-side hook for the consent checkbox.</summary>
        protected void ConsentValidator_ServerValidate(object source, System.Web.UI.WebControls.ServerValidateEventArgs args)
            => args.IsValid = Consent.Checked;

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
        /// Adds an "Account Created" entry to the audit log (dbo.AuditLog). Never throws,
        /// so it can't block sign-up. University is left empty until sign-up collects it.
        /// </summary>
        private static void WriteAccountCreatedAuditEntry(string userId, string email, string firstName,
                                                          string role, string consentIp, string consentAtUtc)
        {
            UserRepository.AppendAudit(
                "Account Created",
                $"New {role} account created (userId={userId}). " +
                $"Consent accepted at {consentAtUtc} UTC from IP {consentIp}.",
                userId, email, firstName, role, university: null);
        }
    }
}