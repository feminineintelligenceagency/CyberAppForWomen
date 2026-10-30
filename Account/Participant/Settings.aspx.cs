using System;
using System.IO;
using System.Data.SqlClient;
using CyberApp_FIA.Services;

namespace CyberApp_FIA.Participant
{

    /// Sprint 2 (April Lee story): participant account settings
    /// Lets a participant permanently delete their account after confirming
    /// with their password and a checkbox.
    /// SecurePage already sends logged-out visitors to the login page.
    public partial class Settings : SecurePage
    {
        // Wrong-password attempts allowed on this page before we sign the person out,
        // so someone at an unlocked computer can't keep guessing.
        private const int MaxPasswordAttempts = 5;
        private const string AttemptsKey = "DeleteAccountAttempts";

        protected void Page_Load(object sender, EventArgs e)
        {
            // Only participants can use this page.
            var role = Session["Role"] as string;
            if (!string.Equals(role, "Participant", StringComparison.OrdinalIgnoreCase))
            {
                Response.Redirect("~/Account/Login.aspx");
                return;
            }

            if (!IsPostBack)
            {
                EmailLiteral.Text = Server.HtmlEncode(Session["Email"] as string ?? "");
            }
        }

        protected void BtnDelete_Click(object sender, EventArgs e)
        {
            FormMessage.Text = "";
            var userId = Session["UserId"] as string;

            //Confirmation step: checkbox must be ticked
            if (!ConfirmCheck.Checked)
            {
                ShowError("Please tick the box to confirm you understand this can't be undone.");
                return;
            }

            //Confirmation step: password must be entered
            var password = ConfirmPassword.Text ?? "";
            if (string.IsNullOrEmpty(password))
            {
                ShowError("Please enter your password.");
                return;
            }

            //Find this user's account in the database
            UserRepository.LoginRecord user;
            try
            {
                user = string.IsNullOrWhiteSpace(userId) ? null : UserRepository.GetLoginById(userId);
            }
            catch (SqlException)
            {
                ShowError("We couldn't reach your account right now. Please try again in a moment.");
                return;
            }

            if (user == null)
            {
                ShowError("We couldn't find your account. Please sign in again.");
                return;
            }

            //Check the password with the team's shared hasher (same check as Login)
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
                int attempts = (Session[AttemptsKey] as int? ?? 0) + 1;
                Session[AttemptsKey] = attempts;

                if (attempts >= MaxPasswordAttempts)
                {
                    // Too many wrong guesses: end the session and send to login.
                    AuthSession.SignOut(Context);
                    Response.Redirect("~/Account/Login.aspx", false);
                    Context.ApplicationInstance.CompleteRequest();
                    return;
                }

                ShowError("That password is incorrect.");
                return;
            }

            //Save the email BEFORE deleting, because the record will be gone afterwards.
            var email = (user.Email ?? "").Trim();

            //Delete everything stored about this user (database + remaining XML files)
            try
            {
                AccountDeletionService.DeleteUser(Server, user.UserId);
            }
            catch (Exception)
            {
                // The database delete runs last and in one transaction, so if anything failed
                // the account still exists and the user can simply try again.
                ShowError("We couldn't delete your account right now. Please try again in a moment.");
                return;
            }

            //Security record without personal details (no email or name)
            UserRepository.AppendAudit("Account Deleted",
                "Participant deleted their own account.",
                userId: user.UserId, role: user.Role, university: user.University);

            //Discreet confirmation email
            //Plain subject and body with no details about the platform's topics,
            //in case someone else sees the inbox. If email isn't set up, this is skipped.
            EmailSender.TrySend(
                email,
                "Your request is complete",
                "Hello,\r\n\r\n" +
                "This confirms that your account has been closed and the information " +
                "associated with it has been removed, as you requested.\r\n\r\n" +
                "If you did not make this request, you can reply to this email.\r\n");

            //End the session and show the confirmation
            AuthSession.SignOut(Context);
            SettingsPanel.Visible = false;
            DonePanel.Visible = true;
        }

        private void ShowError(string message)
        {
            FormMessage.Text = Server.HtmlEncode(message);
        }
    }
}