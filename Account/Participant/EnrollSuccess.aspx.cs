using System;
using System.Globalization;
using System.IO;
using System.Web.UI;
using System.Xml;

namespace CyberApp_FIA.Participant
{
    public partial class EnrollSuccess : SecurePage
    {
        private string EventSessionsXmlPath => Server.MapPath("~/App_Data/eventSessions.xml");
        private string MicrocoursesXmlPath => Server.MapPath("~/App_Data/microcourses.xml");

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;

            // In Page_Load of EnrollSuccess.aspx.cs
            var ret = Request.QueryString["return"] ?? "";
            LnkHome.NavigateUrl = ResolveUrl("~/Account/Participant/Home.aspx") + (string.IsNullOrEmpty(ret) ? "" : ret);

            var eventId = Request.QueryString["eventId"] ?? "";
            var sessionId = Request.QueryString["sessionId"] ?? "";

            if (string.IsNullOrWhiteSpace(eventId) || string.IsNullOrWhiteSpace(sessionId))
            {
                Response.Redirect("~/Account/Participant/Home.aspx");
                return;
            }

            // Load session details
            string courseId = null, helper = "", room = "Zoom";
            DateTime startUtc = DateTime.UtcNow, endUtc = DateTime.UtcNow.AddMinutes(30);

            if (File.Exists(EventSessionsXmlPath))
            {
                var doc = new System.Xml.XmlDocument(); doc.Load(EventSessionsXmlPath);
                var s = (System.Xml.XmlElement)doc.SelectSingleNode($"/eventSessions/session[@eventId='{eventId}' and @id='{sessionId}']");
                if (s != null)
                {
                    courseId = s["courseId"]?.InnerText ?? courseId;
                    helper = s["helper"]?.InnerText ?? helper;
                    room = string.IsNullOrWhiteSpace(s["room"]?.InnerText) ? room : s["room"].InnerText;

                    if (DateTime.TryParse(s["start"]?.InnerText, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var st))
                        startUtc = st;
                    if (DateTime.TryParse(s["end"]?.InnerText, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var en))
                        endUtc = en;
                }
            }

            var title = "(untitled)";
            if (!string.IsNullOrWhiteSpace(courseId) && File.Exists(MicrocoursesXmlPath))
            {
                var doc = new System.Xml.XmlDocument(); doc.Load(MicrocoursesXmlPath);
                var c = (System.Xml.XmlElement)doc.SelectSingleNode($"/microcourses/course[@id='{courseId}']");
                if (c != null) title = c["title"]?.InnerText ?? title;
            }

            // UI
            LitCourseTitle.Text = Server.HtmlEncode(title);
            LitHelper.Text = Server.HtmlEncode(string.IsNullOrWhiteSpace(helper) ? "TBA" : helper);
            LitLocation.Text = Server.HtmlEncode(string.IsNullOrWhiteSpace(room) ? "Zoom" : room);

            var localStart = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc).ToLocalTime();
            var localEnd = DateTime.SpecifyKind(endUtc, DateTimeKind.Utc).ToLocalTime();
            LitWhen.Text = $"{localStart:ddd, MMM d • h:mm tt} – {localEnd:h:mm tt} ({TimeZoneInfo.Local.StandardName})";

            // ICS link
            var icsUrl = ResolveUrl($"~/Account/Participant/Calendar.ashx?eventId={Uri.EscapeDataString(eventId)}&sessionId={Uri.EscapeDataString(sessionId)}");
            LnkIcs.NavigateUrl = icsUrl;
        }
    }
}

