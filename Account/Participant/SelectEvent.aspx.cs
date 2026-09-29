using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml;
using System.Linq;

namespace CyberApp_FIA.Participant
{
    public partial class SelectEvent : SecurePage
    {
        private string EventsXmlPath => Server.MapPath("~/App_Data/events.xml");
        private string UsersXmlPath => Server.MapPath("~/App_Data/users.xml");

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // Gate: only Participants (adjust if Helpers also use this screen)
                var role = (string)Session["Role"];
                if (!string.Equals(role, "Participant", StringComparison.OrdinalIgnoreCase))
                {
                    Response.Redirect("~/Account/Login.aspx");
                    return;
                }

                // If user explicitly clicked "Change event", bypass fast-path and clear current selection
                var bypass = string.Equals(Request.QueryString["change"], "1", StringComparison.OrdinalIgnoreCase);
                if (bypass)
                {
                    Session["EventId"] = null; // make the picker show instead of jumping back
                }

                // === Fast-path: if this participant has a saved last event and we're NOT bypassing, go straight to Home ===
                if (!bypass)
                {
                    var userId = Session["UserId"] as string; // key by user ID
                    var email = (string)Session["Email"] ?? string.Empty;

                    var lastEvent = LoadLastEventIdForUser(userId);
                    if (!string.IsNullOrWhiteSpace(lastEvent))
                    {
                        if (EventExists(lastEvent))
                        {
                            // Event is still valid: scope them into it and go straight to Home
                            Session["EventId"] = lastEvent;
                            Response.Redirect("~/Account/Participant/Home.aspx");
                            return;
                        }
                        else
                        {
                            // Event no longer exists in events.xml:
                            // - Clear the stale lastEventId from the user's profile
                            // - Clear the current EventId from the session
                            // - Show a notification that their event was removed
                            ClearLastEventIdForUser(userId, email);
                            Session["EventId"] = null;

                            // One-time notification on the Select Event page
                            ClientScript.RegisterStartupScript(
                                this.GetType(),
                                "EventDeletedNotice",
                                "alert('Your previously selected event was removed by a University Admin. Please select a new event.');",
                                true
                            );
                        }
                    }
                }
                // === end fast-path ===

                // Try to preselect from Session or users.xml
                var emailForUni = (string)Session["Email"] ?? "";
                var userUni = (string)Session["University"];
                if (string.IsNullOrWhiteSpace(userUni))
                    userUni = LookupUniversityByEmail(emailForUni);

                // Always bind the University dropdown; preselect if we know one
                BindUniversities(out var hadSelection, prefer: userUni);

                // If we preselected a university, load its active events
                if (hadSelection)
                    BindEventsForUniversity(UniversitySelect.SelectedValue);
            }
        }

        // --- UI events ---

        protected void UniversitySelect_SelectedIndexChanged(object sender, EventArgs e)
        {
            BindEventsForUniversity(UniversitySelect.SelectedValue);
        }

        protected void UniRequired_ServerValidate(object source, ServerValidateEventArgs args)
        {
            args.IsValid = !string.IsNullOrWhiteSpace(UniversitySelect.SelectedValue);
        }

        protected void EventRequired_ServerValidate(object source, ServerValidateEventArgs args)
        {
            args.IsValid = !string.IsNullOrWhiteSpace(EventSelect.SelectedValue);
        }

        protected void BtnContinue_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            var selectedUni = UniversitySelect.SelectedValue;
            var selectedEventId = EventSelect.SelectedValue;

            // Persist scope for this session
            Session["University"] = selectedUni;
            Session["EventId"] = selectedEventId;

            // OPTIONAL: store university on the participant’s profile if it was blank
            var email = (string)Session["Email"] ?? "";
            if (!string.IsNullOrWhiteSpace(email))
            {
                SaveUserUniversityIfEmpty(email, selectedUni);

                // NEW: assign a Helper from this university to the participant (one-time, balanced by current load).
                AssignHelperIfMissing(email, selectedUni);
            }

            // NEW: persist lastEventId on the user's profile (keyed by UserId, fallback to email lookup)
            var userId = Session["UserId"] as string;
            SaveLastEventIdForUser(userId, selectedEventId, email);

            // Go to participant home/catalog
            Response.Redirect("~/Account/Participant/Home.aspx");
        }


        // --- Binding helpers ---

        private void BindUniversities(out bool preselected, string prefer)
        {
            preselected = false;

            UniversitySelect.Items.Clear();
            EventSelect.Items.Clear();

            UniversityAvailabilityMessage.Visible = false;
            EventAvailabilityMessage.Visible = false;

            UniversitySelect.Enabled = true;
            EventSelect.Enabled = false;
            BtnContinue.Enabled = true;

            var activeEvents = LoadActiveEvents().ToList();
            var knownUniversities = LoadKnownUniversities();

            UniversitySelect.Items.Add(new ListItem("-- Select university --", ""));

            foreach (var u in knownUniversities)
            {
                var li = new ListItem(u, u);
                UniversitySelect.Items.Add(li);

                if (!string.IsNullOrWhiteSpace(prefer) &&
                    u.Equals(prefer, StringComparison.OrdinalIgnoreCase))
                {
                    UniversitySelect.ClearSelection();
                    li.Selected = true;
                    preselected = true;
                }
            }

            if (activeEvents.Count == 0)
            {
                UniversityAvailabilityMessage.Text =
                    "No universities currently have an active Cyberfair event available. Please check back later or contact the FIA team.";

                UniversityAvailabilityMessage.Visible = true;

                EventSelect.Items.Add(new ListItem("No active events available", ""));
                EventSelect.Enabled = false;

                EventAvailabilityMessage.Text =
                    "There are no active Cyberfair events to choose from right now.";

                EventAvailabilityMessage.Visible = true;

                BtnContinue.Enabled = false;
                preselected = false;

                return;
            }

            EventSelect.Items.Add(new ListItem("-- Select a university first --", ""));

            if (knownUniversities.Count == 0)
            {
                UniversityAvailabilityMessage.Text =
                    "No universities are available yet. Please contact the FIA team before continuing.";

                UniversityAvailabilityMessage.Visible = true;

                EventSelect.Items.Clear();
                EventSelect.Items.Add(new ListItem("No events available", ""));
                EventSelect.Enabled = false;

                BtnContinue.Enabled = false;
                preselected = false;
            }
        }

        private void BindEventsForUniversity(string uni)
        {
            EventSelect.Items.Clear();
            EventSelect.Items.Add(new ListItem("-- Select event --", ""));

            EventAvailabilityMessage.Visible = false;
            EventSelect.Enabled = true;
            BtnContinue.Enabled = true;

            if (string.IsNullOrWhiteSpace(uni))
            {
                EventSelect.Items.Clear();
                EventSelect.Items.Add(new ListItem("-- Select a university first --", ""));
                EventSelect.Enabled = false;

                EventAvailabilityMessage.Text =
                    "Select a university first, then available Cyberfair events will appear here.";

                EventAvailabilityMessage.Visible = true;
                return;
            }

            var matchingEvents = LoadActiveEvents()
                .Where(ev => ev.university.Equals(uni, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matchingEvents.Count == 0)
            {
                EventSelect.Items.Clear();
                EventSelect.Items.Add(new ListItem("No active events available for this university", ""));
                EventSelect.Enabled = false;
                BtnContinue.Enabled = false;

                EventAvailabilityMessage.Text =
                    "No active Cyberfair events are available for the selected university right now. Please check back later or contact the FIA team.";

                EventAvailabilityMessage.Visible = true;
                return;
            }

            foreach (var ev in matchingEvents)
            {
                var label = ev.name;

                if (ev.startUtc.HasValue)
                {
                    label += " — " + ev.startUtc.Value.ToLocalTime().ToString("MMM d, h:mm tt");
                }

                EventSelect.Items.Add(new ListItem(label, ev.id));
            }
        }


        private SortedSet<string> LoadKnownUniversities()
        {
            var universities = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                if (File.Exists(EventsXmlPath))
                {
                    var doc = new XmlDocument();
                    doc.Load(EventsXmlPath);

                    foreach (XmlElement ev in doc.SelectNodes("/events/event"))
                    {
                        var uni = ev["university"]?.InnerText?.Trim();

                        if (!string.IsNullOrWhiteSpace(uni))
                        {
                            universities.Add(uni);
                        }
                    }
                }

                if (File.Exists(UsersXmlPath))
                {
                    var doc = new XmlDocument();
                    doc.Load(UsersXmlPath);

                    foreach (XmlElement user in doc.SelectNodes("/users/user"))
                    {
                        var uni = user["university"]?.InnerText?.Trim();

                        if (!string.IsNullOrWhiteSpace(uni))
                        {
                            universities.Add(uni);
                        }
                    }
                }
            }
            catch
            {
                // If university lookup fails, return whatever was already found.
            }

            return universities;
        }

        private IEnumerable<(string id, string name, string university, DateTime? startUtc, DateTime? endUtc)> LoadActiveEvents()
        {
            var list = new List<(string, string, string, DateTime?, DateTime?)>();
            if (!File.Exists(EventsXmlPath)) return list;

            var doc = new XmlDocument(); doc.Load(EventsXmlPath);
            foreach (XmlElement ev in doc.SelectNodes("/events/event"))
            {
                // Hide deprecated events; show today/future
                var status = ev.GetAttribute("status");
                if (status.Equals("Deprecated", StringComparison.OrdinalIgnoreCase)) continue;

                var uni = ev["university"]?.InnerText ?? "";
                var name = ev["name"]?.InnerText ?? "(unnamed)";
                var id = ev.GetAttribute("id");
                var startStr = ev["startDate"]?.InnerText ?? ev["date"]?.InnerText ?? "";
                var endStr = ev["endDate"]?.InnerText ?? "";

                DateTime? startDt = DateTime.TryParse(startStr, CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var sw) ? sw : (DateTime?)null;
                DateTime? endDt = DateTime.TryParse(endStr, CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var ew) ? ew : (DateTime?)null;

                var now = DateTime.UtcNow;

                // Show event if: no dates set (legacy), OR current time is within start-end range
                if (startDt.HasValue && endDt.HasValue)
                {
                    if (now < startDt.Value || now > endDt.Value)
                        continue; // outside the event window, skip
                }
                else if (startDt.HasValue)
                {
                    if (startDt.Value.Date < DateTime.UtcNow.Date)
                        continue; // old single-date event that's passed
                }

                list.Add((id, name, uni, startDt, endDt));
            }
            return list;
        }

        // --- Profile helpers ---

        private string LookupUniversityByEmail(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email) || !File.Exists(UsersXmlPath)) return "";
                var doc = new XmlDocument(); doc.Load(UsersXmlPath);
                var emailLower = email.ToLowerInvariant();
                var node = doc.SelectSingleNode(
                    $"/users/user[translate(email,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='{emailLower}']");
                return node?["university"]?.InnerText ?? "";
            }
            catch { return ""; }
        }

        private void SaveUserUniversityIfEmpty(string email, string university)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(university) ||
                    string.IsNullOrWhiteSpace(email) ||
                    !File.Exists(UsersXmlPath)) return;

                var doc = new XmlDocument(); doc.Load(UsersXmlPath);
                var emailLower = email.ToLowerInvariant();
                var node = doc.SelectSingleNode(
                    $"/users/user[translate(email,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='{emailLower}']") as XmlElement;

                if (node == null) return;

                var uniNode = node["university"];
                if (uniNode == null)
                {
                    uniNode = doc.CreateElement("university");
                    node.AppendChild(uniNode);
                }

                if (string.IsNullOrWhiteSpace(uniNode.InnerText))
                {
                    uniNode.InnerText = university;
                    doc.Save(UsersXmlPath);
                }
            }
            catch
            {
                // swallow for now; optional profile write shouldn't block flow
            }
        }

        // --- New helpers for assigning Helpers to Participants ---

        /// <summary>
        /// Assigns a Helper from the given university to the participant identified by email,
        /// but only if they do not already have an assignment.
        /// The chosen Helper is the one in that university with the fewest currently assigned participants
        /// (based on the users.xml assignedHelperId attribute), to help balance load.
        /// </summary>
        private void AssignHelperIfMissing(string email, string university)
        {
            try
            {
                // Guard clauses: we need a valid email, university, and users.xml file.
                if (string.IsNullOrWhiteSpace(email) ||
                    string.IsNullOrWhiteSpace(university) ||
                    !File.Exists(UsersXmlPath))
                {
                    return;
                }

                var doc = new XmlDocument();
                doc.Load(UsersXmlPath);

                var emailLower = email.ToLowerInvariant();

                // Locate the participant user row by email (case-insensitive).
                var userNode = doc.SelectSingleNode(
                    $"/users/user[translate(email,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='{emailLower}']"
                ) as XmlElement;

                if (userNode == null)
                {
                    // No user found for this email; nothing to assign.
                    return;
                }

                // Only assign Helpers for Participant accounts; skip other roles.
                var role = (userNode.GetAttribute("role") ?? string.Empty).Trim();
                if (!role.Equals("Participant", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                // Respect existing assignments: if this participant already has a Helper, stop.
                var existingHelperId = userNode.GetAttribute("assignedHelperId");
                if (!string.IsNullOrWhiteSpace(existingHelperId))
                {
                    return;
                }

                // Collect all Helpers whose university matches the participant's selected university.
                var helperCandidates = new List<XmlElement>();
                var helperNodes = doc.SelectNodes("/users/user[@role='Helper']");
                foreach (XmlElement helper in helperNodes)
                {
                    var helperUni = (helper["university"]?.InnerText ?? string.Empty).Trim();
                    if (helperUni.Length == 0)
                    {
                        continue; // helper not scoped to any university yet
                    }

                    if (string.Equals(helperUni, university.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        helperCandidates.Add(helper);
                    }
                }

                if (helperCandidates.Count == 0)
                {
                    // No Helpers registered for this university; leave unassigned.
                    return;
                }

                // Choose the Helper with the fewest assigned participants in this university.
                XmlElement chosenHelper = null;
                var minCount = int.MaxValue;

                foreach (var helper in helperCandidates)
                {
                    var helperId = helper.GetAttribute("id");
                    if (string.IsNullOrWhiteSpace(helperId))
                    {
                        continue;
                    }

                    // Count participants that currently reference this Helper's id.
                    var assignedNodes = doc.SelectNodes(
                        $"/users/user[@role='Participant' and @assignedHelperId='{helperId}']"
                    );
                    var currentCount = assignedNodes?.Count ?? 0;

                    if (currentCount < minCount)
                    {
                        minCount = currentCount;
                        chosenHelper = helper;
                    }
                }

                if (chosenHelper == null)
                {
                    return;
                }

                var chosenId = chosenHelper.GetAttribute("id");
                if (string.IsNullOrWhiteSpace(chosenId))
                {
                    return;
                }

                // Persist the assignment as an attribute on the participant <user> row.
                // This adds the new attribute to users.xml:
                //   <user id="..." role="Participant" assignedHelperId="helper-guid">
                userNode.SetAttribute("assignedHelperId", chosenId);

                doc.Save(UsersXmlPath);
            }
            catch
            {
                // Helper assignment is best-effort; failures should not block navigation.
            }
        }


        // --- New helpers for lastEventId persistence/lookup ---

        private string LoadLastEventIdForUser(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId) || !File.Exists(UsersXmlPath)) return null;
                var doc = new XmlDocument(); doc.Load(UsersXmlPath);
                var node = (XmlElement)doc.SelectSingleNode($"/users/user[@id='{userId}']/lastEventId");
                return node?.InnerText?.Trim();
            }
            catch { return null; }
        }

        private void SaveLastEventIdForUser(string userId, string eventId, string fallbackEmail = "")
        {
            try
            {
                if (string.IsNullOrEmpty(eventId) || !File.Exists(UsersXmlPath)) return;

                var doc = new XmlDocument(); doc.Load(UsersXmlPath);
                XmlElement user = null;

                if (!string.IsNullOrEmpty(userId))
                {
                    user = doc.SelectSingleNode($"/users/user[@id='{userId}']") as XmlElement;
                }

                // Fallback: locate by email if no userId in session
                if (user == null && !string.IsNullOrWhiteSpace(fallbackEmail))
                {
                    var emailLower = fallbackEmail.ToLowerInvariant();
                    user = doc.SelectSingleNode(
                        $"/users/user[translate(email,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='{emailLower}']") as XmlElement;
                }

                if (user == null) return;

                var node = user["lastEventId"];
                if (node == null)
                {
                    node = doc.CreateElement("lastEventId");
                    user.AppendChild(node);
                }
                node.InnerText = eventId;

                doc.Save(UsersXmlPath);
            }
            catch
            {
                // don't block navigation on profile write issues
            }
        }

        /// <summary>
        /// Clears the stored lastEventId for a user if the event is no longer valid,
        /// so they are brought back to the Select Event page instead of silently failing.
        /// </summary>
        private void ClearLastEventIdForUser(string userId, string fallbackEmail = "")
        {
            try
            {
                if (!File.Exists(UsersXmlPath)) return;

                var doc = new XmlDocument(); doc.Load(UsersXmlPath);
                XmlElement user = null;

                if (!string.IsNullOrEmpty(userId))
                {
                    user = doc.SelectSingleNode($"/users/user[@id='{userId}']") as XmlElement;
                }

                if (user == null && !string.IsNullOrWhiteSpace(fallbackEmail))
                {
                    var emailLower = fallbackEmail.ToLowerInvariant();
                    user = doc.SelectSingleNode(
                        $"/users/user[translate(email,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='{emailLower}']") as XmlElement;
                }

                if (user == null) return;

                var node = user["lastEventId"];
                if (node != null)
                {
                    user.RemoveChild(node);
                    doc.Save(UsersXmlPath);
                }
            }
            catch
            {
                // best-effort clean-up; don't block flow
            }
        }

        private bool EventExists(string eventId)
        {
            try
            {
                if (string.IsNullOrEmpty(eventId) || !File.Exists(EventsXmlPath)) return false;
                var doc = new XmlDocument(); doc.Load(EventsXmlPath);
                var ev = (XmlElement)doc.SelectSingleNode($"/events/event[@id='{eventId}']");
                return ev != null;
            }
            catch { return false; }
        }
    }
}


