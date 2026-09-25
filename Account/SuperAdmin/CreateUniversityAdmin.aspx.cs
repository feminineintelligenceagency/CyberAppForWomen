using CyberApp_FIA.Services;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web.UI;
using System.Xml;

namespace CyberApp_FIA.Account
{
    public partial class CreateUniversityAdmin : Page
    {
        /// <summary>
        /// Physical path to ~/App_Data/users.xml.
        /// </summary>
        private string XmlPath => Server.MapPath("~/App_Data/users.xml");

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // ---- Access Gate: only SuperAdmin role ----
                var role = (string)Session["Role"];
                if (!string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
                {
                    Response.Redirect("~/Account/Login.aspx");
                    return;
                }

                WelcomeName.Text = (string)Session["Email"] ?? "Super Admin";
                EnsureXml();
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
            // Run ASP.NET validators (RequiredFieldValidator, etc.)
            if (!Page.IsValid) return;

            var firstName = (FirstName.Text ?? string.Empty).Trim();
            var lastName = (LastName.Text ?? string.Empty).Trim();
            var emailRaw = (Email.Text ?? string.Empty).Trim();
            var university = (University.Text ?? string.Empty).Trim();
            var password = Password.Text ?? string.Empty;

            if (string.IsNullOrWhiteSpace(firstName) ||
                string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(emailRaw) ||
                string.IsNullOrWhiteSpace(university) ||
                string.IsNullOrWhiteSpace(password))
            {
                FormMessage.Text = "<span style='color:#c21d1d'>All fields are required.</span>";
                return;
            }

            // Lowercase invariant email for storage and comparisons, matching CreateAccountPage.
            var email = emailRaw.ToLowerInvariant();

            EnsureXml();

            // Enforce unique email across all roles.
            if (EmailExists(email))
            {
                FormMessage.Text = "<span style='color:#c21d1d'>That email is already registered.</span>";
                return;
            }

            // --- Prepare metadata ---
            var id = Guid.NewGuid().ToString("N");
            var createdAt = DateTime.UtcNow.ToString("o");
            var consentAt = createdAt; // For admin-created accounts we still track acceptance time.
            var consentIp = Request.UserHostAddress ?? string.Empty;

            // --- Derive PBKDF2 password hash (same as CreateAccountPage) ---
            var salt = GenerateSalt(16);                // 16-byte salt
            var hash = HashPassword(password, salt);    // 32-byte derived key

            var doc = new XmlDocument();
            doc.Load(XmlPath);

            var root = doc.DocumentElement;
            if (root == null)
            {
                root = doc.CreateElement("users");
                root.SetAttribute("version", "1");
                doc.AppendChild(root);
            }

            var user = doc.CreateElement("user");
            user.SetAttribute("id", id);
            user.SetAttribute("role", "UniversityAdmin");

            user.AppendChild(Mk(doc, "firstName", firstName));
            user.AppendChild(Mk(doc, "lastName", lastName));
            user.AppendChild(Mk(doc, "email", email));           // stored lowercase
            user.AppendChild(Mk(doc, "university", university));

            user.AppendChild(Mk(doc, "passwordHash", Convert.ToBase64String(hash)));
            user.AppendChild(Mk(doc, "passwordSalt", Convert.ToBase64String(salt)));

            user.AppendChild(Mk(doc, "createdAt", createdAt));
            user.AppendChild(Mk(doc, "consentAcceptedAt", consentAt));
            user.AppendChild(Mk(doc, "consentIp", consentIp));

            root.AppendChild(user);
            doc.Save(XmlPath);

            FormMessage.Text = "<span style='color:#0a7a3c'>University Admin account created.</span>";
            ClearForm();
        }

        // -------------------------
        // Helpers
        // -------------------------

        /// <summary>
        /// Ensures users.xml exists with a versioned &lt;users&gt; root, matching CreateAccountPage.
        /// </summary>
        private void EnsureXml()
        {
            if (File.Exists(XmlPath)) return;

            var dir = Path.GetDirectoryName(XmlPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var init = "<?xml version='1.0' encoding='utf-8'?><users version='1'></users>";
            File.WriteAllText(XmlPath, init, Encoding.UTF8);
        }

        /// <summary>
        /// Checks if an email already exists (case-insensitive) in users.xml, like CreateAccountPage.
        /// </summary>
        private bool EmailExists(string emailLower)
        {
            var doc = new XmlDocument();
            doc.Load(XmlPath);

            var node = doc.SelectSingleNode(
                $"/users/user[translate(email,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='{emailLower}']"
            );
            return node != null;
        }

        /// <summary>
        /// Utility: element with inner text.
        /// </summary>
        private static XmlElement Mk(XmlDocument d, string name, string val)
        {
            var el = d.CreateElement(name);
            el.InnerText = val ?? string.Empty;
            return el;
        }

        /// <summary>
        /// Generates a cryptographically strong random salt, same approach as CreateAccountPage.
        /// </summary>
        private static byte[] GenerateSalt(int size)
        {
            var salt = new byte[size];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }

        /// <summary>
        /// Derives a PBKDF2 password hash using the same pattern as CreateAccountPage:
        /// 100,000 iterations, 32-byte derived key.
        /// </summary>
        private static byte[] HashPassword(string password, byte[] salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000))
            {
                return pbkdf2.GetBytes(32);
            }
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

