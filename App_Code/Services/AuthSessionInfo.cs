using System;

namespace CyberApp_FIA.Services
{
    /// <summary>Safe session details for listing and revoking a user's signed-in devices.</summary>
    public sealed class AuthSessionInfo
    {
        internal AuthSessionInfo(
            string sessionId,
            string deviceType,
            DateTimeOffset createdAtUtc,
            DateTimeOffset lastActiveUtc,
            bool isCurrent)
        {
            SessionId = sessionId;
            DeviceType = deviceType;
            CreatedAtUtc = createdAtUtc;
            LastActiveUtc = lastActiveUtc;
            IsCurrent = isCurrent;
        }

        public string SessionId { get; private set; }
        public string DeviceType { get; private set; }
        /// <summary>UTC time when this authentication session was created.</summary>
        public DateTimeOffset CreatedAtUtc { get; private set; }
        /// <summary>UTC time of the most recent request validated for this session.</summary>
        public DateTimeOffset LastActiveUtc { get; private set; }
        /// <summary>True when this entry belongs to the browser making the listing request.</summary>
        public bool IsCurrent { get; private set; }
    }
}
