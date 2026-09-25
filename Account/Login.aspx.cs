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

            var element = (XmlElement)userNode;
            bool needsRehash;
            // Verify the password using the passHasher utility. If verification fails, log the failed attempt and show a generic error message.
            if (!passHasher.VerifyUser(element, Password.Text, out needsRehash))
            {
                var roleAttr = element.GetAttribute("role");
                WriteFailedSignInAudit(
                    email: emailLower,
                    role: string.IsNullOrWhiteSpace(roleAttr) ? "Unknown" : roleAttr,
                    university: userNode["university"]?.InnerText ?? "",
                    firstName: userNode["firstName"]?.InnerText ?? "",
                    type: "Sign In Failed (Bad Password)",
                    details: "Incorrect password (or missing/corrupted hash data) for existing account during sign in.");

                ShowInvalidCredentials();
                return;
            }

            if (needsRehash)
            {
                UpgradePasswordHash(emailLower, Password.Text);
            }

            // --- Authentication succeeded ---
            // Extract id and role attributes from the <user> element for session initialization.
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
        /// Appends a failed sign-in entry to the University Admin audit log.
        /// </summary>
        private void WriteFailedSignInAudit(
            string email,
            string role,
            string university,
            string firstName,
            string type,
            string details)
        {
            try
            {
                var auditDir = Path.GetDirectoryName(Server.MapPath("~/App_Data/Audit_Log/UnvAdminAudit.xml"));
                if (!string.IsNullOrEmpty(auditDir) && !Directory.Exists(auditDir))
                {
                    Directory.CreateDirectory(auditDir);
                }

                var auditDoc = new XmlDocument();
                if (File.Exists(Server.MapPath("~/App_Data/Audit_Log/UnvAdminAudit.xml")))
                {
                    auditDoc.Load(Server.MapPath("~/App_Data/Audit_Log/UnvAdminAudit.xml"));
                }
                else
                {
                    auditDoc.LoadXml("<?xml version='1.0' encoding='utf-8'?><auditLog version='1'></auditLog>");
                }

                var entry = auditDoc.CreateElement("entry");
                entry.SetAttribute("id", "log-" + Guid.NewGuid().ToString("N"));
                entry.SetAttribute("university", university ?? "");
                entry.SetAttribute("role", role ?? "Unknown");
                entry.SetAttribute("type", type);
                entry.SetAttribute("timestamp", DateTime.UtcNow.ToString("o"));
                entry.SetAttribute("email", email ?? "");
                entry.SetAttribute("firstName", firstName ?? "");

                var detailsEl = auditDoc.CreateElement("details");
                detailsEl.InnerText = details;
                entry.AppendChild(detailsEl);

                auditDoc.DocumentElement.AppendChild(entry);
                auditDoc.Save(Server.MapPath("~/App_Data/Audit_Log/UnvAdminAudit.xml"));
            }
            catch
            {
                // Best-effort only.
            }
        }

        /// <summary>
        /// Re-hashes the user's password with current Argon2id settings and saves it.
        /// Reloads users.xml fresh right before saving so we don't overwrite changes other
        /// requests made since this request first loaded the file.
        /// </summary>
        private void UpgradePasswordHash(string emailLower, string password)
        {
            try
            {
                var doc = new XmlDocument();
                doc.Load(XmlPath);

                var userNode = doc.SelectSingleNode($"/users/user[translate(email,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='{emailLower}']");
                var userEl = userNode as XmlElement;   // null if not found
                if (userEl == null) return;

                passHasher.SetPassword(userEl, password);
                doc.Save(XmlPath);
            }
            catch
            {
                // Swallow: user is already authenticated; upgrade retries on next login.
            }
        }

        /// <summary>
        /// Generic failure message: never reveal whether the email or the password was wrong.
        /// </summary>
        private void ShowInvalidCredentials()
        {
            FormMessage.Text = "<span style='color:#c21d1d'>Invalid email or password.</span>";
        }
    }
}

