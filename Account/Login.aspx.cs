using System;
using System.IO;
using System.Text;
using System.Web.UI;
using System.Xml;
using System.Security.Cryptography;
using CyberApp_FIA.Services;

namespace CyberApp_FIA.Account
{
    /// <summary>
    /// Login page code-behind.
    /// Authenticates a user against an XML store using PBKDF2 password verification,
    /// sets session variables, and redirects to a role-based landing page.
    /// </summary>
    public partial class Login : Page
    {
        /// <summary>
        /// Physical path to the XML user store (~/App_Data/users.xml).
        /// App_Data is not served directly by IIS, making it suitable for lightweight data files.
        /// </summary>
        private string XmlPath => Server.MapPath("~/App_Data/users.xml");

        /// <summary>
        /// Click handler for the Login button:
        /// - Validates the page
        /// - Loads users.xml
        /// - Locates user by email (case-insensitive)
        /// - Verifies password with PBKDF2 using the stored salt
        /// - On success, initializes session and redirects by role
        /// </summary>

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack && Request.QueryString["expired"] == "1")
            {
                // Make sure the old session is really dead on the server
                SessionHelper.SignOut(Context);

                FormMessage.Text = "<span style='color:#c21d1d'>Your session has ended. Please log in again.</span>";
            }
        }
        protected void BtnLogin_Click(object sender, EventArgs e)
        {
            // Respect ASP.NET validation controls (RequiredFieldValidator, etc.).
            if (!Page.IsValid) return;

            // If the user store is missing, there can be no accounts to authenticate against.
            if (!File.Exists(XmlPath))
            {
                FormMessage.Text = "<span style='color:#c21d1d'>No users found. Please create an account first.</span>";
                return;
            }

            // Load users.xml and look up the user node by normalized (lowercase) email.
            var doc = new XmlDocument();
            doc.Load(XmlPath);

            // Normalize input email to lowercase-invariant for consistent matching.
            var emailLower = Email.Text.Trim().ToLowerInvariant();

            // XPath uses translate() to normalize stored emails to lowercase, enabling case-insensitive search.
            var userNode = doc.SelectSingleNode(
                $"/users/user[translate(email,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='{emailLower}']");

            // If not found, do not reveal whether the email exists—return a generic failure message.
            if (userNode == null)
            {
                // --- AUDIT: log failed sign-in where the email does NOT exist in users.xml ---
                try
                {
                    var auditPath = Server.MapPath("~/App_Data/Audit_Log/UnvAdminAudit.xml");

                    // Ensure directory + file exist
                    var auditDir = Path.GetDirectoryName(auditPath);
                    if (!string.IsNullOrEmpty(auditDir) && !Directory.Exists(auditDir))
                    {
                        Directory.CreateDirectory(auditDir);
                    }

                    var auditDoc = new XmlDocument();
                    if (File.Exists(auditPath))
                    {
                        auditDoc.Load(auditPath);
                    }
                    else
                    {
                        auditDoc.LoadXml("<?xml version='1.0' encoding='utf-8'?><auditLog version='1'></auditLog>");
                    }

                    var entry = auditDoc.CreateElement("entry");
                    entry.SetAttribute("id", "log-" + Guid.NewGuid().ToString("N"));
                    entry.SetAttribute("university", ""); // unknown – email not found
                    entry.SetAttribute("role", "Unknown");
                    entry.SetAttribute("type", "Sign In Failed (Unknown Email)");
                    entry.SetAttribute("timestamp", DateTime.UtcNow.ToString("o"));
                    entry.SetAttribute("email", emailLower);   // the attempted email
                    entry.SetAttribute("firstName", "");       // unknown

                    var detailsEl = auditDoc.CreateElement("details");
                    detailsEl.InnerText = "Sign-in attempt with an email address that does not exist in users.xml.";
                    entry.AppendChild(detailsEl);

                    auditDoc.DocumentElement.AppendChild(entry);
                    auditDoc.Save(auditPath);
                }
                catch
                {
                    // Best-effort only; never block login failure flow if audit logging breaks.
                }

                FormMessage.Text = "<span style='color:#c21d1d'>Invalid email or password.</span>";
                return;
            }


            // Retrieve stored salt and hash from the XML node. Both must be present.
            var saltB64 = userNode["passwordSalt"]?.InnerText ?? "";
            var hashB64 = userNode["passwordHash"]?.InnerText ?? "";
            if (string.IsNullOrEmpty(saltB64) || string.IsNullOrEmpty(hashB64))
            {
                // Account entry is incomplete or corrupted; fail closed with a safe error message.
                FormMessage.Text = "<span style='color:#c21d1d'>This account is misconfigured.</span>";
                return;
            }

            // Decode Base64-encoded salt and hash; catch bad formats to avoid exceptions surfacing to the user.
            byte[] salt, storedHash;
            try
            {
                salt = Convert.FromBase64String(saltB64);
                storedHash = Convert.FromBase64String(hashB64);
            }
            catch
            {
                FormMessage.Text = "<span style='color:#c21d1d'>This account is misconfigured.</span>";
                return;
            }

            // Recompute the PBKDF2 hash using the submitted password and the stored per-user salt.
            var enteredHash = HashPassword(Password.Text, salt);

            // Compare hashes using a constant-time routine to reduce timing side-channel leakage.
            if (!SecureEquals(storedHash, enteredHash))
            {
                // --- AUDIT: log failed password attempt for an existing account ---
                try
                {
                    // userNode is non-null here (email exists in users.xml)
                    var userElement = (XmlElement)userNode;
                    var roleAttr = userElement.GetAttribute("role") ?? "";
                    var uni = userNode["university"]?.InnerText ?? "";
                    var firstName = userNode["firstName"]?.InnerText ?? "";

                    var auditPath = Server.MapPath("~/App_Data/Audit_Log/UnvAdminAudit.xml");

                    // Ensure directory + file exist
                    var auditDir = Path.GetDirectoryName(auditPath);
                    if (!string.IsNullOrEmpty(auditDir) && !Directory.Exists(auditDir))
                    {
                        Directory.CreateDirectory(auditDir);
                    }

                    var auditDoc = new XmlDocument();
                    if (File.Exists(auditPath))
                    {
                        auditDoc.Load(auditPath);
                    }
                    else
                    {
                        auditDoc.LoadXml("<?xml version='1.0' encoding='utf-8'?><auditLog version='1'></auditLog>");
                    }

                    var entry = auditDoc.CreateElement("entry");
                    entry.SetAttribute("id", "log-" + Guid.NewGuid().ToString("N"));
                    entry.SetAttribute("university", uni);
                    entry.SetAttribute("role", string.IsNullOrWhiteSpace(roleAttr) ? "Unknown" : roleAttr);
                    entry.SetAttribute("type", "Sign In Failed (Bad Password)");
                    entry.SetAttribute("timestamp", DateTime.UtcNow.ToString("o"));
                    entry.SetAttribute("email", emailLower);
                    entry.SetAttribute("firstName", firstName);

                    var detailsEl = auditDoc.CreateElement("details");
                    detailsEl.InnerText = "Incorrect password entered for existing account during sign in.";
                    entry.AppendChild(detailsEl);

                    auditDoc.DocumentElement.AppendChild(entry);
                    auditDoc.Save(auditPath);
                }
                catch
                {
                    // Best-effort only; never block login failure flow if audit logging breaks.
                }

                FormMessage.Text = "<span style='color:#c21d1d'>Invalid email or password.</span>";
                return;
            }


            // --- Authentication succeeded ---
            // Extract id and role attributes from the <user> element for session initialization.
            var element = (XmlElement)userNode;
            var id = element.GetAttribute("id");
            var role = element.GetAttribute("role");

            // Minimal session initialization.
            Session["UserId"] = id;
            Session["Role"] = role;
            Session["Email"] = emailLower;

            // Optionally load the user's university (may be empty for some roles).
            Session["University"] = userNode["university"]?.InnerText ?? "";

            // --- AUDIT: log sign-in for all primary roles into University Admin audit log ---
            var normalizedRole = (role ?? string.Empty).Trim();

            // Previously this only logged Participant/Helper; now it also logs UniversityAdmin and SuperAdmin.
            if (normalizedRole.Equals("Participant", StringComparison.OrdinalIgnoreCase) ||
                normalizedRole.Equals("Helper", StringComparison.OrdinalIgnoreCase) ||
                normalizedRole.Equals("UniversityAdmin", StringComparison.OrdinalIgnoreCase) ||
                normalizedRole.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    // This writes an <entry> into UnivAdminAudit.xml with the current user's university, role, etc.
                    UniversityAuditLogger.AppendForCurrentUser(
                        this,
                        "Sign In",
                        $"{normalizedRole} signed in."
                    );
                }
                catch
                {
                    // Audit logging is best-effort; never block login.
                }
            }

            // Role-based routing to post-login landing pages.
            // Default falls back to Participant flow if role is missing or unrecognized.
            switch ((role ?? "").Trim().ToLowerInvariant())
            {
                case "superadmin":
                    Response.Redirect("~/Account/SuperAdmin/SuperAdminHome.aspx");
                    break;
                case "universityadmin":
                    Response.Redirect("~/Account/UniversityAdmin/UniversityAdminHome.aspx");
                    break;
                case "helper":
                    Response.Redirect("~/Account/Helper/Home.aspx");
                    break;
                case "participant":
                default:
                    Response.Redirect("~/Account/Participant/SelectEvent.aspx");
                    break;
            }
        }

        /// <summary>
        /// PBKDF2 password hashing (same parameters as sign-up so verification matches).
        /// Uses 100,000 iterations and returns a 32-byte (256-bit) derived key.
        /// Note: In .NET Framework, Rfc2898DeriveBytes uses HMACSHA1 by default.
        /// </summary>
        private static byte[] HashPassword(string password, byte[] salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000))
            {
                return pbkdf2.GetBytes(32); // 256-bit
            }
        }

        /// <summary>
        /// Constant-time byte array comparison to mitigate timing attacks.
        /// Returns true only if arrays are same length and all bytes match.
        /// </summary>
        private static bool SecureEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}

