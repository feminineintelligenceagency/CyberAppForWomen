using System;
using System.Globalization;
using System.IO;
using System.Web;
using System.Web.UI;
using System.Xml;
using CyberApp_FIA.Services;


namespace CyberApp_FIA.Participant
{
    public partial class HelperMessage : SecurePage
    {
        private string HelperMessagesXmlPath => Server.MapPath("~/App_Data/helperMessages.xml");

        private static readonly object HelperMessagesLock = new object();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
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

                if (!TryGetAssignedHelperInfo(userId, out var helperId, out var helperName, out var helperEmail, out var participantName))
                {
                    // No helper assigned yet; send back to home.
                    Response.Redirect("~/Account/Participant/Home.aspx");
                    return;
                }

                HelperName.Text = Server.HtmlEncode(helperName);
            }
        }

        private string CurrentUserId() => Session["UserId"] as string;

        /// <summary>
        /// Reads users.xml to find the participant's assigned helper and related info.
        /// </summary>
        private bool TryGetAssignedHelperInfo(
            string userId,
            out string helperId,
            out string helperName,
            out string helperEmail,
            out string participantName)
        {
            helperId = null;
            helperName = null;
            helperEmail = null;
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

            helperId = me.GetAttribute("assignedHelperId");
            if (string.IsNullOrWhiteSpace(helperId))
                return false;

            // Participant name: prefer firstName, then name, then email.
            participantName = me["firstName"]?.InnerText;
            if (string.IsNullOrWhiteSpace(participantName))
                participantName = me["name"]?.InnerText ?? me["email"]?.InnerText ?? userId;

            var helper = (XmlElement)doc.SelectSingleNode($"/users/user[@id='{helperId}' and @role='Helper']");
            if (helper == null)
                return false;

            helperName = helper["firstName"]?.InnerText;
            if (string.IsNullOrWhiteSpace(helperName))
                helperName = helper["name"]?.InnerText ?? helper["email"]?.InnerText ?? helperId;

            helperEmail = helper["email"]?.InnerText ?? string.Empty;

            return !string.IsNullOrWhiteSpace(helperName);
        }

        protected void SendButton_Click(object sender, EventArgs e)
        {
            var userId = CurrentUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                Response.Redirect("~/Account/Login.aspx");
                return;
            }

            if (!TryGetAssignedHelperInfo(userId, out var helperId, out var helperName, out var helperEmail, out var participantName))
            {
                FormMessage.Text = "<span style='color:#b00020'>We could not find your assigned Helper. Please try again later.</span>";
                return;
            }

            var topic = (TopicText.Text ?? string.Empty).Trim();
            var body = (BodyText.Text ?? string.Empty).Trim();

            var safeTopic = string.IsNullOrWhiteSpace(topic) ? "(no subject)" : topic;


            if (string.IsNullOrWhiteSpace(topic) || string.IsNullOrWhiteSpace(body))
            {
                FormMessage.Text = "<span style='color:#b00020'>Please enter both a topic and a message before sending.</span>";
                return;
            }

            try
            {
                EnsureXmlDoc(HelperMessagesXmlPath, "helperMessages");

                string conversationId;
                lock (HelperMessagesLock)
                {
                    var doc = new XmlDocument();
                    doc.Load(HelperMessagesXmlPath);

                    conversationId = Guid.NewGuid().ToString("N");
                    var nowUtc = DateTime.UtcNow;

                    var conv = doc.CreateElement("conversation");
                    conv.SetAttribute("id", conversationId);
                    conv.SetAttribute("participantId", userId);
                    conv.SetAttribute("participantName", participantName);
                    conv.SetAttribute("helperId", helperId);
                    conv.SetAttribute("helperName", helperName);
                    conv.SetAttribute("helperEmail", helperEmail);
                    conv.SetAttribute("topic", topic);
                    conv.SetAttribute("createdOn", nowUtc.ToString("o", CultureInfo.InvariantCulture));
                    conv.SetAttribute("lastUpdated", nowUtc.ToString("o", CultureInfo.InvariantCulture));

                    var msg = doc.CreateElement("message");
                    msg.SetAttribute("from", "participant");
                    msg.SetAttribute("senderName", participantName);
                    msg.SetAttribute("ts", nowUtc.ToString("o", CultureInfo.InvariantCulture));
                    msg.InnerText = body;

                    conv.AppendChild(msg);
                    doc.DocumentElement.AppendChild(conv);

                    doc.Save(HelperMessagesXmlPath);
                }

                // INSERT: audit log for initial one-on-one message
                try
                {
                    UniversityAuditLogger.AppendForCurrentUser(
                        this,
                        "Participant Helper Message (Initial)",
                        $"Participant started a one-on-one conversation with {helperName} (topic: \"{safeTopic}\")."
                    );
                }
                catch
                {
                    // Best-effort only; never block messaging on audit failures.
                }

                var url = ResolveUrl("~/Account/Participant/HelperConversation.aspx?id=" + HttpUtility.UrlEncode(conversationId));
                Response.Redirect(url, endResponse: true);

            }
            catch (Exception ex)
            {
                FormMessage.Text = "<span style='color:#b00020'>Message could not be sent: "
                                   + Server.HtmlEncode(ex.Message) + "</span>";
            }
        }

        protected void CancelButton_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/Account/Participant/Home.aspx");
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

