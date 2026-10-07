using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Web;
using System.Xml;

namespace CyberApp_FIA.Services
{
    internal static class AuthSessionStore
    {
        // Serialize read-modify-write operations so simultaneous requests do not overwrite registry changes.
        private static readonly object StoreLock = new object();
        private const string StoreRoot = "authSessions";

        // Persist a token hash and display metadata, never the reusable raw auth token.
        internal static void Register(
            HttpContext context,
            string sessionId,
            string userId,
            string tokenHash,
            string deviceType,
            DateTime nowUtc,
            int timeoutMinutes)
        {
            lock (StoreLock)
            {
                var path = GetPath(context);
                var document = Load(path);
                // Expired entries should not appear in the settings list or accumulate indefinitely.
                RemoveExpiredSessions(document, nowUtc);

                var session = document.CreateElement("session");
                session.SetAttribute("id", sessionId);
                session.SetAttribute("userId", userId);
                session.SetAttribute("tokenHash", tokenHash);
                session.SetAttribute("deviceType", deviceType);
                session.SetAttribute("createdAtUtc", FormatUtc(nowUtc));
                session.SetAttribute("lastActiveUtc", FormatUtc(nowUtc));
                session.SetAttribute("timeoutMinutes", timeoutMinutes.ToString(CultureInfo.InvariantCulture));
                document.DocumentElement.AppendChild(session);
                Save(document, path);
            }
        }

        internal static bool ValidateAndTouch(
            HttpContext context,
            string sessionId,
            string userId,
            string tokenHash,
            DateTime nowUtc)
        {
            lock (StoreLock)
            {
                var path = GetPath(context);
                var document = Load(path);
                var changed = RemoveExpiredSessions(document, nowUtc);
                var session = FindSession(document, sessionId, userId);
                if (session == null)
                {
                    if (changed) Save(document, path);
                    return false;
                }

                if (!string.Equals(session.GetAttribute("tokenHash"), tokenHash, StringComparison.Ordinal))
                {
                    if (changed) Save(document, path);
                    return false;
                }

                // A successful authenticated request refreshes the idle timeout and displayed last-active time.
                session.SetAttribute("lastActiveUtc", FormatUtc(nowUtc));
                Save(document, path);
                return true;
            }
        }

        internal static IReadOnlyList<AuthSessionInfo> GetActiveSessions(
            HttpContext context,
            string userId,
            string currentSessionId,
            DateTime nowUtc)
        {
            lock (StoreLock)
            {
                var path = GetPath(context);
                var document = Load(path);
                var changed = RemoveExpiredSessions(document, nowUtc);
                var sessions = new List<AuthSessionInfo>();
                var nodes = document.SelectNodes("/" + StoreRoot + "/session");

                foreach (XmlElement session in nodes)
                {
                    if (!string.Equals(session.GetAttribute("userId"), userId, StringComparison.Ordinal))
                        continue;

                    sessions.Add(new AuthSessionInfo(
                        session.GetAttribute("id"),
                        session.GetAttribute("deviceType"),
                        ParseUtc(session.GetAttribute("createdAtUtc")),
                        ParseUtc(session.GetAttribute("lastActiveUtc")),
                        string.Equals(session.GetAttribute("id"), currentSessionId, StringComparison.Ordinal)));
                }

                if (changed) Save(document, path);
                return sessions.AsReadOnly();
            }
        }

        internal static bool RevokeSession(HttpContext context, string userId, string sessionId)
        {
            lock (StoreLock)
            {
                var path = GetPath(context);
                var document = Load(path);
                // Match both identifiers so a session ID from another user cannot be revoked.
                var session = FindSession(document, sessionId, userId);
                if (session == null) return false;

                session.ParentNode.RemoveChild(session);
                Save(document, path);
                return true;
            }
        }

        internal static void RevokeSessionById(HttpContext context, string sessionId)
        {
            lock (StoreLock)
            {
                var path = GetPath(context);
                var document = Load(path);
                var nodes = document.SelectNodes("/" + StoreRoot + "/session");
                var sessionsToRemove = new List<XmlElement>();

                foreach (XmlElement session in nodes)
                {
                    if (string.Equals(session.GetAttribute("id"), sessionId, StringComparison.Ordinal))
                        sessionsToRemove.Add(session);
                }

                if (sessionsToRemove.Count == 0) return;
                foreach (var session in sessionsToRemove)
                    session.ParentNode.RemoveChild(session);
                Save(document, path);
            }
        }

        internal static int RevokeOtherSessions(
            HttpContext context,
            string userId,
            string currentSessionId,
            DateTime nowUtc)
        {
            lock (StoreLock)
            {
                var path = GetPath(context);
                var document = Load(path);
                var changed = RemoveExpiredSessions(document, nowUtc);
                var nodes = document.SelectNodes("/" + StoreRoot + "/session");
                var sessionsToRemove = new List<XmlElement>();

                foreach (XmlElement session in nodes)
                {
                    if (string.Equals(session.GetAttribute("userId"), userId, StringComparison.Ordinal)
                        && !string.Equals(session.GetAttribute("id"), currentSessionId, StringComparison.Ordinal))
                        sessionsToRemove.Add(session);
                }

                foreach (var session in sessionsToRemove)
                    session.ParentNode.RemoveChild(session);

                if (changed || sessionsToRemove.Count > 0) Save(document, path);
                return sessionsToRemove.Count;
            }
        }

        private static XmlDocument Load(string path)
        {
            var document = new XmlDocument { XmlResolver = null };
            if (!File.Exists(path))
            {
                document.LoadXml("<?xml version=\"1.0\" encoding=\"utf-8\"?><" + StoreRoot + " version=\"1\" />");
                return document;
            }

            var settings = new XmlReaderSettings
            {
                // The registry is local application data; prohibit DTDs and external XML resolution.
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            };
            using (var reader = XmlReader.Create(path, settings))
                document.Load(reader);

            if (document.DocumentElement == null || document.DocumentElement.Name != StoreRoot)
                throw new InvalidDataException("The authentication session store has an invalid root element.");

            return document;
        }

        private static bool RemoveExpiredSessions(XmlDocument document, DateTime nowUtc)
        {
            var nodes = document.SelectNodes("/" + StoreRoot + "/session");
            var expired = new List<XmlElement>();
            foreach (XmlElement session in nodes)
            {
                int timeoutMinutes;
                if (!int.TryParse(session.GetAttribute("timeoutMinutes"), NumberStyles.Integer, CultureInfo.InvariantCulture, out timeoutMinutes)
                    || timeoutMinutes <= 0
                    || ParseUtc(session.GetAttribute("lastActiveUtc")).AddMinutes(timeoutMinutes) <= nowUtc)
                    expired.Add(session);
            }

            foreach (var session in expired)
                session.ParentNode.RemoveChild(session);

            return expired.Count > 0;
        }

        private static XmlElement FindSession(XmlDocument document, string sessionId, string userId)
        {
            var nodes = document.SelectNodes("/" + StoreRoot + "/session");
            foreach (XmlElement session in nodes)
            {
                if (string.Equals(session.GetAttribute("id"), sessionId, StringComparison.Ordinal)
                    && string.Equals(session.GetAttribute("userId"), userId, StringComparison.Ordinal))
                    return session;
            }

            return null;
        }

        private static string GetPath(HttpContext context)
        {
            if (context == null) throw new ArgumentNullException("context");
            return context.Server.MapPath("~/App_Data/authSessions.xml");
        }

        private static string FormatUtc(DateTime value)
        {
            return DateTime.SpecifyKind(value, DateTimeKind.Utc).ToString("o", CultureInfo.InvariantCulture);
        }

        private static DateTimeOffset ParseUtc(string value)
        {
            DateTimeOffset parsed;
            if (!DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out parsed))
                throw new InvalidDataException("The authentication session store contains an invalid timestamp.");

            return parsed;
        }

        private static void Save(XmlDocument document, string path)
        {
            var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                // Replace atomically so readers never observe a partially-written registry file.
                document.Save(temporaryPath);
                if (File.Exists(path))
                    File.Replace(temporaryPath, path, null);
                else
                    File.Move(temporaryPath, path);
            }
            catch
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
                throw;
            }
        }
    }
}
