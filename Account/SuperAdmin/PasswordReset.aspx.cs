using System;
using System.IO;
using System.Security.Cryptography;
using System.Web.UI;
using System.Xml;
using CyberApp_FIA.Account;

namespace CyberApp_FIA.Account.SuperAdmin
{
    public partial class PasswordReset : Page
    {
        private const int SaltByteSize = 16;
        private const int HashByteSize = 32;
        private const int PasswordHashIterations = 100000;
        private string UsersXmlPath => Server.MapPath("~/App_Data/users.xml");

        protected void Page_Load(object sender, EventArgs e)
        {
            RequireSuperAdmin();
            if (!IsPostBack) WelcomeName.Text = (string)Session["Email"] ?? "Super Admin";
        }

        // Epic 1 - User Story #6 (dmundra-29): allow a super admin to reset a user's password, hash it, and clear any existing lockout state.
        protected void BtnResetPassword_Click(object sender, EventArgs e)
        {
            Page.Validate("ResetPassword");
            if (!Page.IsValid) return;
            var emailAddress = EmailAddress.Text?.Trim();
            if (string.IsNullOrWhiteSpace(emailAddress)) { ShowError("Please enter the user's email address."); return; }
            if (!File.Exists(UsersXmlPath)) { ShowError("users.xml could not be found in App_Data."); return; }

            var doc = new XmlDocument { PreserveWhitespace = true };
            doc.Load(UsersXmlPath);
            var user = FindUserByEmail(doc, emailAddress);
            if (user == null) { ShowError("No account was found with that email address."); return; }

            string hash, salt;
            CreatePasswordHash(NewPassword.Text ?? "", out hash, out salt);
            SetOrCreateChildText(doc, user, "passwordHash", hash);
            SetOrCreateChildText(doc, user, "passwordSalt", salt);
            LoginRateLimiting.ClearLockout(doc, user);
            doc.Save(UsersXmlPath);

            EmailAddress.Text = ""; NewPassword.Text = ""; ConfirmPassword.Text = "";
            ShowSuccess("Password reset saved successfully. The user can now sign in with the new password.");
        }

        protected void BtnClear_Click(object sender, EventArgs e)
        {
            EmailAddress.Text = ""; NewPassword.Text = ""; ConfirmPassword.Text = ""; ResetMessage.Text = "";
        }

        private void RequireSuperAdmin()
        {
            if (!string.Equals(Session["Role"] as string, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
                Response.Redirect("~/Account/Login.aspx");
        }

        private XmlElement FindUserByEmail(XmlDocument doc, string email)
        {
            var users = doc.SelectNodes("/users/user");
            if (users == null) return null;
            foreach (XmlElement user in users)
                if (string.Equals(user["email"]?.InnerText?.Trim(), email, StringComparison.OrdinalIgnoreCase)) return user;
            return null;
        }

        private static void CreatePasswordHash(string password, out string hash, out string salt)
        {
            var saltBytes = new byte[SaltByteSize];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(saltBytes);
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, PasswordHashIterations))
            {
                hash = Convert.ToBase64String(pbkdf2.GetBytes(HashByteSize));
                salt = Convert.ToBase64String(saltBytes);
            }
        }

        private static void SetOrCreateChildText(XmlDocument doc, XmlElement parent, string name, string value)
        {
            var child = parent[name];
            if (child == null) { child = doc.CreateElement(name); parent.AppendChild(child); }
            child.InnerText = value ?? "";
        }

        private void ShowSuccess(string message) { ResetMessage.Text = "<span style='color:#0a7a3c'>" + Server.HtmlEncode(message) + "</span>"; }
        private void ShowError(string message) { ResetMessage.Text = "<span style='color:#c21d1d'>" + Server.HtmlEncode(message) + "</span>"; }
    }
}
