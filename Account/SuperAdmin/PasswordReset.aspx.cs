using System;
using System.Data.SqlClient;
using CyberApp_FIA.Services;

namespace CyberApp_FIA.Account.SuperAdmin
{
    /// <summary>
    /// Lets a SuperAdmin set a new password for any account.
    /// Works on the database (dbo.UserCredentials), where accounts live now. It used to edit the old
    /// App_Data/users.xml file with a legacy hash, which the login page no longer reads.
    /// - the new password follows the same rules as everywhere else (PasswordPolicy)
    /// - the password is hashed with Argon2 (passHasher)
    /// - the account's lockout is cleared and the password stamp changes, so every
    ///   session that account already has is signed out (SecurePage checks the stamp)
    /// - the reset is written to the audit log (never the password)
    /// </summary>
    public partial class PasswordReset : SecurePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // SecurePage already requires the SuperAdmin role for this folder on every request;
            // this is the second check, kept so the page is safe even if moved.
            RequireSuperAdmin();
            if (!IsPostBack) WelcomeName.Text = Server.HtmlEncode((string)Session["Email"] ?? "Super Admin");
        }

        protected void BtnResetPassword_Click(object sender, EventArgs e)
        {
            RequireSuperAdmin();

            Page.Validate("ResetPassword");
            if (!Page.IsValid) return;

            var emailAddress = (EmailAddress.Text ?? "").Trim().ToLowerInvariant();
            var newPassword = NewPassword.Text ?? "";

            if (emailAddress.Length == 0) { ShowError("Please enter the user's email address."); return; }

            var passwordProblem = PasswordPolicy.Problem(newPassword, emailAddress);
            if (passwordProblem != null) { ShowError(passwordProblem); return; }

            try
            {
                var account = UserRepository.GetLoginByEmail(emailAddress);
                if (account == null) { ShowError("No account was found with that email address."); return; }

                // passwordChanged: true updates the password stamp, which signs the account out everywhere.
                UserRepository.UpdatePasswordHash(account.UserId, passHasher.HashNewPassword(newPassword), passwordChanged: true);
                UserRepository.ResetFailedLogins(account.UserId);

                var actorEmail = Session["Email"] as string ?? "";
                UserRepository.AppendAudit(
                    "Password Reset (Super Admin)",
                    "Super admin " + actorEmail + " set a new password for " + account.Email + ". Existing sessions were signed out.",
                    Session["UserId"] as string, actorEmail, null, "SuperAdmin", account.University);
            }
            catch (SqlException)
            {
                ShowError("The password could not be saved right now. Please try again in a moment.");
                return;
            }

            EmailAddress.Text = ""; NewPassword.Text = ""; ConfirmPassword.Text = "";
            ShowSuccess("Password reset saved successfully. The user can now sign in with the new password.");
        }

        protected void BtnClear_Click(object sender, EventArgs e)
        {
            RequireSuperAdmin();
            EmailAddress.Text = ""; NewPassword.Text = ""; ConfirmPassword.Text = ""; ResetMessage.Text = "";
        }

        private void RequireSuperAdmin()
        {
            if (!string.Equals(Session["Role"] as string, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
                Response.Redirect("~/Account/Login.aspx");
        }

        private void ShowSuccess(string message) { ResetMessage.Text = "<span style='color:#0a7a3c'>" + Server.HtmlEncode(message) + "</span>"; }
        private void ShowError(string message) { ResetMessage.Text = "<span style='color:#c21d1d'>" + Server.HtmlEncode(message) + "</span>"; }
    }
}
