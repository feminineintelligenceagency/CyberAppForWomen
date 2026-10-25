using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml;

namespace CyberApp_FIA.Services
{
    /// <summary>
    /// Sprint 3 (#21, April Lee): course reminders for unfinished courses.
    ///
    /// Built on the shared notification system:
    /// - Reminders are saved with NotificationService.Create (CourseReminder category).
    /// - "Off" uses the shared on/off preference in NotificationStore.
    /// - Duplicates are prevented by the event key, which includes the course and the
    ///   current day or week. So a user gets at most one reminder per course per day (Daily)
    ///   or per week (Weekly), no matter how many times she opens the site.
    ///
    /// The frequency (Daily/Weekly) and the email opt-in are saved on the user's record in
    /// users.xml, so they are removed automatically when the account is deleted.
    /// </summary>
    public static class ReminderService
    {
        public const string Daily = "Daily";
        public const string Weekly = "Weekly";
        public const string Off = "Off";

        // New accounts get weekly in-site reminders, and no emails until they opt in.
        private const string DefaultFrequency = Weekly;

        private const string FrequencyElement = "reminderFrequency";
        private const string EmailElement = "reminderEmail";

        private static readonly object UsersLock = new object();

        public class ReminderSettings
        {
            public string Frequency { get; set; }
            public bool EmailEnabled { get; set; }
        }

        // ===================== Settings =====================

        /// <summary>Reads the user's reminder settings (frequency and email opt-in).</summary>
        public static ReminderSettings GetSettings(HttpContext context, string userId)
        {
            var settings = new ReminderSettings { Frequency = DefaultFrequency, EmailEnabled = false };
            if (string.IsNullOrWhiteSpace(userId)) return settings;

            // "Off" lives in the shared notification preference.
            if (!NotificationStore.GetEnabled(context, userId, NotificationService.CourseReminder))
            {
                settings.Frequency = Off;
            }

            var user = FindUser(LoadXml(UsersPath(context)), userId);
            if (user != null)
            {
                var freq = (user[FrequencyElement]?.InnerText ?? "").Trim();
                if (settings.Frequency != Off && (freq == Daily || freq == Weekly))
                    settings.Frequency = freq;

                settings.EmailEnabled = string.Equals(
                    (user[EmailElement]?.InnerText ?? "").Trim(), "true", StringComparison.OrdinalIgnoreCase);
            }

            return settings;
        }

        /// <summary>Saves the user's reminder settings from the Settings page.</summary>
        public static void SaveSettings(HttpContext context, string userId, string frequency, bool emailEnabled)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId is required.", nameof(userId));

            // Only accept the three known values (never trust what the browser sends).
            if (frequency != Daily && frequency != Weekly && frequency != Off)
                frequency = DefaultFrequency;

            // Turn the whole reminder category on or off in the shared store.
            NotificationStore.SetEnabled(context, userId, NotificationService.CourseReminder, frequency != Off);

            lock (UsersLock)
            {
                var path = UsersPath(context);
                if (!File.Exists(path)) return;

                var doc = new XmlDocument { PreserveWhitespace = true };
                doc.Load(path);

                var user = FindUser(doc, userId);
                if (user == null) return;

                // Keep the last Daily/Weekly choice even when turned off, so turning
                // reminders back on remembers how often she wanted them.
                if (frequency != Off)
                    SetChild(doc, user, FrequencyElement, frequency);

                SetChild(doc, user, EmailElement, emailEnabled ? "true" : "false");
                doc.Save(path);
            }
        }

        // ===================== Creating reminders =====================

        /// <summary>
        /// Creates reminders for the user's unfinished courses, if one is due.
        /// Called when the participant opens Home. Returns how many new reminders were created.
        /// Never throws: a reminder problem must not break the page.
        /// </summary>
        public static int CheckForUser(HttpContext context, string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId)) return 0;

                var settings = GetSettings(context, userId);
                if (settings.Frequency == Off) return 0;

                var unfinished = GetUnfinishedCourses(context, userId);
                if (unfinished.Count == 0) return 0;

                var period = CurrentPeriodKey(settings.Frequency);
                int created = 0;

                foreach (var course in unfinished)
                {
                    bool added = NotificationService.Create(
                        context,
                        userId,
                        NotificationService.CourseReminder,
                        "Course reminder",
                        "You still have \"" + course.Value + "\" to finish.",
                        "~/Account/Participant/Home.aspx",
                        "course-reminder:" + course.Key + ":" + period);

                    if (added) created++;
                }

                // One discreet email per reminder round, only if she opted in.
                if (created > 0 && settings.EmailEnabled)
                {
                    SendReminderEmail(context, userId);
                }

                return created;
            }
            catch (Exception ex)
            {
                Trace.TraceError("Course reminder check failed: {0}", ex);
                return 0;
            }
        }

        /// <summary>
        /// Unfinished = the user is enrolled in a session for a course, and that course is not
        /// in her completions. Returns courseId -> course title.
        /// </summary>
        public static Dictionary<string, string> GetUnfinishedCourses(HttpContext context, string userId)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var server = context.Server;

            var enrollments = LoadXml(server.MapPath("~/App_Data/enrollments.xml"));
            var sessions = LoadXml(server.MapPath("~/App_Data/eventSessions.xml"));
            if (enrollments?.DocumentElement == null || sessions?.DocumentElement == null) return result;

            // 1. Which sessions is she enrolled in? (compare ids as plain text, not inside XPath)
            var enrolledSessions = new List<Tuple<string, string>>(); // (eventId, sessionId)
            foreach (XmlElement session in enrollments.DocumentElement.SelectNodes("session"))
            {
                bool isEnrolled = false;
                foreach (XmlElement u in session.SelectNodes("enrolled/user"))
                {
                    var id = u.GetAttribute("id");
                    if (string.IsNullOrEmpty(id)) id = u.GetAttribute("userId");
                    if (string.Equals(id, userId, StringComparison.OrdinalIgnoreCase)) { isEnrolled = true; break; }
                }

                if (isEnrolled)
                    enrolledSessions.Add(Tuple.Create(session.GetAttribute("eventId"), session.GetAttribute("id")));
            }
            if (enrolledSessions.Count == 0) return result;

            // 2. Which course is each of those sessions for?
            var courseIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (XmlElement s in sessions.DocumentElement.SelectNodes("session"))
            {
                var key = Tuple.Create(s.GetAttribute("eventId"), s.GetAttribute("id"));
                if (!enrolledSessions.Any(e => string.Equals(e.Item1, key.Item1, StringComparison.OrdinalIgnoreCase) &&
                                               string.Equals(e.Item2, key.Item2, StringComparison.OrdinalIgnoreCase)))
                    continue;

                var courseId = (s["courseId"]?.InnerText ?? "").Trim();
                if (courseId.Length > 0) courseIds.Add(courseId);
            }

            // 3. Remove the ones she already completed.
            var completions = LoadXml(server.MapPath("~/App_Data/completions.xml"));
            if (completions?.DocumentElement != null)
            {
                foreach (XmlElement u in completions.DocumentElement.SelectNodes("user"))
                {
                    if (!string.Equals(u.GetAttribute("id"), userId, StringComparison.OrdinalIgnoreCase)) continue;
                    foreach (XmlElement c in u.SelectNodes("course"))
                        courseIds.Remove(c.GetAttribute("id"));
                }
            }

            // 4. Look up course titles.
            var titles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var micro = LoadXml(server.MapPath("~/App_Data/microcourses.xml"));
            if (micro?.DocumentElement != null)
            {
                foreach (XmlElement c in micro.DocumentElement.SelectNodes("course"))
                    titles[c.GetAttribute("id")] = (c["title"]?.InnerText ?? "").Trim();
            }

            foreach (var id in courseIds)
            {
                string title;
                result[id] = titles.TryGetValue(id, out title) && title.Length > 0 ? title : "a course";
            }

            return result;
        }

        // ===================== Helpers =====================

        /// <summary>
        /// The part of the event key that changes each day (Daily) or each week (Weekly).
        /// Weekly uses the date of that week's Monday.
        /// </summary>
        private static string CurrentPeriodKey(string frequency)
        {
            var today = DateTime.UtcNow.Date;

            if (frequency == Daily)
                return "day-" + today.ToString("yyyy-MM-dd");

            int daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
            return "week-" + today.AddDays(-daysSinceMonday).ToString("yyyy-MM-dd");
        }

        /// <summary>
        /// Generic email: the subject and body never mention FIA, courses, or the site's topic,
        /// in case someone else can see her inbox.
        /// </summary>
        private static void SendReminderEmail(HttpContext context, string userId)
        {
            var user = FindUser(LoadXml(UsersPath(context)), userId);
            var email = user?["email"]?.InnerText?.Trim();
            if (string.IsNullOrWhiteSpace(email)) return;

            EmailSender.TrySend(
                email,
                "You have an update",
                "Hello,\r\n\r\n" +
                "You have a new update waiting for you. Sign in to your account to see it.\r\n\r\n" +
                "You can change or turn off these emails in your account settings.\r\n");
        }

        private static string UsersPath(HttpContext context) => context.Server.MapPath("~/App_Data/users.xml");

        private static XmlDocument LoadXml(string path)
        {
            if (!File.Exists(path)) return null;
            var doc = new XmlDocument { XmlResolver = null };
            doc.Load(path);
            return doc;
        }

        private static XmlElement FindUser(XmlDocument doc, string userId)
        {
            if (doc?.DocumentElement == null) return null;
            foreach (XmlElement u in doc.DocumentElement.SelectNodes("user"))
            {
                if (string.Equals(u.GetAttribute("id"), userId, StringComparison.OrdinalIgnoreCase))
                    return u;
            }
            return null;
        }

        private static void SetChild(XmlDocument doc, XmlElement parent, string name, string value)
        {
            var child = parent[name];
            if (child == null)
            {
                child = doc.CreateElement(name);
                parent.AppendChild(child);
            }
            child.InnerText = value;
        }
    }
}