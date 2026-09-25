using System;
using System.IO;
using System.Xml;
using System.Web.UI;

namespace CyberApp_FIA.Helper
{
    /// <summary>
    /// Simple in-app viewer for microcourse external resources.
    /// Tries to show the URL in an iframe and always offers a
    /// "open in new tab" fallback.
    /// </summary>
    public partial class ResourceViewer : SecurePage
    {
        private string MicrocoursesXmlPath => Server.MapPath("~/App_Data/microcourses.xml");

        protected void Page_Load(object sender, EventArgs e)
        {
            // Helpers only
            var role = (string)Session["Role"];
            if (!string.Equals(role, "Helper", StringComparison.OrdinalIgnoreCase))
            {
                Response.Redirect("~/Account/Login.aspx");
                return;
            }

            if (!IsPostBack)
            {
                HelperName.Text = GetHelperDisplayName();
                BindView();
            }
        }

        private string GetHelperDisplayName()
        {
            var name = Session["HelperName"] as string;
            if (!string.IsNullOrWhiteSpace(name)) return Server.HtmlEncode(name);

            var email = Session["Email"] as string;
            if (!string.IsNullOrWhiteSpace(email) && email.Contains("@"))
            {
                return Server.HtmlEncode(email.Split('@')[0]);
            }

            return "Peer Helper";
        }

        private void EnsureMicrocoursesXml()
        {
            if (File.Exists(MicrocoursesXmlPath)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(MicrocoursesXmlPath));
            File.WriteAllText(MicrocoursesXmlPath, "<?xml version='1.0' encoding='utf-8'?><microcourses version='1'></microcourses>");
        }

        private void BindView()
        {
            var courseId = Request.QueryString["courseId"];
            var rawUrl = Request.QueryString["url"];

            // Default title/summary
            string title = "Microcourse resources";
            string summary = "Use this viewer to watch videos, review readings, or open linked activities.";

            if (!string.IsNullOrWhiteSpace(courseId))
            {
                EnsureMicrocoursesXml();
                var doc = new XmlDocument();
                doc.Load(MicrocoursesXmlPath);

                var c = (XmlElement)doc.SelectSingleNode($"/microcourses/course[@id='{courseId}']");
                if (c != null)
                {
                    title = c["title"]?.InnerText ?? title;
                    var sum = c["summary"]?.InnerText;
                    if (!string.IsNullOrWhiteSpace(sum))
                    {
                        summary = sum;
                    }
                }
            }

            PageTitleLiteral.Text = Server.HtmlEncode(title);
            SummaryLiteral.Text = Server.HtmlEncode(summary);

            if (string.IsNullOrWhiteSpace(rawUrl))
            {
                FrameWrapper.Visible = false;
                ErrorLiteral.Text = "No resource link was attached to this microcourse yet.";
                return;
            }

            // Basic sanity check on the URL
            Uri uri;
            if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                FrameWrapper.Visible = false;
                ErrorLiteral.Text = "This resource link looks unusual. You can still try opening it in a separate tab.";
                FallbackLink.NavigateUrl = rawUrl;
                return;
            }

            // Configure iframe + fallback
            ResourceFrame.Attributes["src"] = uri.ToString();
            FallbackLink.NavigateUrl = uri.ToString();
            ErrorLiteral.Text = "If the content does not appear or Google Classroom blocks embedding, use the “Open in new tab” button above.";
        }
    }
}
