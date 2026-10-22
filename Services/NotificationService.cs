using System;
using System.Web;

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

            return NotificationStore.Add(context, new NotificationInfo
            {
                UserId = userId,
                Category = category,
                Title = title,
                Message = message,
                LinkUrl = linkUrl,
                EventKey = eventKey
            });
        }
    }
}