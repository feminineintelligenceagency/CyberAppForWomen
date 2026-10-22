using System;

namespace CyberApp_FIA.Services
{
    public class NotificationInfo
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string Category { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string LinkUrl { get; set; }
        public string EventKey { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedUtc { get; set; }
    }
}