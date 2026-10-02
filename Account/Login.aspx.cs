using CyberApp_FIA.Services;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Web.UI;
using System.Xml;
using System.Xml.Linq;
using System.Globalization;   // Epic #7 (Piece 3): needed to read the saved lockout time

namespace CyberApp_FIA.Account
{
    public partial class Login : Page
    {
        private static readonly object UsersFileLock = new object();
        private const string GenericLoginError = "<span style='color:#c21d1d'>Invalid email or password.</span>";
        private static readonly string LockoutError = "<span style='color:#c21d1d'>You have been locked out for "
            + ((int)LoginRateLimiting.LockoutDuration.TotalMinutes) + " minutes.</span>";
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
            if (!Page.IsValid || !File.Exists(XmlPath)) { FormMessage.Text = GenericLoginError; return; }
            var emailLower = (Email.Text ?? "").Trim().ToLowerInvariant();
            var password = Password.Text ?? "";
            lock (UsersFileLock)
            {
                var doc = new XmlDocument(); doc.Load(XmlPath);
                var userNode = doc.SelectSingleNode($"/users/user[translate(email,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='{emailLower}']");
                // Epic 1 - User Story #6 (dmundra-29): block sign-ins when the account is already locked out due to repeated failures.
                if (userNode != null && LoginRateLimiting.IsLockedOut((XmlElement)userNode, DateTime.UtcNow)) { FormMessage.Text = LockoutError; return; }
                if (userNode == null) { passHasher.dummyVerify(password); FormMessage.Text = GenericLoginError; return; }
                var user = (XmlElement)userNode;
                
                var now = DateTime.UtcNow;

                if (LoginRateLimiting.IsLockedOut(user, now)) {FormMessage.Text = LockoutError; return;}

                bool needsRehash;
                // Verify the password using the passHasher utility. If verification fails, log the failed attempt and show a generic error message.
                if (!passHasher.VerifyUser(user, Password.Text, out needsRehash))
                {
                    // Epic 1 - User Story #6 (dmundra-29): increment failed-login count and lock the account after repeated invalid attempts.
                    LoginRateLimiting.RecordFailure(doc, user, now);
                    doc.Save(XmlPath);

                    var roleAttr = user.GetAttribute("role");
                    WriteFailedSignInAudit(
                        email: emailLower,
                        role: string.IsNullOrWhiteSpace(roleAttr) ? "Unknown" : roleAttr,
                        university: userNode["university"]?.InnerText ?? "",
                        firstName: userNode["firstName"]?.InnerText ?? "",
                        type: "Sign In Failed (Bad Password)",
                        details: "Incorrect password (or missing/corrupted hash data) for existing account during sign in.");

                    FormMessage.Text = LoginRateLimiting.IsLockedOut(user, now) ? LockoutError : GenericLoginError;
                    return;
                }

                if (needsRehash)
                {
                    try
                    {
                        passHasher.SetPassword(user, Password.Text);
                    }
                    catch 
                    {
                        // If rehashing fails, we can log the error but still allow the user to log in.
                    }

                }

                LoginRateLimiting.RecordSuccess(doc, user); doc.Save(XmlPath);
                Session["UserId"] = user.GetAttribute("id"); Session["Role"] = user.GetAttribute("role"); Session["Email"] = emailLower; Session["University"] = user["university"]?.InnerText ?? "";
                AuthSession.SignIn(Context);
                try { UniversityAuditLogger.AppendForCurrentUser(this, "Sign In", $"{user.GetAttribute("role")} signed in."); } catch { }
                switch ((user.GetAttribute("role") ?? "").Trim().ToLowerInvariant())
                {
                    case "superadmin": Response.Redirect("~/Account/SuperAdmin/SuperAdminHome.aspx"); break;
                    case "universityadmin": Response.Redirect("~/Account/UniversityAdmin/UniversityAdminHome.aspx"); break;
                    case "helper": Response.Redirect("~/Account/Helper/Home.aspx"); break;
                    default: Response.Redirect("~/Account/Participant/SelectEvent.aspx"); break;
                }
            }
        }


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

    }
}
