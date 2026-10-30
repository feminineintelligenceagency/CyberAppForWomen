using System;
using System.Web;
using System.Configuration;
using System.Diagnostics;
using System.Xml;

namespace CyberApp_FIA.Services
{
    public static class NotificationService
    {
        public const string Certification = "Certification";
        public const string CourseReminder = "CourseReminder";

        public static bool Create(
            HttpContext context,
            string userId,
            string category,
            string title,
            string message,
            string linkUrl,
            string eventKey)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            if (string.IsNullOrWhiteSpace(userId) ||
                string.IsNullOrWhiteSpace(category) ||
                string.IsNullOrWhiteSpace(eventKey))
                throw new ArgumentException(
                    "UserId, category, and eventKey are required.");

            // Respect the user's preference before saving a notification.
            if (!NotificationStore.GetEnabled(context, userId, category))
                return false;

            // Notification links must stay within the application.
            if (!string.IsNullOrEmpty(linkUrl) &&
                (!linkUrl.StartsWith("~/", StringComparison.Ordinal) ||
                 linkUrl.Contains("\\") ||
                 linkUrl.Contains("\r") ||
                 linkUrl.Contains("\n")))
                throw new ArgumentException(
                    "Use an application-relative link starting with ~/.");

            bool created = NotificationStore.Add(context, new NotificationInfo
            {
                UserId = userId,
                Category = category,
                Title = title,
                Message = message,
                LinkUrl = linkUrl,
                EventKey = eventKey
            });

            // Course reminders handle their own email opt-in and summary.
            if (created && category != CourseReminder)
            {
                SendEmail(context, userId, title, message, linkUrl);
            }

            return created;
        }

        private static void SendEmail(
            HttpContext context,
            string userId,
            string title,
            string message,
            string linkUrl)
        {
            try
            {
                // Use a configured website address rather than the request's host.
                Uri baseUri;
                string baseUrl = ConfigurationManager.AppSettings["Notifications:BaseUrl"];

                if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out baseUri) ||
                    baseUri.Scheme != Uri.UriSchemeHttps ||
                    !string.IsNullOrEmpty(baseUri.UserInfo) ||
                    !string.IsNullOrEmpty(baseUri.Query) ||
                    !string.IsNullOrEmpty(baseUri.Fragment))
                {
                    Trace.TraceError("Notification email skipped: invalid Notifications:BaseUrl.");
                    return;
                }

                baseUri = new Uri(baseUri.AbsoluteUri.TrimEnd('/') + "/");
                string body = message ?? string.Empty;

                if (!string.IsNullOrEmpty(linkUrl))
                {
                    var destination = new Uri(baseUri, linkUrl.Substring(2));

                    if (!baseUri.IsBaseOf(destination))
                        throw new ArgumentException("Notification link leaves the application.");

                    body += Environment.NewLine + Environment.NewLine +
                            "View your progress: " + destination.AbsoluteUri;
                }

                var users = new XmlDocument { XmlResolver = null };
                var settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    XmlResolver = null
                };

                using (var reader = XmlReader.Create(
                    context.Server.MapPath("~/App_Data/users.xml"), settings))
                {
                    users.Load(reader);
                }

                foreach (XmlElement user in users.SelectNodes("/users/user"))
                {
                    if (!string.Equals(
                        user.GetAttribute("id"), userId, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    string email = user["email"]?.InnerText?.Trim();

                    if (!EmailSender.TrySend(email, title, body))
                    {
                        Trace.TraceError(
                            "Notification email failed or SMTP is not configured for user {0}.",
                            userId);
                    }

                    return;
                }

                Trace.TraceError("Notification email recipient not found: {0}.", userId);
            }
            catch (Exception ex)
            {
                // Keep the saved notification if email delivery fails.
                Trace.TraceError("Notification email failed: {0}", ex);
            }
        }
    }
}