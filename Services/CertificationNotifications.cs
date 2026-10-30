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

        public static void ModulePublished(
    HttpContext context,
    XmlElement course,
    bool wasPublished)
        {
            bool isPublished = string.Equals(
                course.GetAttribute("status"),
                "Published",
                StringComparison.OrdinalIgnoreCase);

            if (wasPublished || !isPublished)
                return;

            string courseId = course.GetAttribute("id");
            string title = course["title"]?.InnerText;

            if (string.IsNullOrWhiteSpace(courseId))
                return;

            if (string.IsNullOrWhiteSpace(title))
                title = courseId;

            try
            {
                var settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    XmlResolver = null
                };

                var users = new XmlDocument { XmlResolver = null };

                using (var reader = XmlReader.Create(
                    context.Server.MapPath("~/App_Data/users.xml"), settings))
                {
                    users.Load(reader);
                }

                foreach (XmlElement user in users.SelectNodes("/users/user"))
                {
                    if (!string.Equals(
                        user.GetAttribute("role"),
                        "Helper",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string helperId = user.GetAttribute("id");
                    if (string.IsNullOrWhiteSpace(helperId))
                        continue;

                    // One failed notification must not stop delivery to other Helpers.
                    try
                    {
                        NotificationService.Create(
                            context,
                            helperId,
                            NotificationService.Certification,
                            "New certification module available",
                            $"\"{title}\" is now available. Open Certification Progress to view its requirements.",
                            "~/Account/Helper/CertificationProgress.aspx",
                            $"certification:{courseId}:module-published");
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError(
                            "Module notification failed for Helper {0}: {1}",
                            helperId, ex);
                    }
                }
            }
            catch (Exception ex)
            {
                // The course is already saved; report notification failures separately.
                Trace.TraceError("Module publication notifications failed: {0}", ex);
            }
        }

    }
}