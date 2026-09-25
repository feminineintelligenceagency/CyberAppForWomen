using System;
using System.IO;
using System.Text;
using System.Web.UI;
using System.Xml;
using System.Globalization;   // Epic #7 (Piece 3): needed to read the saved lockout time
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

        // Epic #7 (Piece 3): lock an account after this many wrong passwords in a row...
        private const int MaxFailedAttempts = 5;

        // ...for this long.
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

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

            // Epic #7: find the user by looping and comparing emails as plain text, instead of
            // building an XPath query from user input (prevents XPath injection).
            XmlNode userNode = null;
            foreach (XmlNode node in doc.SelectNodes("/users/user"))
            {
                var storedEmail = (node["email"]?.InnerText ?? "").Trim().ToLowerInvariant();
                if (storedEmail == emailLower)
                {
                    userNode = node;
                    break;
                }
            }

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

            // --- Epic #7 (Piece 3): is this account currently locked? ---
            // If so, stop here WITHOUT checking the password, so guessing can't continue
            // during the lockout (even a correct guess is rejected until it expires).
            var lockoutText = GetChildText(userNode, "lockoutUntil");
            if (DateTime.TryParse(lockoutText, null, DateTimeStyles.RoundtripKind, out var lockoutUntil)
                && lockoutUntil > DateTime.UtcNow)
            {
                var minutesLeft = (int)Math.Ceiling((lockoutUntil - DateTime.UtcNow).TotalMinutes);
                FormMessage.Text = $"<span style='color:#c21d1d'>Too many failed sign-in attempts. Please try again in {minutesLeft} minute(s).</span>";
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

                // --- Epic #7 (Piece 3): count this failure; lock the account after too many ---
                int.TryParse(GetChildText(userNode, "failedAttempts"), out var failedAttempts);
                failedAttempts++;

                bool nowLocked = failedAttempts >= MaxFailedAttempts;
                if (nowLocked)
                {
                    // Lock until 15 minutes from now, and reset the counter for after the lock ends.
                    SetChildText(doc, userNode, "lockoutUntil", DateTime.UtcNow.Add(LockoutDuration).ToString("o"));
                    SetChildText(doc, userNode, "failedAttempts", "0");
                }
                else
                {
                    SetChildText(doc, userNode, "failedAttempts", failedAttempts.ToString());
                }
                doc.Save(XmlPath);


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
                    entry.SetAttribute("type", nowLocked ? "Account Locked (Too Many Failed Sign-Ins)" : "Sign In Failed (Bad Password)");
                    entry.SetAttribute("timestamp", DateTime.UtcNow.ToString("o"));
                    entry.SetAttribute("email", emailLower);
                    entry.SetAttribute("firstName", firstName);

                    var detailsEl = auditDoc.CreateElement("details");
                    detailsEl.InnerText = nowLocked
                        ? $"Account locked for {LockoutDuration.TotalMinutes} minutes after {MaxFailedAttempts} incorrect passwords."
                        : "Incorrect password entered for existing account during sign in.";
                    entry.AppendChild(detailsEl);

                    auditDoc.DocumentElement.AppendChild(entry);
                    auditDoc.Save(auditPath);
                }
                catch
                {
                    // Best-effort only; never block login failure flow if audit logging breaks.
                }

                FormMessage.Text = nowLocked
                    ? $"<span style='color:#c21d1d'>Too many failed sign-in attempts. Please try again in {LockoutDuration.TotalMinutes} minutes.</span>"
                    : "<span style='color:#c21d1d'>Invalid email or password.</span>";
                return;
            }

            // --- Epic #7 (Piece 3): correct password, so clear any failed-attempt history ---
            if (userNode["failedAttempts"] != null || userNode["lockoutUntil"] != null)
            {
                RemoveChild(userNode, "failedAttempts");
                RemoveChild(userNode, "lockoutUntil");
                doc.Save(XmlPath);
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
            AuthSession.SignIn(Context);

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


        // ===================== Epic #7: small XML helpers =====================

        /// <summary>
        /// Reads a child element's text, or "" if it doesn't exist.</summary>
        private static string GetChildText(XmlNode node, string name)
        {
            return node[name]?.InnerText ?? "";
        }

        /// <summary>
        /// Sets a child element's text, creating the element if it doesn't exist yet.</summary>
        private static void SetChildText(XmlDocument doc, XmlNode node, string name, string value)
        {
            var child = node[name];
            if (child == null)
            {
                child = doc.CreateElement(name);
                node.AppendChild(child);
            }
            child.InnerText = value;
        }

        /// <summary>
        /// Removes a child element if it exists.</summary>
        private static void RemoveChild(XmlNode node, string name)
        {
            var child = node[name];
            if (child != null) node.RemoveChild(child);
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

