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
    }
}
