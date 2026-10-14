using System;
using System.Web;
using System.Web.UI;
using CyberApp_FIA.Services;   // AuthSession (Epic #7)

namespace CyberApp_FIA.Account
{
    /// <summary>
    /// Reset Password page (opened from the email link).
    /// - Checks the token is real, not expired, and not used before showing the form.
    /// - Checks it again when the form is submitted, then saves the new password
    ///   and marks the token as used so the link only works once.
    /// </summary>
    public partial class ResetPassword : Page
    {
        private const int MinPasswordLength = 8;

        private string Token => Request.QueryString["token"] ?? "";

        protected void Page_Load(object sender, EventArgs e)
        {
            // Don't let the browser cache this page
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();

            if (!IsPostBack)
            {
                if (PasswordResetService.GetUserIdForToken(Token) == null)
                {
                    ShowInvalid();
                }
            }
        }

        protected void BtnReset_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            var newPassword = NewPassword.Text ?? "";

            if (newPassword.Length < MinPasswordLength)
            {
                FormMessage.Text = "Your password must be at least " + MinPasswordLength + " characters.";
                return;
            }

            // Checks the token again, marks it used, saves the password, stamps passwordChangedUtc
            if (!PasswordResetService.ResetPassword(Token, newPassword))
            {
                ShowInvalid();
                return;
            }

            // End any session in this browser too, so the user signs in fresh
            AuthSession.SignOut(Context);

            FormPanel.Visible = false;
            SuccessPanel.Visible = true;
        }

        private void ShowInvalid()
        {
            FormPanel.Visible = false;
            InvalidPanel.Visible = true;
        }
    }
}