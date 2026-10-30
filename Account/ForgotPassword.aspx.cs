using System;
using System.Diagnostics;
using System.IO;
using System.Web;
using System.Web.UI;
using System.Xml;

namespace CyberApp_FIA.Account
{
    /// <summary>
    /// Forgot Password page.
    /// The user types an email. If it matches an account, a reset link is emailed to the
    /// account's REGISTERED email address (from users.xml, not what was typed).
    /// The page always shows the same message, so it can't be used to check whose email is registered.
    /// </summary>
    public partial class ForgotPassword : Page
    {
        private string UsersXmlPath => Server.MapPath("~/App_Data/users.xml");

        protected void BtnSend_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            var typedEmail = (Email.Text ?? "").Trim();

            try
            {
                if (File.Exists(UsersXmlPath))
                {
                    var doc = new XmlDocument();
                    doc.Load(UsersXmlPath);

                    var user = PasswordResetService.FindUserByEmail(doc, typedEmail);
                    if (user != null)
                    {
                        var userId = user.GetAttribute("id");
                        var registeredEmail = user["email"]?.InnerText?.Trim();

                        if (!string.IsNullOrWhiteSpace(userId) && !string.IsNullOrWhiteSpace(registeredEmail))
                        {
                            var token = PasswordResetService.CreateToken(userId);

                            var link = Request.Url.GetLeftPart(UriPartial.Authority)
                                       + ResolveUrl("~/Account/ResetPassword.aspx")
                                       + "?token=" + HttpUtility.UrlEncode(token);

                            var body =
                                "We received a request to reset your password." + Environment.NewLine + Environment.NewLine +
                                "Use this link within " + (int)PasswordResetService.TokenLifetime.TotalMinutes + " minutes. It only works once:" + Environment.NewLine +
                                link + Environment.NewLine + Environment.NewLine +
                                "If you didn't ask for this, you can ignore this email. Your password won't change.";

                            CyberApp_FIA.Services.EmailSender.TrySend(registeredEmail, body);   // Story #10: generic subject
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Don't show errors to the user (that could reveal whether the email exists).
                // Shows up in Visual Studio's Output window while debugging.
                Debug.WriteLine("ForgotPassword error: " + ex);
            }

            // Same message every time, whether or not the account exists
            FormPanel.Visible = false;
            FormMessage.Text =
                "If an account exists for that email, we've sent a link to reset your password. " +
                "The link expires in 30 minutes.";
        }
    }
}