using CyberApp_FIA.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Xml;

namespace CyberApp_FIA.Helper
{
    /// <summary>
    /// Helper view of all conversations with a single participant.
    /// Reads from helperMessages.xml and filters by participantId + helperId.
    /// </summary>
    public partial class ParticipantConversations : Page
    {
        private string HelperMessagesXmlPath => Server.MapPath("~/App_Data/helperMessages.xml");
        private string UsersXmlPath => Server.MapPath("~/App_Data/users.xml");

        private sealed class ConversationRow
        {
            public string Topic { get; set; }
            public DateTime CreatedOnLocal { get; set; }
            public DateTime LastUpdatedLocal { get; set; }
            public string ViewUrl { get; set; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                GuardHelperRole();
                LoadPage();
            }
        }

        private void GuardHelperRole()
        {
            var roleRaw = Session["Role"] as string ?? "";
            if (!string.Equals(roleRaw.Trim(), "Helper", StringComparison.OrdinalIgnoreCase))
            {
                Response.Redirect("~/Account/Login.aspx");
                return;
            }

            var userId = Session["UserId"] as string;
            if (string.IsNullOrWhiteSpace(userId))
            {
                Response.Redirect("~/Account/Login.aspx");
                return;
            }
        }

        private void LoadPage()
        {
            var helperId = Session["UserId"] as string ?? "";
            var participantId = (Request.QueryString["participantId"] ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(helperId) || string.IsNullOrWhiteSpace(participantId))
            {
                Response.Redirect("~/Account/Helper/OneOnOneHelp.aspx");
                return;
            }

            // Set participant name in header
            ParticipantName.Text = GetParticipantDisplayName(participantId);

            var rows = LoadConversationRows(helperId, participantId);

            NoConversationsPH.Visible = rows.Count == 0;
            ConversationsRepeater.DataSource = rows;
            ConversationsRepeater.DataBind();
        }

        private string GetParticipantDisplayName(string participantId)
        {
            if (!File.Exists(UsersXmlPath))
                return "(participant)";

            try
            {
                var doc = new XmlDocument();
                doc.Load(UsersXmlPath);

                var user = (XmlElement)doc.SelectSingleNode($"/users/user[@id='{participantId}']");
                if (user == null)
                    return "(participant)";

                var firstName = user["firstName"]?.InnerText ?? "";
                var lastName = user["lastName"]?.InnerText ?? "";
                var email = user["email"]?.InnerText ?? "";

                if (!string.IsNullOrWhiteSpace(firstName))
                {
                    if (!string.IsNullOrWhiteSpace(lastName))
                        return $"{firstName} {lastName}".Trim();
                    return firstName.Trim();
                }

                if (!string.IsNullOrWhiteSpace(email))
                    return email.Trim();

                return "(participant)";
            }
            catch
            {
                return "(participant)";
            }
        }

        private List<ConversationRow> LoadConversationRows(string helperId, string participantId)
        {
            var rows = new List<ConversationRow>();

            if (!File.Exists(HelperMessagesXmlPath))
                return rows;

            try
            {
                var doc = new XmlDocument();
                doc.Load(HelperMessagesXmlPath);

                var convNodes = doc.SelectNodes(
                    $"/helperMessages/conversation[@helperId='{helperId}' and @participantId='{participantId}']");

                if (convNodes == null)
                    return rows;

                foreach (XmlElement conv in convNodes)
                {
                    var topic = DataProtector.Decrypt(conv.GetAttribute("topic") ?? "");   // Epic #7: decrypt topic
                    var id = conv.GetAttribute("id") ?? "";

                    var createdOnStr = conv.GetAttribute("createdOn");
                    var lastUpdatedStr = conv.GetAttribute("lastUpdated");

                    DateTime createdOnUtc;
                    DateTime lastUpdatedUtc;

                    if (!DateTime.TryParse(createdOnStr, CultureInfo.InvariantCulture,
                        DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out createdOnUtc))
                    {
                        DateTime.TryParse(createdOnStr, out createdOnUtc);
                    }

                    if (!DateTime.TryParse(lastUpdatedStr, CultureInfo.InvariantCulture,
                        DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out lastUpdatedUtc))
                    {
                        lastUpdatedUtc = createdOnUtc;
                    }

                    var createdLocal = DateTime.SpecifyKind(createdOnUtc, DateTimeKind.Utc).ToLocalTime();
                    var lastLocal = DateTime.SpecifyKind(lastUpdatedUtc, DateTimeKind.Utc).ToLocalTime();

                    var viewUrl = ResolveUrl(
                        "~/Account/Helper/HelperConversation.aspx?id="
                        + HttpUtility.UrlEncode(id)
                        + "&participantId="
                        + HttpUtility.UrlEncode(participantId));

                    rows.Add(new ConversationRow
                    {
                        Topic = topic,
                        CreatedOnLocal = createdLocal,
                        LastUpdatedLocal = lastLocal,
                        ViewUrl = viewUrl
                    });
                }
            }
            catch
            {
                rows.Clear();
            }

            return rows
                .OrderByDescending(r => r.LastUpdatedLocal)
                .ToList();
        }
    }
}
