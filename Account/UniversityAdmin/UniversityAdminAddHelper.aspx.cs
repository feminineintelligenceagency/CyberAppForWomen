using CyberApp_FIA.Services;
using System;
using System.Data.SqlClient;
using System.Web.UI;

namespace CyberApp_FIA.Account
{
    /// <summary>
    /// University Admin page for creating Helper accounts
    /// - Only University Admins can view the page or submit the form
    /// - The Helper is always linked to the signed-in admin's own university, looked up on the server 
    /// - Hashes the password with Argon2 (passHasher.HashNewPassword)
    /// - Creates the user and credentials in one transaction (UserRepository.CreateUser)
    /// </summary>
    public partial class UniversityAdminAddHelper : SecurePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // Access gate on EVERY request, including button postbacks.
            if (!IsUniversityAdmin())
            {
                Response.Redirect("~/Account/Login.aspx");
                return;
            }

            if (!IsPostBack)
            {
                var uni = GetAdminUniversity();
                UniversityDisplay.Text = Server.HtmlEncode(string.IsNullOrWhiteSpace(uni) ? "(not set)" : uni);

                // Kept only so the existing markup still works. The value is NOT trusted
                // when creating the Helper, because anything in the page can be edited in the browser.
                UniversityValue.Value = uni ?? string.Empty;
            }
        }

        protected void BtnBack_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/Account/UniversityAdmin/UniversityAdminHome.aspx");
        }

        /// <summary>
        /// Creates a new Helper linked to this University Admin's university.
        /// </summary>
        protected void BtnCreateHelper_Click(object sender, EventArgs e)
        {
            if (!IsUniversityAdmin())
            {
                Response.Redirect("~/Account/Login.aspx");
                return;
            }

            if (!Page.IsValid) return;

            var email = (NewHelperEmail.Text ?? "").Trim().ToLowerInvariant();
            var firstName = (NewHelperFirstName.Text ?? "").Trim();
            var lastName = (NewHelperLastName.Text ?? "").Trim();
            var password = NewHelperPassword.Text ?? "";

            if (email.Length == 0 || firstName.Length == 0 || lastName.Length == 0 || password.Length == 0)
            {
                ShowError("All fields are required.");
                return;
            }

            UserRepository.NewUser newUser;
            string uni;
            try
            {
                // Always use the admin's university from the server
                uni = GetAdminUniversity();
                if (string.IsNullOrWhiteSpace(uni))
                {
                    ShowError("Your university is not set. Please contact a Super Admin.");
                    return;
                }

                if (UserRepository.EmailExists(email))
                {
                    ShowError("A user with this email already exists.");
                    return;
                }

                newUser = new UserRepository.NewUser
                {
                    UserId = Guid.NewGuid().ToString("N"),
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    Role = "Helper",
                    University = uni,
                    // The Helper hasn't accepted the consent form themselves yet,
                    // so these stay empty rather than recording the admin's acceptance.
                    ConsentAcceptedAtUtc = null,
                    ConsentIp = null
                };

                if (!UserRepository.CreateUser(newUser, passHasher.HashNewPassword(password)))
                {
                    ShowError("A user with this email already exists.");
                    return;
                }
            }
            catch (SqlException)
            {
                ShowError("We couldn't create the account right now. Please try again in a moment.");
                return;
            }

            // Record who created the Helper (best-effort; never blocks creation).
            try
            {
                UniversityAuditLogger.AppendForCurrentUser(
                    this,
                    "Helper Created",
                    $"University Admin {Session["Email"]} created Helper {email} (userId={newUser.UserId}) for {uni}.");
            }
            catch { }

            HelperFormMessage.Text = "<span style='color:#0a7a3c'>Helper account created and linked to your university.</span>";

            // Clear inputs but keep university visible.
            NewHelperEmail.Text = "";
            NewHelperFirstName.Text = "";
            NewHelperLastName.Text = "";
            NewHelperPassword.Text = "";
        }

        private bool IsUniversityAdmin()
        {
            var role = (Session["Role"] as string ?? "").Trim();
            return string.Equals(role, "UniversityAdmin", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// The signed-in admin's university: from the session (set at login from the database),
        /// falling back to a database lookup by email.
        /// </summary>
        private string GetAdminUniversity()
        {
            var uni = (Session["University"] as string ?? "").Trim();
            if (uni.Length > 0) return uni;

            var email = Session["Email"] as string;
            if (string.IsNullOrWhiteSpace(email)) return "";

            var admin = UserRepository.GetLoginByEmail(email);
            return (admin?.University ?? "").Trim();
        }

        private void ShowError(string message)
        {
            HelperFormMessage.Text = "<span style='color:#b91c1c'>" + Server.HtmlEncode(message) + "</span>";
        }
    }
}