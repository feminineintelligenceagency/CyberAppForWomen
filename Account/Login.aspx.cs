using System;
using System.IO;
using System.Security.Cryptography;
using System.Web.UI;
using System.Xml;
using CyberApp_FIA.Services;

namespace CyberApp_FIA.Account
{
    public partial class Login : Page
    {
        private static readonly object UsersFileLock = new object();
        private const string GenericLoginError = "<span style='color:#c21d1d'>Invalid email or password.</span>";
        private static readonly string LockoutError = "<span style='color:#c21d1d'>You have been locked out for "
            + ((int)LoginRateLimiting.LockoutDuration.TotalMinutes) + " minutes.</span>";
        private string XmlPath => Server.MapPath("~/App_Data/users.xml");

        protected void BtnLogin_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid || !File.Exists(XmlPath)) { FormMessage.Text = GenericLoginError; return; }
            var emailLower = (Email.Text ?? "").Trim().ToLowerInvariant();
            lock (UsersFileLock)
            {
                var doc = new XmlDocument(); doc.Load(XmlPath);
                var userNode = doc.SelectSingleNode($"/users/user[translate(email,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='{emailLower}']");
                if (userNode != null && LoginRateLimiting.IsLockedOut((XmlElement)userNode, DateTime.UtcNow)) { FormMessage.Text = LockoutError; return; }
                if (userNode == null) { FormMessage.Text = GenericLoginError; return; }
                var user = (XmlElement)userNode;
                byte[] salt, storedHash;
                try { salt = Convert.FromBase64String(user["passwordSalt"]?.InnerText ?? ""); storedHash = Convert.FromBase64String(user["passwordHash"]?.InnerText ?? ""); }
                catch { FormMessage.Text = GenericLoginError; return; }
                if (!SecureEquals(storedHash, HashPassword(Password.Text, salt)))
                {
                    LoginRateLimiting.RecordFailure(doc, user, DateTime.UtcNow); doc.Save(XmlPath);
                    FormMessage.Text = LoginRateLimiting.IsLockedOut(user, DateTime.UtcNow) ? LockoutError : GenericLoginError;
                    return;
                }
                LoginRateLimiting.RecordSuccess(doc, user); doc.Save(XmlPath);
                Session["UserId"] = user.GetAttribute("id"); Session["Role"] = user.GetAttribute("role"); Session["Email"] = emailLower; Session["University"] = user["university"]?.InnerText ?? "";
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

        private static byte[] HashPassword(string password, byte[] salt) { using (var p = new Rfc2898DeriveBytes(password, salt, 100000)) return p.GetBytes(32); }
        private static bool SecureEquals(byte[] a, byte[] b) { if (a == null || b == null || a.Length != b.Length) return false; int d = 0; for (int i = 0; i < a.Length; i++) d |= a[i] ^ b[i]; return d == 0; }
    }
}
