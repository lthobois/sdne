using System;
using System.Web;
using System.Web.UI;

namespace WebFormsNet48Basics
{
    public class Global : HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            ScriptManager.ScriptResourceMapping.AddDefinition(
                "jquery",
                new ScriptResourceDefinition
                {
                    Path = "~/Scripts/jquery-3.7.1.min.js",
                    DebugPath = "~/Scripts/jquery-3.7.1.js",
                    CdnPath = "https://code.jquery.com/jquery-3.7.1.min.js",
                    CdnDebugPath = "https://code.jquery.com/jquery-3.7.1.js",
                    CdnSupportsSecureConnection = true,
                    LoadSuccessExpression = "window.jQuery"
                });
        }

        protected void Application_EndRequest()
        {
            var resp = HttpContext.Current.Response;
            resp.Cache.SetCacheability(HttpCacheability.NoCache);
            resp.Cache.SetNoStore();
            resp.Cache.SetRevalidation(HttpCacheRevalidation.AllCaches);
            resp.Cache.SetExpires(DateTime.UtcNow.AddMinutes(-1));
            resp.AppendHeader("Pragma", "no-cache");

            foreach (string cookieName in resp.Cookies)
            {
                var cookie = resp.Cookies[cookieName];
                if (cookie == null)
                {
                    continue;
                }

                cookie.Secure = true;
                cookie.HttpOnly = true;
                cookie.SameSite = SameSiteMode.Lax;
            }
        }
    }
}
