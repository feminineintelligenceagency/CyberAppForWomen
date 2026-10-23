using System;
using System.Diagnostics;
using System.Web;
using System.Xml;

namespace CyberApp_FIA.Services
{
    public static class CertificationNotifications
    {
        public static void SessionRequirementCompleted(
            HttpContext context,
            string helperId,
            XmlElement course,
            string countField,
            int previousCount)
        {
            string requirementField;
            string stepName;

            if (countField == "teachingSessions")
            {
                requirementField = "minSessionsTaught";
                stepName = "teaching sessions";
            }
            else if (countField == "helpSessions")
            {
                requirementField = "minHelpSessions";
                stepName = "one-on-one help sessions";
            }
            else
            {
                throw new ArgumentException("Unknown session type.", "countField");
            }

            int required;
            int current;

            if (!int.TryParse(course[requirementField]?.InnerText, out required) ||
                !int.TryParse(course[countField]?.InnerText, out current) ||
                required <= 0 || previousCount >= required || current < required)
            {
                return;
            }

            string courseId = course.GetAttribute("id");
            string title = course["title"]?.InnerText;

            if (string.IsNullOrWhiteSpace(title))
                title = courseId;

            // Notification failure must not undo saved progress.
            try
            {
                NotificationService.Create(
                    context,
                    helperId,
                    NotificationService.Certification,
                    "Certification step completed",
                    $"You reached the required {required} {stepName} for \"{title}\". Session credit remains subject to review.",
                    "~/Account/Helper/CertificationProgress.aspx",
                    $"certification:{courseId}:{countField}:required:{required}");
            }
            catch (Exception ex)
            {
                Trace.TraceError(
                    "Certification notification failed: {0}", ex);
            }
        }
    }
}