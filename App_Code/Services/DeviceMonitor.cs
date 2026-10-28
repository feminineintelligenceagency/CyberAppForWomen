using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Xml;

namespace CyberApp_FIA.Services
{
    /// <summary>Details of a sign-in from a browser/device the account has not been seen on before.</summary>
    public sealed class UnrecognizedLoginAlert
    {
        internal UnrecognizedLoginAlert(string alertId, string userId, string sessionId, string deviceType, DateTime activatedAtUtc)
        {
            AlertId = alertId;
            UserId = userId;
            SessionId = sessionId;
            DeviceType = deviceType;
            ActivatedAtUtc = activatedAtUtc;
        }

        public string AlertId { get; private set; }
        public string UserId { get; private set; }
        public string SessionId { get; private set; }
        public string DeviceType { get; private set; }
        /// <summary>UTC time the new authentication session was activated.</summary>
        public DateTime ActivatedAtUtc { get; private set; }
    }

    /// <summary>
    /// Recognizes returning browsers with a long-lived device cookie and records a login alert
    /// (device type + session activation time) when an account signs in from an unknown one.
    /// </summary>
    public static class DeviceMonitor
    {
        public const string DeviceCookieName = "DeviceToken";
        private const string KnownRoot = "knownDevices";
        private const string AlertRoot = "loginAlerts";
        private static readonly object StoreLock = new object();

        /// <summary>
        /// Checks the browser's device cookie against the user's known devices. A device is registered the
        /// first time it is seen; an alert is logged (and returned) when the user already had other known
        /// devices. The user's very first device is never flagged. Returns null for a recognized device.
        /// </summary>
        public static UnrecognizedLoginAlert CheckSignIn(
            HttpContext context,
            string userId,
            string sessionId,
            string deviceType,
            DateTime activatedAtUtc)
        {
            if (context == null) throw new ArgumentNullException("context");
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("A user ID is required.", "userId");

            var token = context.Request.Cookies[DeviceCookieName]?.Value;
            var isNewCookie = string.IsNullOrEmpty(token);
            if (isNewCookie) token = NewToken();
            var tokenHash = Hash(token);

            UnrecognizedLoginAlert alert = null;
            lock (StoreLock)
            {
                var knownPath = context.Server.MapPath("~/App_Data/knownDevices.xml");
                var known = Load(knownPath, KnownRoot);

                XmlElement match = null;
                var userHasDevices = false;
                foreach (XmlElement device in known.SelectNodes("/" + KnownRoot + "/device"))
                {
                    if (!string.Equals(device.GetAttribute("userId"), userId, StringComparison.Ordinal)) continue;
                    userHasDevices = true;
                    if (string.Equals(device.GetAttribute("tokenHash"), tokenHash, StringComparison.Ordinal))
                        match = device;
                }

                if (match != null)
                {
                    match.SetAttribute("lastSeenUtc", FormatUtc(activatedAtUtc));
                }
                else
                {
                    var device = known.CreateElement("device");
                    device.SetAttribute("userId", userId);
                    device.SetAttribute("tokenHash", tokenHash);
                    device.SetAttribute("deviceType", deviceType);
                    device.SetAttribute("firstSeenUtc", FormatUtc(activatedAtUtc));
                    device.SetAttribute("lastSeenUtc", FormatUtc(activatedAtUtc));
                    known.DocumentElement.AppendChild(device);

                    if (userHasDevices)
                        alert = LogAlert(context, userId, sessionId, deviceType, activatedAtUtc);
                }

                Save(known, knownPath);
            }

            // Only the hash is stored server-side; the raw token lives in this HttpOnly cookie for a year.
            context.Response.Cookies.Add(new HttpCookie(DeviceCookieName, token)
            {
                HttpOnly = true,
                Secure = true,
                Path = "/",
                Expires = DateTime.UtcNow.AddYears(1)
            });

            return alert;
        }

        // Appends to loginAlerts.xml; notified="false" marks it for the email step to pick up.
        private static UnrecognizedLoginAlert LogAlert(
            HttpContext context, string userId, string sessionId, string deviceType, DateTime activatedAtUtc)
        {
            var path = context.Server.MapPath("~/App_Data/loginAlerts.xml");
            var doc = Load(path, AlertRoot);
            var id = "alert-" + Guid.NewGuid().ToString("N");

            var el = doc.CreateElement("alert");
            el.SetAttribute("id", id);
            el.SetAttribute("userId", userId);
            el.SetAttribute("sessionId", sessionId);
            el.SetAttribute("deviceType", deviceType);
            el.SetAttribute("activatedAtUtc", FormatUtc(activatedAtUtc));
            el.SetAttribute("notified", "false");
            doc.DocumentElement.AppendChild(el);
            Save(doc, path);

            return new UnrecognizedLoginAlert(id, userId, sessionId, deviceType, activatedAtUtc);
        }

        private static string NewToken()
        {
            var bytes = new byte[32];
            using (var rng = new RNGCryptoServiceProvider())
                rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        private static string Hash(string token)
        {
            using (var sha256 = SHA256.Create())
                return Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(token)));
        }

        private static string FormatUtc(DateTime value)
        {
            return DateTime.SpecifyKind(value, DateTimeKind.Utc).ToString("o", CultureInfo.InvariantCulture);
        }

        private static XmlDocument Load(string path, string root)
        {
            var document = new XmlDocument { XmlResolver = null };
            if (!File.Exists(path))
            {
                document.LoadXml("<?xml version=\"1.0\" encoding=\"utf-8\"?><" + root + " version=\"1\" />");
                return document;
            }

            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
            using (var reader = XmlReader.Create(path, settings))
                document.Load(reader);

            if (document.DocumentElement == null || document.DocumentElement.Name != root)
                throw new InvalidDataException("The " + root + " store has an invalid root element.");
            return document;
        }

        private static void Save(XmlDocument document, string path)
        {
            var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                document.Save(temporaryPath);
                if (File.Exists(path)) File.Replace(temporaryPath, path, null);
                else File.Move(temporaryPath, path);
            }
            catch
            {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
                throw;
            }
        }
    }
}
