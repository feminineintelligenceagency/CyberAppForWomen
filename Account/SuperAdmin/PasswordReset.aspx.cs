using System;
using System.IO;
using System.Security.Cryptography;
using System.Web.UI;
using System.Xml;

namespace CyberApp_FIA.Account.SuperAdmin
{
    /// <summary>
    /// Super Admin password reset page.
    /// 
    /// This page updates an existing user's passwordHash and passwordSalt in users.xml.
    /// It uses the same PBKDF2 hashing pattern used by the FIA account system:
    /// - 16-byte salt
    /// - 32-byte hash
    /// - 100,000 iterations
    /// - Base64 stored values
    /// </summary>
    public partial class PasswordReset : SecurePage
    {
        private const int SaltByteSize = 16;
        private const int HashByteSize = 32;
        private const int PasswordHashIterations = 100000;

        private string UsersXmlPath => Server.MapPath("~/App_Data/users.xml");

        protected void Page_Load(object sender, EventArgs e)
        {
            RequireSuperAdmin();

            if (!IsPostBack)
            {
                WelcomeName.Text = (string)Session["Email"] ?? "Super Admin";
            }
        }

        protected void BtnResetPassword_Click(object sender, EventArgs e)
        {
            Page.Validate("ResetPassword");

            if (!Page.IsValid)
            {
                return;
            }

            var emailAddress = EmailAddress.Text?.Trim();
            var newPassword = NewPassword.Text ?? "";

            if (string.IsNullOrWhiteSpace(emailAddress))
            {
                ShowError("Please enter the user's email address.");
                return;
            }

          
            if (!File.Exists(UsersXmlPath))
            {
                ShowError("users.xml could not be found in App_Data.");
                return;
            }

            var doc = new XmlDocument
            {
                PreserveWhitespace = true
            };

            doc.Load(UsersXmlPath);

            var user = FindUserByEmail(doc, emailAddress);

            if (user == null)
            {
                ShowError("No account was found with that email address.");
                return;
            }

            string passwordHash;
            string passwordSalt;

            CreatePasswordHash(newPassword, out passwordHash, out passwordSalt);

            SetOrCreateChildText(doc, user, "passwordHash", passwordHash);
            SetOrCreateChildText(doc, user, "passwordSalt", passwordSalt);

            doc.Save(UsersXmlPath);

            EmailAddress.Text = "";
            NewPassword.Text = "";
            ConfirmPassword.Text = "";

            ShowSuccess("Password reset saved successfully. The user can now sign in with the new password.");
        }

        protected void BtnClear_Click(object sender, EventArgs e)
        {
            EmailAddress.Text = "";
            NewPassword.Text = "";
            ConfirmPassword.Text = "";
            ResetMessage.Text = "";
        }

        private void RequireSuperAdmin()
        {
            var role = Session["Role"] as string;

            if (!string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
            {
                Response.Redirect("~/Account/Login.aspx");
            }
        }

        private XmlElement FindUserByEmail(XmlDocument doc, string emailAddress)
        {
            var users = doc.SelectNodes("/users/user");

            if (users == null)
            {
                return null;
            }

            foreach (XmlElement user in users)
            {
                var storedEmail = user["email"]?.InnerText?.Trim();

                if (string.Equals(storedEmail, emailAddress, StringComparison.OrdinalIgnoreCase))
                {
                    return user;
                }
            }

            return null;
        }

        private static void CreatePasswordHash(string password, out string passwordHash, out string passwordSalt)
        {
            var saltBytes = new byte[SaltByteSize];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }

            using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, PasswordHashIterations))
            {
                var hashBytes = pbkdf2.GetBytes(HashByteSize);

                passwordHash = Convert.ToBase64String(hashBytes);
                passwordSalt = Convert.ToBase64String(saltBytes);
            }
        }

        private static void SetOrCreateChildText(XmlDocument doc, XmlElement parent, string childName, string value)
        {
            var child = parent[childName];

            if (child == null)
            {
                child = doc.CreateElement(childName);
                parent.AppendChild(child);
            }

            child.InnerText = value ?? "";
        }

        private void ShowSuccess(string message)
        {
            ResetMessage.Text = "<span style='color:#0a7a3c'>" + Server.HtmlEncode(message) + "</span>";
        }

        private void ShowError(string message)
        {
            ResetMessage.Text = "<span style='color:#c21d1d'>" + Server.HtmlEncode(message) + "</span>";
        }
    }
}