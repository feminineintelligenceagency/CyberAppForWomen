using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Web;
using System.Web.UI;
using System.Xml;
using CyberApp_FIA.Services;


namespace CyberApp_FIA.Participant
{
    public partial class HelperConversation : SecurePage
    {
        private string HelperMessagesXmlPath => Server.MapPath("~/App_Data/helperMessages.xml");
        private static readonly object HelperMessagesLock = new object();

        private string ConversationId => Request.QueryString["id"] ?? string.Empty;

        protected void Page_Load(object sender, EventArgs e)
        {
            var role = (string)Session["Role"];
            if (!string.Equals(role, "Participant", StringComparison.OrdinalIgnoreCase))
            {
                Response.Redirect("~/Account/Login.aspx");
                return;
            }

            var userId = CurrentUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                Response.Redirect("~/Account/Login.aspx");
                return;
            }

            if (string.IsNullOrWhiteSpace(ConversationId))
            {
                Response.Redirect("~/Account/Participant/Home.aspx");
                return;
            }

            if (!IsPostBack)
            {
                try
                {
                    LoadConversation(userId, ConversationId);
                }
                catch
                {
                    Response.Redirect("~/Account/Participant/Home.aspx");
                }
            }
        }

        private string CurrentUserId() => Session["UserId"] as string;

        private void LoadConversation(string participantId, string conversationId)
        {
            if (!File.Exists(HelperMessagesXmlPath))
                throw new InvalidOperationException("No conversations found.");

            var doc = new XmlDocument();
            doc.Load(HelperMessagesXmlPath);

            var conv = (XmlElement)doc.SelectSingleNode($"/helperMessages/conversation[@id='{conversationId}']");
            if (conv == null)
                throw new InvalidOperationException("Conversation not found.");

            var convParticipantId = conv.GetAttribute("participantId");
            if (!string.Equals(convParticipantId, participantId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Not your conversation.");

            var topic = conv.GetAttribute("topic") ?? "";
            var helperName = conv.GetAttribute("helperName") ?? "";

            TopicLiteral.Text = Server.HtmlEncode(topic);
            HelperNameLiteral.Text = Server.HtmlEncode(helperName);

            var rows = new List<object>();

            foreach (XmlElement msg in conv.SelectNodes("message"))
            {
                var from = msg.GetAttribute("from") ?? "participant";
                var senderName = msg.GetAttribute("senderName") ?? "";
                var tsStr = msg.GetAttribute("ts") ?? "";
                var body = msg.InnerText ?? "";

                DateTime tsUtc;
                if (!DateTime.TryParse(tsStr, CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out tsUtc))
                {
                    DateTime.TryParse(tsStr, out tsUtc);
                }

                var local = DateTime.SpecifyKind(tsUtc, DateTimeKind.Utc).ToLocalTime();
                var isMe = string.Equals(from, "participant", StringComparison.OrdinalIgnoreCase);

                rows.Add(new
                {
                    SenderName = senderName,
                    TimeLocal = local,
                    Body = body,
                    CssClass = isMe ? "msg msg-me" : "msg msg-them"
                });
            }

            MessagesRepeater.DataSource = rows;
            MessagesRepeater.DataBind();
        }

        protected void SendReplyButton_Click(object sender, EventArgs e)
        {
            var userId = CurrentUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                Response.Redirect("~/Account/Login.aspx");
                return;
            }

            var reply = (ReplyBody.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(reply))
            {
                FormMessage.Text = "<span style='color:#b00020'>Please enter a message before sending.</span>";
                return;
            }

            try
            {
                string participantName;
                if (!TryGetParticipantName(userId, out participantName))
                {
                    FormMessage.Text = "<span style='color:#b00020'>We could not confirm your account name. Please try again later.</span>";
                    return;
                }

                EnsureXmlDoc(HelperMessagesXmlPath, "helperMessages");

                // For audit logging after we save
                string topicForLog = null;
                string helperNameForLog = null;

                lock (HelperMessagesLock)
                {
                    var doc = new XmlDocument();
                    doc.Load(HelperMessagesXmlPath);

                    var conv = (XmlElement)doc.SelectSingleNode($"/helperMessages/conversation[@id='{ConversationId}']");
                    if (conv == null)
                    {
                        FormMessage.Text = "<span style='color:#b00020'>Conversation not found.</span>";
                        return;
                    }

                    var convParticipantId = conv.GetAttribute("participantId");
                    if (!string.Equals(convParticipantId, userId, StringComparison.OrdinalIgnoreCase))
                    {
                        FormMessage.Text = "<span style='color:#b00020'>You do not have access to this conversation.</span>";
                        return;
                    }

                    var nowUtc = DateTime.UtcNow;

                    var msg = doc.CreateElement("message");
                    msg.SetAttribute("from", "participant");
                    msg.SetAttribute("senderName", participantName);
                    msg.SetAttribute("ts", nowUtc.ToString("o", CultureInfo.InvariantCulture));
                    msg.InnerText = reply;

                    conv.AppendChild(msg);
                    conv.SetAttribute("lastUpdated", nowUtc.ToString("o", CultureInfo.InvariantCulture));

                    // capture for logging outside the lock
                    topicForLog = conv.GetAttribute("topic") ?? string.Empty;
                    helperNameForLog = conv.GetAttribute("helperName") ?? string.Empty;

                    doc.Save(HelperMessagesXmlPath);
                }

                // INSERT: audit log for participant reply in conversation
                try
                {
                    var safeTopic = string.IsNullOrWhiteSpace(topicForLog) ? "(no subject)" : topicForLog;
                    var safeHelperName = string.IsNullOrWhiteSpace(helperNameForLog) ? "their Helper" : helperNameForLog;

                    UniversityAuditLogger.AppendForCurrentUser(
                        this,
                        "Participant Helper Message (Reply)",
                        $"Participant replied in a one-on-one conversation with {safeHelperName} (topic: \"{safeTopic}\")."
                    );
                }
                catch
                {
                    // Best-effort only
                }


                ReplyBody.Text = string.Empty;
                LoadConversation(userId, ConversationId);
                FormMessage.Text = "<span style='color:#0a6b4f'>Message sent.</span>";
            }
            catch (Exception ex)
            {
                FormMessage.Text = "<span style='color:#b00020'>Message could not be sent: "
                                   + Server.HtmlEncode(ex.Message) + "</span>";
            }
        }

        /// <summary>
        /// Gets the participant's display name from users.xml.
        /// </summary>
        private bool TryGetParticipantName(string userId, out string participantName)
        {
            participantName = null;

            if (string.IsNullOrWhiteSpace(userId))
                return false;

            var usersPath = Server.MapPath("~/App_Data/users.xml");
            if (!File.Exists(usersPath))
                return false;

            var doc = new XmlDocument();
            doc.Load(usersPath);

            var me = (XmlElement)doc.SelectSingleNode($"/users/user[@id='{userId}']");
            if (me == null)
                return false;

            participantName = me["firstName"]?.InnerText;
            if (string.IsNullOrWhiteSpace(participantName))
                participantName = me["name"]?.InnerText ?? me["email"]?.InnerText ?? userId;

            return !string.IsNullOrWhiteSpace(participantName);
        }

        private void EnsureXmlDoc(string path, string rootName)
        {
            if (File.Exists(path)) return;
            var doc = new XmlDocument();
            doc.AppendChild(doc.CreateElement(rootName));
            doc.Save(path);
        }
    }
}
