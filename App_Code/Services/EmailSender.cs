using System;
using System.Configuration;
using System.Net;
using System.Net.Mail;

namespace CyberApp_FIA.Services
{
    // Sends emails like the account deletion confirmation.
    // Email login info is in Secrets.config (not on GitHub).
    // If Secrets.config is missing, it just won't send the email.
    //
    // Story #10: all emails have to look generic so someone checking
    // the user's inbox can't tell what they're doing on the site.
    public static class EmailSender
    {
        // same subject for every email
        public const string GenericSubject = "You have a new update";

        // shows this name instead of the email address
        public const string SenderName = "Account Notifications";

        // first line of every email so the preview is generic
        private const string GenericOpening = "You have a new update.";

        // sends the email, returns true if it worked
        public static bool TrySend(string toAddress, string body)
        {
            // get the email settings from Secrets.config
            var host = ConfigurationManager.AppSettings["Smtp:Host"];
            var portText = ConfigurationManager.AppSettings["Smtp:Port"];
            var user = ConfigurationManager.AppSettings["Smtp:User"];
            var password = ConfigurationManager.AppSettings["Smtp:Password"];
            var from = ConfigurationManager.AppSettings["Smtp:From"];

            // if settings are missing just skip
            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from) ||
                string.IsNullOrWhiteSpace(toAddress))
            {
                return false;
            }

            // default port is 587
            int port;
            if (!int.TryParse(portText, out port)) port = 587;

            try
            {
                using (var message = new MailMessage())
                using (var client = new SmtpClient(host, port))
                {
                    message.From = new MailAddress(from, SenderName);
                    message.To.Add(toAddress);
                    message.Subject = GenericSubject;
                    message.Body = GenericOpening + Environment.NewLine + Environment.NewLine + (body ?? string.Empty);
                    message.IsBodyHtml = false; // plain text email

                    client.EnableSsl = true; // secure connection
                    client.Credentials = new NetworkCredential(user, password);
                    client.Send(message);
                }
                return true;
            }
            catch (Exception)
            {
                // if email fails don't crash the page
                return false;
            }
        }
    }
}