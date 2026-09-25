using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web.UI;
using System.Xml;

namespace CyberApp_FIA.Account
{
    /// <summary>
    /// Code-behind for a sign-up page that persists users into an XML file.
    /// Handles consent validation, unique-email enforcement, secure password hashing (PBKDF2),
    /// and creates the XML datastore on first run.
    /// </summary>
    public partial class CreateAccountPage : Page
    {
        /// <summary>
        /// Physical path to ~/App_Data/users.xml (per-app sandboxed data folder in ASP.NET).
        /// Server.MapPath translates the virtual path to a filesystem path.
        /// </summary>
        private string XmlPath => Server.MapPath("~/App_Data/users.xml");

        private string AuditXmlPath => Server.MapPath("~/App_Data/Audit_Log/UnvAdminAudit.xml");


        /// <summary>
        /// Click handler for the "Sign Up" button.
        /// Validates inputs, double-checks consent, hashes password with per-user salt,
        /// ensures XML store exists, enforces unique email, and appends new user record.
        /// </summary>
        protected void BtnSignUp_Click(object sender, EventArgs e)
        {
            // Honor ASP.NET validators (RequiredFieldValidator, RegularExpressionValidator, CustomValidator, etc.).
            // If any Page.Validate() rules failed, do nothing.
            if (!Page.IsValid) return;

            // Defense in depth: even though a CustomValidator checks consent, we re-check server-side.
            if (!Consent.Checked)
            {
                // Show a red inline message and stop. (Note: immediately after we set this, we redirect on success;
                // on error we DO NOT redirect, so the message is visible.)
                FormMessage.Text = "<span style='color:#c21d1d'>Please accept the consent.</span>";
                return;
            }

            // --- Prepare new user metadata ---
            // Generate a compact GUID without dashes as the primary identifier.
            var id = Guid.NewGuid().ToString("N");

            // Use an ISO 8601 UTC timestamp for consistent server/client parsing, auditing, and sorting.
            var createdAt = DateTime.UtcNow.ToString("o");
            var consentAt = createdAt; // Consent captured at the same instant as creation.

            // --- Derive secure password hash ---
            // Generate a cryptographically strong random salt (16 bytes here; can be 16-32+).
            var salt = GenerateSalt(16);

            // Derive a 256-bit hash via PBKDF2 using the provided password and the per-user salt.
            // Iteration count is set in HashPassword (100k). Consider tuning based on environment.
            var hash = HashPassword(Password.Text, salt);

            // Ensure the XML users store exists and is initialized with a root <users> element.
            EnsureXml();

            // Enforce case-insensitive uniqueness of email. If already present, bail out gracefully.
            if (EmailExists(Email.Text))
            {
                FormMessage.Text = "<span style='color:#c21d1d'>That email is already registered.</span>";
                return;
            }

            // --- Persist the new user into users.xml ---
            var doc = new XmlDocument();
            doc.Load(XmlPath);

            // Create <user> node with attributes and child elements.
            var user = doc.CreateElement("user");

            // Attributes: immutable id, and role (posted from the form; e.g., "Participant", "Helper", etc.).
            user.SetAttribute("id", id);
            user.SetAttribute("role", Role.Value); // Example: "Participant"

            // Child nodes: first/last name; canonicalized email; university placeholder; password material; audit fields.
            user.AppendChild(Mk(doc, "firstName", FirstName.Text.Trim()));
            user.AppendChild(Mk(doc, "lastName", LastName.Text.Trim()));

            // Lowercase invariant email to enforce case-insensitive uniqueness and consistent comparisons.
            user.AppendChild(Mk(doc, "email", Email.Text.Trim().ToLowerInvariant()));

            // Placeholder for university selection; kept empty here but slot exists for future updates.
            user.AppendChild(Mk(doc, "university", ""));

            // Store the PBKDF2 hash and salt as Base64 strings. (Never store plaintext passwords.)
            user.AppendChild(Mk(doc, "passwordHash", Convert.ToBase64String(hash)));
            user.AppendChild(Mk(doc, "passwordSalt", Convert.ToBase64String(salt)));

            // Timestamps and consent audit data.
            user.AppendChild(Mk(doc, "createdAt", createdAt));
            user.AppendChild(Mk(doc, "consentAcceptedAt", consentAt));

            // Record client IP best-effort for audit (may be proxy/NAT; treat as informational).
            user.AppendChild(Mk(doc, "consentIp", Request.UserHostAddress ?? ""));

            // Attach new <user> to the document root and save to disk.
            doc.DocumentElement.AppendChild(user);
            doc.Save(XmlPath);

            // Write audit log entry that the account was created and consent accepted.
            WriteAccountCreatedAuditEntry(
                userId: id,
                email: Email.Text.Trim().ToLowerInvariant(),
                firstName: FirstName.Text.Trim(),
                role: Role.Value,                      // e.g., "Participant"
                consentIp: Request.UserHostAddress ?? "",
                consentAtUtc: consentAt               // same UTC timestamp you used for consentAcceptedAt
            );

            // Inform the user and navigate to the login page.
            // Note: Since Response.Redirect occurs immediately, this success message won't be seen on the current page.
            FormMessage.Text = "<span style='color:#0a7a3c'>Account created! You can sign in now.</span>";
            Response.Redirect("~/Account/Login.aspx");
        }

        /// <summary>
        /// CustomValidator server-side hook for the consent checkbox.
        /// This makes consent a hard requirement regardless of client-side scripts.
        /// </summary>
        protected void ConsentValidator_ServerValidate(object source, System.Web.UI.WebControls.ServerValidateEventArgs args)
            => args.IsValid = Consent.Checked;

        // -------------------------
        // Helpers
        // -------------------------

        /// <summary>
        /// Creates the users.xml store if it does not exist, with a versioned <users> root.
        /// Ensures the App_Data folder exists and writes a UTF-8 XML file.
        /// </summary>
        private void EnsureXml()
        {
            if (File.Exists(XmlPath)) return;

            // Make sure the directory path exists (MapPath can point to a path that isn't created yet).
            Directory.CreateDirectory(Path.GetDirectoryName(XmlPath));

            // Minimal, versioned users document. Version could be used to drive future migrations.
            var init = "<?xml version='1.0' encoding='utf-8'?><users version='1'></users>";

            // Explicitly write UTF-8 with BOM-less encoding to avoid parser issues across environments.
            File.WriteAllText(XmlPath, init, Encoding.UTF8);
        }

        /// <summary>
        /// Checks for an existing user with the same email (case-insensitive) using XPath.
        /// Uses translate() to normalize the stored email to lowercase before comparison.
        /// </summary>
        private bool EmailExists(string email)
        {
            var doc = new XmlDocument();
            doc.Load(XmlPath);

            // The XPath expression:
            // /users/user[translate(email,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='{lower}']
            // translates any stored email to lowercase and compares with the provided lowercase target.
            var node = doc.SelectSingleNode($"/users/user[translate(email,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='{email.ToLower()}']");
            return node != null;
        }

        // Creates the UnvAdminAudit.xml store if it does not exist.
        private void EnsureAuditXml()
        {
            var dir = Path.GetDirectoryName(AuditXmlPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (!File.Exists(AuditXmlPath))
            {
                var init = "<?xml version='1.0' encoding='utf-8'?><auditLog version='1'></auditLog>";
                File.WriteAllText(AuditXmlPath, init, Encoding.UTF8);
            }
        }

        /// <summary>
        /// Writes an audit entry that a new account was created and consent was accepted.
        /// </summary>
        private void WriteAccountCreatedAuditEntry(
            string userId,
            string email,
            string firstName,
            string role,
            string consentIp,
            string consentAtUtc)
        {
            try
            {
                EnsureAuditXml();

                var doc = new XmlDocument();
                doc.Load(AuditXmlPath);

                var entry = doc.CreateElement("entry");
                entry.SetAttribute("id", "log-" + Guid.NewGuid().ToString("N"));

                // NOTE: adjust this if you later capture university at sign-up.
                entry.SetAttribute("university", "Arizona State University");

                entry.SetAttribute("role", string.IsNullOrWhiteSpace(role) ? "Participant" : role);
                entry.SetAttribute("type", "Account Created");
                entry.SetAttribute("timestamp", DateTime.UtcNow.ToString("o"));
                entry.SetAttribute("email", email ?? string.Empty);
                entry.SetAttribute("firstName", firstName ?? string.Empty);

                var details = doc.CreateElement("details");
                details.InnerText =
                    $"New {entry.GetAttribute("role")} account created (userId={userId}). " +
                    $"Consent accepted at {consentAtUtc} UTC from IP {consentIp}.";
                entry.AppendChild(details);

                doc.DocumentElement.AppendChild(entry);
                doc.Save(AuditXmlPath);
            }
            catch
            {
                // Fail-safe: never block sign-up on audit logging.
            }
        }


        /// <summary>
        /// Utility: creates an element with text content, safely defaulting nulls to empty string.
        /// </summary>
        private static XmlElement Mk(XmlDocument d, string name, string val)
        {
            var el = d.CreateElement(name);
            el.InnerText = val ?? "";
            return el;
        }

        /// <summary>
        /// Generates a cryptographically strong random salt of the requested byte length.
        /// RNGCryptoServiceProvider is a secure PRNG suitable for cryptographic material.
        /// </summary>
        private static byte[] GenerateSalt(int size)
        {
            var salt = new byte[size];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt); // Fills the array with nonzero, uniformly random bytes.
            }
            return salt;
        }

        /// <summary>
        /// Derives a password hash using PBKDF2 (Rfc2898DeriveBytes).
        /// - Uses the provided per-user salt.
        /// - 100,000 iterations (demo-friendly; consider higher for production as hardware allows).
        /// - Returns a 32-byte (256-bit) derived key suitable for storage.
        /// </summary>
        private static byte[] HashPassword(string password, byte[] salt)
        {
            // NOTE: In .NET Framework, Rfc2898DeriveBytes defaults to HMACSHA1.
            // In newer .NETs, you can specify HMACSHA256 explicitly if available.
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000))
            {
                return pbkdf2.GetBytes(32); // 256-bit hash
            }
        }
    }
}

