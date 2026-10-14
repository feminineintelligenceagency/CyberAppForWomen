using System;
using System.Globalization;
using System.Xml;

namespace CyberApp_FIA.Account
{
    // Epic 1 - User Story #6 (dmundra-29): Added login lockout enforcement with failed-attempt tracking and reset logic.
    internal static class LoginRateLimiting
    {
        internal const int MaxFailedAttempts = 5;
        internal static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
        private const string FailedLoginCountElement = "failedLoginCount";
        private const string LockoutUntilElement = "lockoutUntilUtc";

        internal static bool IsLockedOut(XmlElement user, DateTime utcNow)
        {
            if (user == null) return false;
            var until = ReadUtc(user, LockoutUntilElement);
            if (!until.HasValue) return false;
            if (until.Value > utcNow) return true;
            ClearLockout(user);
            return false;
        }

        internal static void RecordFailure(XmlDocument document, XmlElement user, DateTime utcNow)
        {
            if (document == null) throw new ArgumentNullException("document");
            if (user == null) throw new ArgumentNullException("user");
            var count = ReadInt(user, FailedLoginCountElement) + 1;
            SetChildText(document, user, FailedLoginCountElement, count.ToString(CultureInfo.InvariantCulture));
            if (count >= MaxFailedAttempts)
                SetChildText(document, user, LockoutUntilElement, utcNow.Add(LockoutDuration).ToString("o", CultureInfo.InvariantCulture));
        }

        internal static void RecordSuccess(XmlDocument document, XmlElement user)
        {
            if (document == null) throw new ArgumentNullException("document");
            if (user == null) throw new ArgumentNullException("user");
            ClearLockout(document, user);
        }

        internal static void ClearLockout(XmlElement user)
        {
            if (user != null && user.OwnerDocument != null) ClearLockout(user.OwnerDocument, user);
        }

        internal static void ClearLockout(XmlDocument document, XmlElement user)
        {
            if (document == null) throw new ArgumentNullException("document");
            if (user == null) throw new ArgumentNullException("user");
            SetChildText(document, user, FailedLoginCountElement, "0");
            SetChildText(document, user, LockoutUntilElement, "");
        }

        private static int ReadInt(XmlElement user, string name)
        {
            int value;
            return int.TryParse(user[name]?.InnerText, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) && value >= 0 ? value : 0;
        }

        private static DateTime? ReadUtc(XmlElement user, string name)
        {
            DateTime value;
            return DateTime.TryParse(user[name]?.InnerText, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out value) ? value : (DateTime?)null;
        }

        private static void SetChildText(XmlDocument document, XmlElement parent, string name, string value)
        {
            var child = parent[name];
            if (child == null) { child = document.CreateElement(name); parent.AppendChild(child); }
            child.InnerText = value ?? "";
        }
    }
}
