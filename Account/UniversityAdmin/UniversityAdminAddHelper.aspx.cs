using System;
using System.IO;
using System.Security.Cryptography;
using System.Web.UI;
using System.Xml;

namespace CyberApp_FIA.Account
{
    public partial class UniversityAdminAddHelper : SecurePage
    {
        /// <summary>
        /// Physical path to the users XML datastore.
        /// </summary>
        private string UsersXmlPath => Server.MapPath("~/App_Data/users.xml");

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // Access gate: must be UniversityAdmin
                var role = (string)Session["Role"];
                if (!string.Equals(role, "UniversityAdmin", StringComparison.OrdinalIgnoreCase))
                {
                    Response.Redirect("~/Account/Login.aspx");
                    return;
                }

                var email = (string)Session["Email"] ?? string.Empty;

                // Prefer university from session, else lookup via users.xml
                var uni = (string)Session["University"];
                if (string.IsNullOrWhiteSpace(uni))
                {
                    uni = LookupUniversityByEmail(email);
                }

                UniversityDisplay.Text = string.IsNullOrWhiteSpace(uni) ? "(not set)" : uni;
                UniversityValue.Value = uni ?? string.Empty;
            }
        }

        protected void BtnBack_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/Account/UniversityAdmin/UniversityAdminHome.aspx");
        }

        /// <summary>
        /// Handles creation of a new Helper user scoped to this University Admin's university.
        /// </summary>
        protected void BtnCreateHelper_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            var uni = (UniversityValue.Value ?? "").Trim();
            var emailRaw = (NewHelperEmail.Text ?? "").Trim();
            var firstName = (NewHelperFirstName.Text ?? "").Trim();
            var lastName = (NewHelperLastName.Text ?? "").Trim();
            var password = (NewHelperPassword.Text ?? "").Trim();

            if (string.IsNullOrEmpty(uni))
            {
                HelperFormMessage.Text = "<span style='color:#b91c1c'>Your university is not set. Please contact a Super Admin.</span>";
                return;
            }

            if (string.IsNullOrWhiteSpace(emailRaw) ||
                string.IsNullOrWhiteSpace(firstName) ||
                string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(password))
            {
                HelperFormMessage.Text = "<span style='color:#b91c1c'>All fields are required.</span>";
                return;
            }

            // Normalize email to lowercase for storage and comparison (matches CreateAccount/Login behavior).
            var emailLower = emailRaw.ToLowerInvariant();

            EnsureUsersXml();

            var doc = new XmlDocument();
            doc.Load(UsersXmlPath);

            // Check for duplicate email (case-insensitive)
            var existing = doc.SelectSingleNode(
                $"/users/user[translate(email,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='{emailLower}']");
            if (existing != null)
            {
                HelperFormMessage.Text = "<span style='color:#b91c1c'>A user with this email already exists.</span>";
                return;
            }

            // Hash and salt the password using the same PBKDF2 pattern as CreateAccount/Login.
            HashPassword(password, out var hashB64, out var saltB64);

            var root = doc.DocumentElement ?? doc.AppendChild(doc.CreateElement("users"));

            var userEl = doc.CreateElement("user");
            userEl.SetAttribute("id", Guid.NewGuid().ToString("N"));
            userEl.SetAttribute("role", "Helper");

            // Children: firstName, lastName, email (lowercased), university, passwordHash, passwordSalt, createdAt, consentAcceptedAt, consentIp
            userEl.AppendChild(Mk(doc, "firstName", firstName));
            userEl.AppendChild(Mk(doc, "lastName", lastName));
            userEl.AppendChild(Mk(doc, "email", emailLower));
            userEl.AppendChild(Mk(doc, "university", uni));
            userEl.AppendChild(Mk(doc, "passwordHash", hashB64));
            userEl.AppendChild(Mk(doc, "passwordSalt", saltB64));

            var nowIso = DateTime.UtcNow.ToString("o");
            userEl.AppendChild(Mk(doc, "createdAt", nowIso));
            userEl.AppendChild(Mk(doc, "consentAcceptedAt", nowIso));
            var ip = Request.UserHostAddress ?? "";
            userEl.AppendChild(Mk(doc, "consentIp", ip));

            root.AppendChild(userEl);
            doc.Save(UsersXmlPath);

            HelperFormMessage.Text = "<span style='color:#0a7a3c'>Helper account created and linked to your university.</span>";

            // Clear inputs but keep university visible.
            NewHelperEmail.Text = "";
            NewHelperFirstName.Text = "";
            NewHelperLastName.Text = "";
            NewHelperPassword.Text = "";
        }

        // ---------- Helpers ----------

        private void EnsureUsersXml()
        {
            if (File.Exists(UsersXmlPath)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(UsersXmlPath) ?? "");
            var init = "<?xml version='1.0' encoding='utf-8'?><users version='1'></users>";
            File.WriteAllText(UsersXmlPath, init);
        }

        private static XmlElement Mk(XmlDocument d, string name, string value)
        {
            var el = d.CreateElement(name);
            el.InnerText = value ?? "";
            return el;
        }

        /// <summary>
        /// Looks up a user's university by email in users.xml (case-insensitive).
        /// </summary>
        private string LookupUniversityByEmail(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email) || !File.Exists(UsersXmlPath)) return "";
                var doc = new XmlDocument();
                doc.Load(UsersXmlPath);
                var emailLower = email.ToLowerInvariant();

                var node = doc.SelectSingleNode(
                    $"/users/user[translate(email,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='{emailLower}']");
                return node?["university"]?.InnerText ?? "";
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Generates a salted PBKDF2 hash using the same parameters as CreateAccountPage/Login:
        /// - 16-byte random salt
        /// - 100,000 iterations
        /// - 32-byte (256-bit) derived key
        /// Returns both hash and salt as Base64 strings.
        /// </summary>
        private static void HashPassword(string password, out string hash, out string salt)
        {
            var saltBytes = GenerateSalt(16);
            var hashBytes = HashPasswordCore(password, saltBytes);

            salt = Convert.ToBase64String(saltBytes);
            hash = Convert.ToBase64String(hashBytes);
        }

        /// <summary>
        /// Generates a cryptographically strong random salt of the requested byte length.
        /// Mirrors CreateAccountPage.GenerateSalt.
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
        /// Derives a password hash using PBKDF2 (Rfc2898DeriveBytes) with 100,000 iterations.
        /// Mirrors CreateAccountPage.HashPassword and Login.HashPassword.
        /// </summary>
        private static byte[] HashPasswordCore(string password, byte[] salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000))
            {
                return pbkdf2.GetBytes(32); // 256-bit hash
            }
        }
    }
}
