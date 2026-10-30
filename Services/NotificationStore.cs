using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml;
using System.Xml.Linq;

namespace CyberApp_FIA.Services
{
    public static class NotificationStore
    {
        // Prevent simultaneous requests from overwriting each other's changes.
        private static readonly object StoreLock = new object();

        public static bool Add(HttpContext context, NotificationInfo notification)
        {
            if (notification == null)
                throw new ArgumentNullException("notification");

            if (string.IsNullOrWhiteSpace(notification.UserId) ||
                string.IsNullOrWhiteSpace(notification.Category) ||
                string.IsNullOrWhiteSpace(notification.EventKey))
                throw new ArgumentException(
                    "UserId, Category, and EventKey are required.");

            lock (StoreLock)
            {
                string path = GetPath(context);
                var document = Load(path);

                // Each event creates only one notification per user and category.
                bool exists = document.Root.Elements("notification").Any(n =>
                    (string)n.Attribute("userId") == notification.UserId &&
                    (string)n.Attribute("category") == notification.Category &&
                    (string)n.Attribute("eventKey") == notification.EventKey);

                if (exists) return false;

                notification.Id = Guid.NewGuid().ToString("N");
                notification.CreatedUtc = DateTime.UtcNow;
                notification.IsRead = false;

                document.Root.Add(new XElement("notification",
                    new XAttribute("id", notification.Id),
                    new XAttribute("userId", notification.UserId),
                    new XAttribute("category", notification.Category),
                    new XAttribute("eventKey", notification.EventKey),
                    new XAttribute("isRead", false),
                    new XAttribute("createdUtc", notification.CreatedUtc),
                    new XElement("title", notification.Title ?? ""),
                    new XElement("message", notification.Message ?? ""),
                    new XElement("linkUrl", notification.LinkUrl ?? "")
                ));

                Save(document, path);
                return true;
            }
        }

        public static List<NotificationInfo> GetForUser(
            HttpContext context, string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId is required.");

            lock (StoreLock)
            {
                var document = Load(GetPath(context));

                return document.Root.Elements("notification")
                    .Where(n => (string)n.Attribute("userId") == userId)
                    .Select(n => new NotificationInfo
                    {
                        Id = (string)n.Attribute("id"),
                        UserId = (string)n.Attribute("userId"),
                        Category = (string)n.Attribute("category"),
                        EventKey = (string)n.Attribute("eventKey"),
                        IsRead = (bool)n.Attribute("isRead"),
                        CreatedUtc = (DateTime)n.Attribute("createdUtc"),
                        Title = (string)n.Element("title"),
                        Message = (string)n.Element("message"),
                        LinkUrl = (string)n.Element("linkUrl")
                    })
                    .OrderByDescending(n => n.CreatedUtc)
                    .ToList();
            }
        }

        public static bool MarkAsRead(
            HttpContext context, string userId, string notificationId)
        {
            if (string.IsNullOrWhiteSpace(userId) ||
                string.IsNullOrWhiteSpace(notificationId))
                return false;

            lock (StoreLock)
            {
                string path = GetPath(context);
                var document = Load(path);

                // Only update notifications belonging to this user.
                var notification = document.Root.Elements("notification")
                    .FirstOrDefault(n =>
                        (string)n.Attribute("userId") == userId &&
                        (string)n.Attribute("id") == notificationId);

                if (notification == null) return false;

                notification.SetAttributeValue("isRead", true);
                Save(document, path);
                return true;
            }
        }

        public static bool GetEnabled(
            HttpContext context, string userId, string category)
        {
            if (string.IsNullOrWhiteSpace(userId) ||
                string.IsNullOrWhiteSpace(category))
                throw new ArgumentException("UserId and category are required.");

            lock (StoreLock)
            {
                var document = Load(GetPath(context));

                var preference = document.Root.Elements("preference")
                    .FirstOrDefault(p =>
                        (string)p.Attribute("userId") == userId &&
                        (string)p.Attribute("category") == category);

                // Notifications are enabled until the user turns them off.
                return preference == null ||
                    (bool)preference.Attribute("enabled");
            }
        }

        public static void SetEnabled(
            HttpContext context, string userId, string category, bool enabled)
        {
            if (string.IsNullOrWhiteSpace(userId) ||
                string.IsNullOrWhiteSpace(category))
                throw new ArgumentException("UserId and category are required.");

            lock (StoreLock)
            {
                string path = GetPath(context);
                var document = Load(path);

                var preference = document.Root.Elements("preference")
                    .FirstOrDefault(p =>
                        (string)p.Attribute("userId") == userId &&
                        (string)p.Attribute("category") == category);

                if (preference == null)
                {
                    preference = new XElement("preference",
                        new XAttribute("userId", userId),
                        new XAttribute("category", category));

                    document.Root.Add(preference);
                }

                preference.SetAttributeValue("enabled", enabled);
                Save(document, path);
            }
        }

        private static string GetPath(HttpContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            return context.Server.MapPath("~/App_Data/notifications.xml");
        }

        private static XDocument Load(string path)
        {
            if (!File.Exists(path))
                return new XDocument(new XElement("notifications"));

            // Block external XML content when reading the file.
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            };

            using (var reader = XmlReader.Create(path, settings))
            {
                var document = XDocument.Load(reader);

                if (document.Root == null ||
                    document.Root.Name != "notifications")
                    throw new InvalidDataException(
                        "Invalid notification storage file.");

                return document;
            }
        }

        private static void Save(XDocument document, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            string temporaryPath = path + "." +
                Guid.NewGuid().ToString("N") + ".tmp";

            try
            {
                // Finish writing before replacing the existing file.
                document.Save(temporaryPath);

                if (File.Exists(path))
                    File.Replace(temporaryPath, path, null);
                else
                    File.Move(temporaryPath, path);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }
    }
}