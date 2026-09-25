using System;
using System.Globalization;
using System.IO;
using System.Web.UI;
using System.Xml;
using System.Collections.Generic;
using CyberApp_FIA.Services;

namespace CyberApp_FIA.Account
{
    /// <summary>
    /// University Admin home/dashboard.
    /// - Gates access to users with the "UniversityAdmin" role.
    /// - Displays the admin's university and lists that university's events.
    /// - Allows creating new events stored in ~/App_Data/events.xml.
    /// - Provides navigation into the university-scoped audit log.
    /// - NEW: Provides navigation to the "Add Helper" page for this university.
    /// </summary>
    public partial class UniversityAdminHome : Page
    {
        /// <summary>
        /// Physical path to the events XML datastore (per-app, non-public App_Data folder).
        /// </summary>
        private string EventsXmlPath => Server.MapPath("~/App_Data/events.xml");

        /// <summary>
        /// Physical path to the users XML datastore (used to look up a user's university).
        /// </summary>
        private string UsersXmlPath => Server.MapPath("~/App_Data/users.xml");

        /// <summary>
        /// Initial page load: authorize role, resolve university, populate UI, bind events list.
        /// </summary>
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // ---- Access gate: only University Admins allowed on this page ----
                var role = (string)Session["Role"];
                if (!string.Equals(role, "UniversityAdmin", StringComparison.OrdinalIgnoreCase))
                {
                    // If not authorized, bounce to Login.
                    Response.Redirect("~/Account/Login.aspx");
                    return;
                }

                // Show a friendly greeting: use email if available, else a generic label.
                var email = (string)Session["Email"] ?? "";
                WelcomeName.Text = email.Length > 0 ? email : "University Admin";

                // Determine university to operate under:
                // 1) prefer value from session (set at login),
                // 2) else try users.xml lookup by email.
                var uni = (string)Session["University"];
                if (string.IsNullOrWhiteSpace(uni))
                {
                    uni = LookupUniversityByEmail(email);
                }

                // Display the university and keep a hidden field for consistent saves.
                UniversityDisplay.Text = string.IsNullOrWhiteSpace(uni) ? "(not set)" : uni;
                UniversityValue.Value = uni; // fixed value used when saving the event

                // Populate the event list for this university.
                BindEventsForUniversity(uni);
            }
        }

        /// <summary>
        /// Logs the user out by clearing the session and returning to the welcome page.
        /// </summary>
        protected void BtnLogout_Click(object sender, EventArgs e)
        {
            AuthSession.SignOut(Context);   // Epic #7: fully end the session and delete session + token cookies
            Response.Redirect("~/Welcome_Page.aspx");
        }

        /// <summary>
        /// Navigates to the University Admin audit log page where the admin can
        /// review helper progress and general activity for their university.
        /// </summary>
        protected void BtnOpenAudit_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/Account/UniversityAdmin/UniversityAdminAudit.aspx");
        }

        /// <summary>
        /// NEW: Navigates to the page where this University Admin can add helpers
        /// for their own university.
        /// </summary>
        protected void BtnAddHelper_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/Account/UniversityAdmin/UniversityAdminAddHelper.aspx");
        }

        /// <summary>
        /// Creates a new event for the admin's university:
        /// - Validates inputs
        /// - Ensures events.xml exists
        /// - Appends a new &lt;event&gt; with metadata, description, and date (ISO 8601 UTC midnight)
        /// - Rebinds the event list
        /// </summary>
        protected void BtnCreateEvent_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            var uni = (UniversityValue.Value ?? "").Trim();
            var name = (EventName.Text ?? "").Trim();
            var desc = (Description.Text ?? "").Trim();
            var startInput = (EventStartDate.Text ?? "").Trim();
            var endInput = (EventEndDate.Text ?? "").Trim();

            if (string.IsNullOrEmpty(uni))
            {
                FormMessage.Text = "<span style='color:#c21d1d'>Your university is not set. Ask a Super Admin to add it to your account.</span>";
                return;
            }

            if (!TryParseLocalToIso(startInput, out var startIso))
            {
                FormMessage.Text = "<span style='color:#c21d1d'>Please enter a valid start date and time.</span>";
                return;
            }

            if (!TryParseLocalToIso(endInput, out var endIso))
            {
                FormMessage.Text = "<span style='color:#c21d1d'>Please enter a valid end date and time.</span>";
                return;
            }

            if (string.Compare(endIso, startIso, StringComparison.Ordinal) <= 0)
            {
                FormMessage.Text = "<span style='color:#c21d1d'>End date/time must be after start.</span>";
                return;
            }

            EnsureEventsXml();

            var doc = new XmlDocument();
            doc.Load(EventsXmlPath);

            var ev = doc.CreateElement("event");
            ev.SetAttribute("id", Guid.NewGuid().ToString("N"));
            ev.SetAttribute("status", "Draft");
            ev.SetAttribute("createdAt", DateTime.UtcNow.ToString("o"));
            ev.SetAttribute("createdBy", (Session["Email"] as string) ?? "universityadmin@unknown");

            ev.AppendChild(Mk(doc, "university", uni));
            ev.AppendChild(Mk(doc, "name", name));
            ev.AppendChild(Mk(doc, "startDate", startIso));
            ev.AppendChild(Mk(doc, "endDate", endIso));
            ev.AppendChild(Mk(doc, "description", desc));
            ev.AppendChild(doc.CreateElement("courses"));

            doc.DocumentElement.AppendChild(ev);
            doc.Save(EventsXmlPath);

            // Inform user, clear inputs, and refresh the event list
            FormMessage.Text = "<span style='color:#0a7a3c'>Cyberfair event created.</span>";
            ClearForm();
            BindEventsForUniversity(uni);
        }
        

        /// <summary>
        /// Clears form fields for event creation (university remains unchanged).
        /// </summary>
        protected void BtnClear_Click(object sender, EventArgs e)
        {
            ClearForm();
            FormMessage.Text = "";
        }

        // ---------- Helpers ----------

        /// <summary>
        /// Ensures events.xml exists in App_Data with a versioned <events> root element.
        /// Creates directories as needed.
        /// </summary>
        private void EnsureEventsXml()
        {
            if (File.Exists(EventsXmlPath)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(EventsXmlPath));
            var init = "<?xml version='1.0' encoding='utf-8'?><events version='1'></events>";
            File.WriteAllText(EventsXmlPath, init);
        }

        /// <summary>
        /// Binds the repeater with this university's events.
        /// - Reads events.xml
        /// - Filters by <university> exact match (case-insensitive)
        /// - Projects minimal fields for UI consumption (id, name, status, date, manageUrl)
        /// </summary>
        private void BindEventsForUniversity(string uni)
        {
            var currentRows = new List<object>();
            var pastRows = new List<object>();

            if (!File.Exists(EventsXmlPath))
            {
                NoCurrentEventsPlaceholder.Visible = true;
                NoPastEventsPlaceholder.Visible = true;

                CurrentEventsRepeater.DataSource = currentRows;
                CurrentEventsRepeater.DataBind();

                PastEventsRepeater.DataSource = pastRows;
                PastEventsRepeater.DataBind();

                return;
            }

            var nowUtc = DateTime.UtcNow;

            var doc = new XmlDocument();
            doc.Load(EventsXmlPath);
            var nodes = doc.SelectNodes("/events/event");

            foreach (XmlElement ev in nodes)
            {
                var evUni = ev["university"]?.InnerText ?? "";
                if (!string.Equals(evUni, uni, StringComparison.OrdinalIgnoreCase)) continue;

                var id = ev.GetAttribute("id");
                var name = ev["name"]?.InnerText ?? "(unnamed)";
                var status = ev.GetAttribute("status");

                if (string.IsNullOrWhiteSpace(status))
                {
                    status = "Draft";
                }

                var startStr = ev["startDate"]?.InnerText ?? ev["date"]?.InnerText ?? "";
                var endStr = ev["endDate"]?.InnerText ?? "";

                var hasStartDate = DateTime.TryParse(
                    startStr,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                    out var startUtc
                );

                var hasEndDate = DateTime.TryParse(
                    endStr,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                    out var endUtc
                );

                string dateHuman;

                if (hasStartDate && hasEndDate)
                {
                    dateHuman = startUtc.ToLocalTime().ToString("MMM d, h:mm tt") +
                                " — " + endUtc.ToLocalTime().ToString("MMM d, h:mm tt");
                }
                else if (hasStartDate)
                {
                    dateHuman = startUtc.ToLocalTime().ToString("MMM d, h:mm tt");
                }
                else
                {
                    dateHuman = "(unset)";
                }

                var row = new
                {
                    id,
                    name,
                    status,
                    dateHuman,
                    manageUrl = ResolveUrl($"~/Account/UniversityAdmin/EventManage.aspx?id={id}")
                };

                // Past Events:
                // If the event has a valid end date and that end date is before the current UTC time,
                // it belongs in the Past Events section.
                if (hasEndDate && endUtc < nowUtc)
                {
                    pastRows.Add(row);
                }
                else
                {
                    // Upcoming / Current Events:
                    // If the event is ongoing, future, or missing a valid end date, keep it visible here.
                    currentRows.Add(row);
                }
            }

            NoCurrentEventsPlaceholder.Visible = currentRows.Count == 0;
            CurrentEventsRepeater.DataSource = currentRows;
            CurrentEventsRepeater.DataBind();

            NoPastEventsPlaceholder.Visible = pastRows.Count == 0;
            PastEventsRepeater.DataSource = pastRows;
            PastEventsRepeater.DataBind();
        }

        /// <summary>
        /// Utility: create an XML element with text content (null-safe to empty).
        /// </summary>
        private static XmlElement Mk(XmlDocument d, string name, string val)
        {
            var el = d.CreateElement(name);
            el.InnerText = val ?? "";
            return el;
        }

        /// <summary>
        /// Parses a date string (typically HTML5 yyyy-MM-dd) as local time and converts to ISO 8601 UTC midnight.
        /// Returns true on success with the ISO string in 'iso'; otherwise false.
        /// </summary>
        private static bool TryParseLocalToIso(string input, out string iso)
        {
            iso = "";
            if (string.IsNullOrWhiteSpace(input)) return false;

            if (DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt) ||
                DateTime.TryParse(input, out dt))
            {
                iso = dt.ToUniversalTime().ToString("o");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Looks up the user's university in users.xml based on their email (case-insensitive).
        /// Returns empty string if not found or on error.
        /// </summary>
        private string LookupUniversityByEmail(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email) || !File.Exists(UsersXmlPath)) return "";
                var doc = new XmlDocument();
                doc.Load(UsersXmlPath);
                var emailLower = email.ToLowerInvariant();

                // Case-insensitive email match via translate() for normalization.
                var node = doc.SelectSingleNode(
                    $"/users/user[translate(email,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='{emailLower}']");
                return node?["university"]?.InnerText ?? "";
            }
            catch { return ""; }
        }

        /// <summary>
        /// Resets form input fields; university stays as-is (set at login/page load).
        /// </summary>
        private void ClearForm()
        {
            EventName.Text = "";
            EventStartDate.Text = "";
            EventEndDate.Text = "";
            Description.Text = "";
        }
    }
}

