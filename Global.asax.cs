using System;
using System.Web;

namespace CyberApp_FIA
{
    public class Global : HttpApplication
    {
        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            if (!Request.IsSecureConnection)
            {
                var secureUrl = new UriBuilder(Request.Url)
                {
                    Scheme = Uri.UriSchemeHttps,
                    Port = Request.Url.IsLoopback ? 44347 : 443
                };

                Response.RedirectPermanent(secureUrl.Uri.AbsoluteUri, false);
                Context.ApplicationInstance.CompleteRequest();
            }
        }
    }
}