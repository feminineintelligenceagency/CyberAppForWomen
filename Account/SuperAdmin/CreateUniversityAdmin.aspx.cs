using CyberApp_FIA.Services;
using System;
using System.Data.SqlClient;
using System.Web.UI;

namespace CyberApp_FIA.Account
{
    /// <summary>
    /// SuperAdmin page for creating University Admin accounts (database version).
    /// - Only SuperAdmins can view the page or submit the form
    /// - Hashes the password with Argon2 (passHasher.HashNewPassword)
    /// - Creates the user and credentials in one transaction (UserRepository.CreateUser)
    /// - Writes an audit entry recording which SuperAdmin created the account
    /// </summary>
    public partial class CreateUniversityAdmin : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // Access gate on EVERY request, including button postbacks, not just the first load.
            if (!IsSuperAdmin())
            {
                Response.Redirect("~/Account/Login.aspx");
                return;
            }

            if (!IsPostBack)
            {
                WelcomeName.Text = Server.HtmlEncode(Session["Email"] as string ?? "Super Admin");
            }
        }

        protected void BtnBackHome_Click(object sender, EventArgs e)
        {
            // NOTE: CausesValidation="false" in markup, so this works even if fields are empty/invalid.
            Response.Redirect("~/Account/SuperAdmin/SuperAdminHome.aspx");
        }

        protected void BtnLogout_Click(object sender, EventArgs e)
        {
            AuthSession.SignOut(Context);   // Epic #7: fully end the session and delete session + token cookies
            Response.Redirect("~/Welcome_Page.aspx");
        }

        protected void BtnCreate_Click(object sender, EventArgs e)
        {
            // Defense in depth: re-check the role right where the account is created.
            if (!IsSuperAdmin())
            {
                Response.Redirect("~/Account/Login.aspx");
                return;
            }

            // Run ASP.NET validators (RequiredFieldValidator, etc.)
            if (!Page.IsValid) return;

            var firstName = (FirstName.Text ?? string.Empty).Trim();
            var lastName = (LastName.Text ?? string.Empty).Trim();
            var email = (Email.Text ?? string.Empty).Trim().ToLowerInvariant();
            var university = (University.Text ?? string.Empty).Trim();
            var password = Password.Text ?? string.Empty;

            if (firstName.Length == 0 || lastName.Length == 0 || email.Length == 0 ||
                university.Length == 0 || password.Length == 0)
            {
                ShowError("All fields are required.");
                return;
            }

            UserRepository.NewUser newUser;
            try
            {
                // Quick check first so we don't spend Argon2 time on a duplicate.
                if (UserRepository.EmailExists(email))
                {
                    ShowError("That email is already registered.");
                    return;
                }

                newUser = new UserRepository.NewUser
                {
                    UserId = Guid.NewGuid().ToString("N"),
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    Role = "UniversityAdmin",
                    University = university,
                    // The admin didn't accept the participant consent form themselves,
                    // so these stay empty rather than recording the SuperAdmin's acceptance.
                    ConsentAcceptedAtUtc = null,
                    ConsentIp = null
                };

                if (!UserRepository.CreateUser(newUser, passHasher.HashNewPassword(password)))
                {
                    // Someone registered the same email between the check and the insert.
                    ShowError("That email is already registered.");
                    return;
                }
            }
            catch (SqlException)
            {
                ShowError("We couldn't create the account right now. Please try again in a moment.");
                return;
            }

            // Record who created the admin account (best-effort; never blocks creation).
            try
            {
                UniversityAuditLogger.AppendForCurrentUser(
                    this,
                    "University Admin Created",
                    $"SuperAdmin {Session["Email"]} created University Admin {email} (userId={newUser.UserId}) for {university}.");
            }
            catch { }

            FormMessage.Text = "<span style='color:#0a7a3c'>University Admin account created.</span>";
            ClearForm();
        }

        // -------------------------
        // Helpers
        // -------------------------

        private bool IsSuperAdmin()
        {
            var role = (Session["Role"] as string ?? "").Trim();
            return string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);
        }

        private void ShowError(string message)
        {
            FormMessage.Text = "<span style='color:#c21d1d'>" + Server.HtmlEncode(message) + "</span>";
        }

        private void ClearForm()
        {
            FirstName.Text = string.Empty;
            LastName.Text = string.Empty;
            Email.Text = string.Empty;
            University.Text = string.Empty;
            Password.Text = string.Empty;
        }
    }
}