using System;
using System.Web;
using System.Web.UI;

namespace CyberApp_FIA
{
    public class SecurePage : System.Web.UI.Page
    {
        protected override void OnInit(EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            Response.Cache.SetExpires(DateTime.UtcNow.AddMinutes(-1));

            if (string.IsNullOrWhiteSpace(Session["UserId"] as string))
            {
                Response.Redirect("~/Account/Login.aspx?expired=1");
                return;
            }

            var timeoutMs = Session.Timeout * 60 * 1000;
            var loginUrl = ResolveUrl("~/Account/Login.aspx?expired=1");

            var script =
                "(function(){" +
                "  var t;" +
                "  function reset(){ clearTimeout(t); t = setTimeout(function(){ window.location = '" + loginUrl + "'; }, " + timeoutMs + "); }" +
                "  reset();" +
                "  window.addEventListener('pageshow', function(e){ if (e.persisted) { window.location.reload(); } });" +
                "  if (window.Sys && Sys.WebForms && Sys.WebForms.PageRequestManager) {" +
                "    Sys.WebForms.PageRequestManager.getInstance().add_endRequest(reset);" +
                "  }" +
                "})();";

            ClientScript.RegisterStartupScript(typeof(SecurePage), "idleTimeout", script, true);

            base.OnInit(e);
        }
    }
}