using System;
using System.Web;

namespace WebFormsNet48Basics
{
    public class BasePage : System.Web.UI.Page
    {
        private const string AntiCsrfSessionKey = "__AntiCsrfToken";
        private const string AntiCsrfFieldName = "__RequestVerificationToken";

        protected override void OnInit(EventArgs e)
        {
            // Must be set in OnInit/Page_Init for ViewState MAC user binding.
            ViewStateUserKey = Session?.SessionID;
            base.OnInit(e);
        }

        protected override void OnLoad(EventArgs e)
        {
            if (IsPostBack)
            {
                ValidateAntiCsrfToken();
            }

            base.OnLoad(e);
        }

        protected override void OnPreRender(EventArgs e)
        {
            // Emit a standard anti-CSRF hidden field so scanners can detect it.
            ClientScript.RegisterHiddenField(AntiCsrfFieldName, GetOrCreateAntiCsrfToken());
            base.OnPreRender(e);
        }

        private string GetOrCreateAntiCsrfToken()
        {
            if (Session == null)
            {
                throw new HttpException(400, "Bad Request.");
            }

            var token = Session[AntiCsrfSessionKey] as string;
            if (string.IsNullOrEmpty(token))
            {
                token = Guid.NewGuid().ToString("N");
                Session[AntiCsrfSessionKey] = token;
            }

            return token;
        }

        private void ValidateAntiCsrfToken()
        {
            var expectedToken = Session?[AntiCsrfSessionKey] as string;
            var postedToken = Request.Form[AntiCsrfFieldName];

            if (string.IsNullOrEmpty(expectedToken) || !string.Equals(expectedToken, postedToken, StringComparison.Ordinal))
            {
                throw new HttpException(400, "Bad Request.");
            }
        }
    }
}
