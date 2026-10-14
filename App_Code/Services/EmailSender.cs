using System;
using System.Configuration;
using System.Net;
using System.Net.Mail;

namespace CyberApp_FIA.Services
{
    
    /// Sprint 2: sends plain-text emails (e.g. the account deletion confirmation).
    ///
    /// The email account's settings live in "Secrets.config" (next to Web.config), which is
    /// NOT committed to GitHub (see .gitignore). If those settings are missing, emails
    /// are simply skipped instead of crashing the page.
    
    public static class EmailSender
    {
        
        /// Sends an email. Returns true if it was sent, false if email isn't set up or sending failed.
        
        public static bool TrySend(string toAddress, string subject, string body)
        {
            // Read the email account settings (from Secrets.config, merged into appSettings).
            var host = ConfigurationManager.AppSettings["Smtp:Host"];
            var portText = ConfigurationManager.AppSettings["Smtp:Port"];
            var user = ConfigurationManager.AppSettings["Smtp:User"];
            var password = ConfigurationManager.AppSettings["Smtp:Password"];
            var from = ConfigurationManager.AppSettings["Smtp:From"];

            // Email not configured on this machine -> skip quietly.
            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from) ||
                string.IsNullOrWhiteSpace(toAddress))
            {
                return false;
            }

            int port;
            if (!int.TryParse(portText, out port)) port = 587;   // 587 = standard secure email port

            try
            {
                using (var message = new MailMessage(from, toAddress, subject, body))
                using (var client = new SmtpClient(host, port))
                {
                    message.IsBodyHtml = false;          // plain text: nothing hidden, nothing flashy
                    client.EnableSsl = true;             // encrypt the connection to the mail server
                    client.Credentials = new NetworkCredential(user, password);
                    client.Send(message);
                }
                return true;
            }
            catch (Exception)
            {
                // Never let an email problem stop the main action (e.g. the deletion).
                return false;
            }
        }
    }
}