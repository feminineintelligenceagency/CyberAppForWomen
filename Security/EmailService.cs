using System.IO;
using System.Net.Mail;

namespace CyberApp_FIA
{
    /// <summary>
    /// Sends email using the settings in Web.config (system.net/mailSettings).
    /// Locally, Web.config saves each email as a .eml file in a folder instead of sending it.
    /// When we move to a real mail server, only Web.config changes, not this code.
    /// </summary>
    public static class EmailService
    {
        public static void Send(string to, string subject, string body)
        {
            using (var msg = new MailMessage())
            {
                msg.To.Add(to);
                msg.Subject = subject;
                msg.Body = body;
                msg.IsBodyHtml = false;

                using (var smtp = new SmtpClient())
                {
                    // Make sure the local pickup folder exists so sending doesn't fail
                    if (smtp.DeliveryMethod == SmtpDeliveryMethod.SpecifiedPickupDirectory &&
                        !string.IsNullOrEmpty(smtp.PickupDirectoryLocation) &&
                        !Directory.Exists(smtp.PickupDirectoryLocation))
                    {
                        Directory.CreateDirectory(smtp.PickupDirectoryLocation);
                    }

                    smtp.Send(msg);
                }
            }
        }
    }
}