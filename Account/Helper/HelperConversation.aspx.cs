using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Web;
using System.Web.UI;
using System.Xml;
using CyberApp_FIA.Services;


namespace CyberApp_FIA.Helper
{
    /// <summary>
    /// Helper view of a single one-on-one conversation thread.
    /// Reads and writes to helperMessages.xml, sharing the same schema as the participant side.
    /// </summary>
    public partial class HelperConversation : SecurePage
    {
        private string HelperMessagesXmlPath => Server.MapPath("~/App_Data/helperMessages.xml");
        private string UsersXmlPath => Server.MapPath("~/App_Data/users.xml");

        private sealed class MessageRow
        {
            public string FromRoleCss { get; set; } // "helper" or "participant"
            public string FromLabel { get; set; }   // "You" or "Participant"
            public DateTime SentOnLocal { get; set; }
            public string BodyHtml { get; set; }
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
            var convId = (Request.QueryString["id"] ?? string.Empty).Trim();
            var participantId = (Request.QueryString["participantId"] ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(helperId) || string.IsNullOrWhiteSpace(convId))
            {
                Response.Redirect("~/Account/Helper/OneOnOneHelp.aspx");
                return;
            }

            if (string.IsNullOrWhiteSpace(participantId))
            {
                // For safety, we allow missing participantId but we won't link back nicely.
                BackToParticipantConversations.NavigateUrl = ResolveUrl("~/Account/Helper/OneOnOneHelp.aspx");
            }
            else
            {
                BackToParticipantConversations.NavigateUrl = ResolveUrl(
                    "~/Account/Helper/ParticipantConversations.aspx?participantId=" + Server.UrlEncode(participantId));
            }

            // Load conversation from XML
            XmlDocument doc;
            XmlElement conv;
            if (!TryLoadConversation(helperId, convId, out doc, out conv))
            {
                // If we can't find it, send them back.
                Response.Redirect("~/Account/Helper/OneOnOneHelp.aspx");
                return;
            }

            // Mark as read as soon as the helper opens the thread
            MarkConversationReadByHelper(doc, conv);

            // Participant name in header
            var participantIdAttr = participantId;
            if (string.IsNullOrWhiteSpace(participantIdAttr))
                participantIdAttr = conv.GetAttribute("participantId") ?? string.Empty;

            ParticipantName.Text = GetParticipantDisplayName(participantIdAttr);

            // Topic + dates
            var topic = conv.GetAttribute("topic") ?? "";
            Topic.Text = Server.HtmlEncode(topic);

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

            CreatedOn.Text = createdLocal.ToString("MMM d, yyyy • h:mm tt");
            LastUpdated.Text = lastLocal.ToString("MMM d, yyyy • h:mm tt");

            // Messages
            var rows = LoadMessageRows(conv);
            MessagesRepeater.DataSource = rows;
            MessagesRepeater.DataBind();
        }

        private bool TryLoadConversation(string helperId, string convId, out XmlDocument doc, out XmlElement conv)
        {
            doc = null;
            conv = null;

            if (!File.Exists(HelperMessagesXmlPath))
                return false;

            try
            {
                doc = new XmlDocument();
                doc.Load(HelperMessagesXmlPath);

                conv = (XmlElement)doc.SelectSingleNode(
                    $"/helperMessages/conversation[@id='{convId}' and @helperId='{helperId}']");

                return conv != null;
            }
            catch
            {
                return false;
            }
        }

        private void MarkConversationReadByHelper(XmlDocument doc, XmlElement conv)
        {
            if (doc == null || conv == null)
                return;

            try
            {
                conv.SetAttribute("helperLastReadUtc", DateTime.UtcNow.ToString("o"));
                doc.Save(HelperMessagesXmlPath);
            }
            catch
            {
                // Best-effort only; failure to write the read timestamp
                // should not stop the conversation page from loading.
            }
        }

        private string GetParticipantDisplayName(string participantId)
        {
            if (string.IsNullOrWhiteSpace(participantId) || !File.Exists(UsersXmlPath))
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

        private List<MessageRow> LoadMessageRows(XmlElement conv)
        {
            var rows = new List<MessageRow>();
            if (conv == null)
                return rows;

            var msgNodes = conv.SelectNodes("message");
            if (msgNodes == null)
                return rows;

            foreach (XmlElement m in msgNodes)
            {
                var from = (m.GetAttribute("from") ?? string.Empty).Trim();
                var sentOnStr = (m.GetAttribute("sentOn") ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(sentOnStr))
                {
                    sentOnStr = (m.GetAttribute("ts") ?? string.Empty).Trim();
                }

                DateTime sentOnUtc;
                if (!DateTime.TryParse(sentOnStr, CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out sentOnUtc))
                {
                    DateTime.TryParse(sentOnStr, out sentOnUtc);
                }

                var sentLocal = DateTime.SpecifyKind(sentOnUtc, DateTimeKind.Utc).ToLocalTime();

                // Body can be either inner text or <body> child element; support both
                string bodyRaw = m["body"]?.InnerText;
                if (string.IsNullOrEmpty(bodyRaw))
                    bodyRaw = m.InnerText ?? string.Empty;

                var bodyHtml = Server.HtmlEncode(bodyRaw).Replace("\r\n", "<br />").Replace("\n", "<br />");

                var isHelper = from.Equals("helper", StringComparison.OrdinalIgnoreCase)
                               || from.Equals("Helper", StringComparison.OrdinalIgnoreCase);

                var row = new MessageRow
                {
                    FromRoleCss = isHelper ? "helper" : "participant",
                    FromLabel = isHelper ? "You" : "Participant",
                    SentOnLocal = sentLocal,
                    BodyHtml = bodyHtml
                };

                rows.Add(row);
            }

            return rows;
        }

        protected void SendReplyButton_Click(object sender, EventArgs e)
        {
            FormMessage.Text = string.Empty;

            var helperId = Session["UserId"] as string ?? "";
            var convId = (Request.QueryString["id"] ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(helperId) || string.IsNullOrWhiteSpace(convId))
            {
                FormMessage.Text = "<span style='color:#b91c1c;'>Missing conversation context.</span>";
                return;
            }

            var replyText = (ReplyText.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(replyText))
            {
                FormMessage.Text = "<span style='color:#b91c1c;'>Please enter a message before sending.</span>";
                return;
            }

            XmlDocument doc;
            XmlElement conv;
            if (!TryLoadConversation(helperId, convId, out doc, out conv) || conv == null)
            {
                FormMessage.Text = "<span style='color:#b91c1c;'>Conversation not found.</span>";
                return;
            }

            try
            {
                // Append new message node
                var msg = doc.CreateElement("message");
                msg.SetAttribute("from", "Helper");
                msg.SetAttribute("sentOn", DateTime.UtcNow.ToString("o"));

                // Simple inner text message body (participant side can also read this)
                msg.InnerText = replyText;

                conv.AppendChild(msg);

                // Update lastUpdated attribute
                conv.SetAttribute("helperLastReadUtc", DateTime.UtcNow.ToString("o"));

                doc.Save(HelperMessagesXmlPath);

                // --- Audit log: helper replied to participant in this conversation ---
                try
                {
                    // Get participant id: prefer querystring, fall back to conversation attribute
                    var participantId = (Request.QueryString["participantId"] ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(participantId))
                    {
                        participantId = conv.GetAttribute("participantId") ?? string.Empty;
                    }

                    var participantDisplay = GetParticipantDisplayName(participantId);

                    var topic = conv.GetAttribute("topic") ?? string.Empty;
                    var safeTopic = topic ?? string.Empty;
                    if (safeTopic.Length > 120)
                    {
                        safeTopic = safeTopic.Substring(0, 120) + "...";
                    }

                    UniversityAuditLogger.AppendForCurrentUser(
                        this,
                        "Helper Message (Reply)",
                        $"Helper replied in a one-on-one conversation with {participantDisplay} (topic: \"{safeTopic}\").");
                }
                catch
                {
                    // Audit logging should never break main helper flows.
                }
                // --- end audit log block ---

                // Clear textbox and reload thread so Helper sees their message
                ReplyText.Text = string.Empty;
                FormMessage.Text = "<span style='color:#065f46;'>Reply sent.</span>";
                LoadPage();
            }
            catch (Exception ex)
            {
                FormMessage.Text = "<span style='color:#b91c1c;'>Failed to send reply: "
                    + Server.HtmlEncode(ex.Message) + "</span>";
            }

        }
    }
}
