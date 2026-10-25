using System;
using System.Linq;
using System.Web.UI.WebControls;
using CyberApp_FIA.Services;

namespace CyberApp_FIA.Participant
{
    /// <summary>
    /// Sprint 3 (#21): participant inbox.
    /// Lists the user's notifications from the shared notification store and lets her
    /// open them or mark them as read. SecurePage keeps logged-out visitors out.
    /// </summary>
    public partial class Notifications : SecurePage
    {
        private string UserId => Session["UserId"] as string;

        protected void Page_Load(object sender, EventArgs e)
        {
            var role = Session["Role"] as string;
            if (!string.Equals(role, "Participant", StringComparison.OrdinalIgnoreCase))
            {
                Response.Redirect("~/Account/Login.aspx");
                return;
            }

            if (!IsPostBack)
            {
                Bind();
            }
        }

        private void Bind()
        {
            var items = NotificationStore.GetForUser(Context, UserId);

            NotificationsRepeater.DataSource = items.Select(n => new
            {
                n.Id,
                n.Title,
                n.Message,
                n.IsRead,
                When = n.CreatedUtc.ToLocalTime().ToString("MMM d, yyyy h:mm tt"),
                HasLink = IsSafeLink(n.LinkUrl)
            }).ToList();
            NotificationsRepeater.DataBind();

            EmptyPanel.Visible = items.Count == 0;
            MarkAllBtn.Visible = items.Any(n => !n.IsRead);
        }

        protected void NotificationsRepeater_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            var id = e.CommandArgument as string;
            if (string.IsNullOrWhiteSpace(id)) return;

            if (e.CommandName == "read")
            {
                NotificationStore.MarkAsRead(Context, UserId, id);
                Bind();
                return;
            }

            if (e.CommandName == "open")
            {
                // Look it up from THIS user's notifications, so nobody can open someone else's.
                var item = NotificationStore.GetForUser(Context, UserId).FirstOrDefault(n => n.Id == id);
                if (item == null) { Bind(); return; }

                NotificationStore.MarkAsRead(Context, UserId, id);

                if (IsSafeLink(item.LinkUrl))
                {
                    Response.Redirect(ResolveUrl(item.LinkUrl), false);
                    Context.ApplicationInstance.CompleteRequest();
                    return;
                }

                Bind();
            }
        }

        protected void MarkAllBtn_Click(object sender, EventArgs e)
        {
            foreach (var n in NotificationStore.GetForUser(Context, UserId).Where(n => !n.IsRead))
            {
                NotificationStore.MarkAsRead(Context, UserId, n.Id);
            }
            Bind();
        }

        /// <summary>
        /// Only follow links that stay inside the app (same rule NotificationService uses when saving),
        /// so a tampered notification can't send the user to another site.
        /// </summary>
        private static bool IsSafeLink(string link)
        {
            return !string.IsNullOrEmpty(link) &&
                   link.StartsWith("~/", StringComparison.Ordinal) &&
                   !link.StartsWith("~//", StringComparison.Ordinal) &&
                   !link.Contains("\\") && !link.Contains("\r") && !link.Contains("\n");
        }
    }
}