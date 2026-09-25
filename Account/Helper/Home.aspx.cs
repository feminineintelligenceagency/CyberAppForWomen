using System;
using System.IO;
using System.Web.UI;
using System.Xml;

namespace CyberApp_FIA.Helper
{
    /// <summary>
    /// Landing workspace for Helpers.
    /// Greets the Helper by name, shows their university and role,
    /// and gives them a branded top panel.
    /// </summary>
    public partial class Home : SecurePage
    {
        private string UsersXmlPath => Server.MapPath("~/App_Data/users.xml");

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                InitializeHeader();
            }
        }

        /// <summary>
        /// Loads the Helper's name and university from session / users.xml and
        /// populates the top-panel literals. Also enforces that only Helpers
        /// can reach this workspace.
        /// </summary>
        private void InitializeHeader()
        {
            var roleRaw = Session["Role"] as string ?? "";
            if (!string.Equals(roleRaw.Trim(), "Helper", StringComparison.OrdinalIgnoreCase))
            {
                // If someone who is not a Helper hits this page, send them back to login.
                Response.Redirect("~/Account/Login.aspx");
                return;
            }

            var userId = Session["UserId"] as string;
            if (string.IsNullOrWhiteSpace(userId))
            {
                Response.Redirect("~/Account/Login.aspx");
                return;
            }

            string firstName = "";
            string lastName = "";
            string university = Session["University"] as string ?? "";

            try
            {
                if (File.Exists(UsersXmlPath))
                {
                    var doc = new XmlDocument();
                    doc.Load(UsersXmlPath);

                    // Simple lookup by @id in users.xml
                    var node = doc.SelectSingleNode($"/users/user[@id='{userId}']");
                    if (node != null)
                    {
                        firstName = node["firstName"]?.InnerText ?? "";
                        lastName = node["lastName"]?.InnerText ?? "";

                        var uniFromFile = node["university"]?.InnerText;
                        if (!string.IsNullOrWhiteSpace(uniFromFile))
                        {
                            university = uniFromFile;
                        }
                    }
                }
            }
            catch
            {
                // If anything goes wrong with XML loading, fall back gracefully to session values.
            }

            var fullName = (firstName + " " + lastName).Trim();
            if (string.IsNullOrWhiteSpace(fullName))
            {
                fullName = "Peer Helper";
            }

            HelperName.Text = Server.HtmlEncode(fullName);

            if (string.IsNullOrWhiteSpace(university))
            {
                University.Text = "Your University";
            }
            else
            {
                University.Text = Server.HtmlEncode(university);
            }

            // Role is fixed label for this workspace
            RoleLiteral.Text = "Peer Helper";
        }

        /// <summary>
        /// Simple logout: clear session and return to the welcome page.
        /// </summary>
        protected void BtnLogout_Click(object sender, EventArgs e)
        {
            SessionHelper.SignOut(Context);
            Response.Redirect("~/Welcome_Page.aspx");
        }
    }
}
