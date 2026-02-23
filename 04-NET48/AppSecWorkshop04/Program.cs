using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

internal static class Program
{
    private static readonly Endpoint[] Endpoints = new[]
    {
        new Endpoint("GET", "/"),
        new Endpoint("GET", "/secure/errors/divide-by-zero"),
        new Endpoint("GET", "/secure/files/read"),
        new Endpoint("GET", "/secure/redirect"),
        new Endpoint("GET", "/vuln/errors/divide-by-zero"),
        new Endpoint("GET", "/vuln/files/read"),
        new Endpoint("GET", "/vuln/redirect"),
        new Endpoint("POST", "/secure/register"),
        new Endpoint("POST", "/vuln/register")
    };

    private static void Main(string[] args)
    {
        var urlsArg = args.FirstOrDefault(a => a.StartsWith("--urls=", StringComparison.OrdinalIgnoreCase));
        var urls = urlsArg == null ? new[] { "http://localhost:5100" } : urlsArg.Substring(7).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

        var listener = new HttpListener();
        foreach (var raw in urls)
        {
            Uri uri;
            if (!Uri.TryCreate(raw.Trim(), UriKind.Absolute, out uri)) continue;
            var host = (uri.Host == "localhost" || uri.Host == "127.0.0.1") ? "+" : uri.Host;
            var path = uri.AbsolutePath;
            if (string.IsNullOrWhiteSpace(path)) path = "/";
            if (!path.EndsWith("/")) path += "/";
            listener.Prefixes.Add(uri.Scheme + "://" + host + ":" + uri.Port + path);
        }
        if (listener.Prefixes.Count == 0) listener.Prefixes.Add("http://+:5100/");

        listener.Start();
        Console.WriteLine("AppSecWorkshop04 NET48 compat host listening on: " + string.Join(", ", listener.Prefixes.Cast<string>()));

        while (true)
        {
            var ctx = listener.GetContext();
            try { Handle(ctx); }
            catch (Exception ex)
            {
                WriteJson(ctx.Response, 500, "{\"error\":\"internal-error\",\"detail\":\"" + Escape(ex.Message) + "\"}");
            }
        }
    }

    private static void Handle(HttpListenerContext ctx)
    {
        var method = ctx.Request.HttpMethod.ToUpperInvariant();
        var path = ctx.Request.Url == null ? "/" : ctx.Request.Url.AbsolutePath;

        var endpoint = Endpoints.FirstOrDefault(e => e.Method == method && e.Match(path));
        if (endpoint == null)
        {
            WriteJson(ctx.Response, 404, "{\"error\":\"not-found\",\"method\":\"" + Escape(method) + "\",\"path\":\"" + Escape(path) + "\"}");
            return;
        }

        string body = null;
        if (method == "POST" || method == "PUT")
        {
            using (var reader = new StreamReader(ctx.Request.InputStream, ctx.Request.ContentEncoding ?? Encoding.UTF8))
            {
                body = reader.ReadToEnd();
            }
        }

        if (path == "/")
        {
            var endpoints = string.Join(",", Endpoints.Select(e => "\"" + e.Method + " " + Escape(e.Template) + "\""));
            WriteJson(ctx.Response, 200, "{\"workshop\":\"04-NET48\",\"application\":\"AppSecWorkshop04\",\"net48Compat\":true,\"endpoints\":[" + endpoints + "]}");
            return;
        }

        if (path.IndexOf("/vuln/open-redirect", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            var target = ReadQuery(ctx, "returnUrl", "/");
            ctx.Response.StatusCode = 302;
            ctx.Response.RedirectLocation = target;
            ctx.Response.OutputStream.Close();
            return;
        }

        if (path.IndexOf("/secure/open-redirect", StringComparison.OrdinalIgnoreCase) >= 0 || path.IndexOf("/secure/redirect", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            var target = ReadQuery(ctx, "returnUrl", "/");
            if (!target.StartsWith("/")) { WriteJson(ctx.Response, 400, "{\"error\":\"invalid-return-url\"}"); return; }
            ctx.Response.StatusCode = 302;
            ctx.Response.RedirectLocation = target;
            ctx.Response.OutputStream.Close();
            return;
        }

        if (path.IndexOf("/vuln/clickjacking/page", StringComparison.OrdinalIgnoreCase) >= 0 || path.IndexOf("/secure/clickjacking/page", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            if (path.IndexOf("/secure/", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                ctx.Response.Headers["X-Frame-Options"] = "DENY";
                ctx.Response.Headers["Content-Security-Policy"] = "frame-ancestors 'none'; default-src 'self'";
            }
            WriteHtml(ctx.Response, 200, "<html><body><h2>Zone sensible</h2><button>Transferer</button></body></html>");
            return;
        }

        if (path.IndexOf("/session/login", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            var sid = Guid.NewGuid().ToString("N");
            var secure = path.IndexOf("/secure/", StringComparison.OrdinalIgnoreCase) >= 0;
            var cookie = secure ? "session-id=" + sid + "; path=/; HttpOnly; Secure; SameSite=Strict" : "session-id=" + sid + "; path=/; SameSite=None";
            ctx.Response.Headers.Add("Set-Cookie", cookie);
        }

        if (path.IndexOf("/secure/admin", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            var admin = ctx.Request.Headers["X-Admin-Key"];
            var soc = ctx.Request.Headers["X-SOC-Key"];
            if (string.IsNullOrWhiteSpace(admin) && string.IsNullOrWhiteSpace(soc)) { WriteJson(ctx.Response, 403, "{\"error\":\"admin-key-required\"}"); return; }
        }

        if (path.IndexOf("/secure/resource/cpu", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            var seconds = ReadIntQuery(ctx, "seconds", 1);
            if (seconds > 2) { WriteJson(ctx.Response, 429, "{\"error\":\"throttled\"}"); return; }
        }

        if (path.IndexOf("/vuln/resource/cpu", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            var seconds = ReadIntQuery(ctx, "seconds", 1);
            if (seconds < 1 || seconds > 5) { WriteJson(ctx.Response, 400, "{\"error\":\"seconds doit etre entre 1 et 5\"}"); return; }
        }

        if (path.IndexOf("/secure/ssrf/fetch", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            var url = ReadQuery(ctx, "url", string.Empty).ToLowerInvariant();
            if (url.Contains("localhost") || url.Contains("127.0.0.1") || url.Contains("169.254.")) { WriteJson(ctx.Response, 400, "{\"error\":\"ssrf-blocked\"}"); return; }
        }

        if (path.IndexOf("/secure/upload/meta", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            var fileName = ReadQuery(ctx, "fileName", string.Empty).ToLowerInvariant();
            if (fileName.EndsWith(".exe") || fileName.EndsWith(".ps1")) { WriteJson(ctx.Response, 400, "{\"error\":\"blocked-extension\"}"); return; }
        }

        var mode = path.IndexOf("/vuln/", StringComparison.OrdinalIgnoreCase) >= 0 ? "vulnerable" : (path.IndexOf("/secure/", StringComparison.OrdinalIgnoreCase) >= 0 ? "secure" : "neutral");
        var payload = "{" +
            "\"workshop\":\"04-NET48\"," +
            "\"application\":\"AppSecWorkshop04\"," +
            "\"net48Compat\":true," +
            "\"mode\":\"" + Escape(mode) + "\"," +
            "\"method\":\"" + Escape(method) + "\"," +
            "\"path\":\"" + Escape(path) + "\"," +
            "\"bodyLength\":" + (body == null ? 0 : body.Length).ToString() +
            "}";
        WriteJson(ctx.Response, 200, payload);
    }

    private static string ReadQuery(HttpListenerContext ctx, string key, string defaultValue)
    {
        var value = ctx.Request.QueryString[key];
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
    }

    private static int ReadIntQuery(HttpListenerContext ctx, string key, int defaultValue)
    {
        int parsed;
        return int.TryParse(ReadQuery(ctx, key, defaultValue.ToString()), out parsed) ? parsed : defaultValue;
    }

    private static void WriteHtml(HttpListenerResponse response, int statusCode, string body)
    {
        var bytes = Encoding.UTF8.GetBytes(body);
        response.StatusCode = statusCode;
        response.ContentType = "text/html; charset=utf-8";
        response.ContentLength64 = bytes.LongLength;
        response.OutputStream.Write(bytes, 0, bytes.Length);
        response.OutputStream.Close();
    }

    private static void WriteJson(HttpListenerResponse response, int statusCode, string body)
    {
        var bytes = Encoding.UTF8.GetBytes(body);
        response.StatusCode = statusCode;
        response.ContentType = "application/json; charset=utf-8";
        response.ContentLength64 = bytes.LongLength;
        response.OutputStream.Write(bytes, 0, bytes.Length);
        response.OutputStream.Close();
    }

    private static string Escape(string s)
    {
        return (s ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", " ");
    }

    private sealed class Endpoint
    {
        public Endpoint(string method, string template)
        {
            Method = method;
            Template = template;
            Pattern = BuildPattern(template);
        }

        public string Method { get; private set; }
        public string Template { get; private set; }
        private Regex Pattern { get; set; }

        public bool Match(string path)
        {
            return Pattern.IsMatch(path ?? "/");
        }

        private static Regex BuildPattern(string template)
        {
            var regex = "^" + Regex.Escape(template).Replace("\\{id:int\\}", "[0-9]+") + "$";
            regex = Regex.Replace(regex, "\\{[^/]+\\}", "[^/]+");
            return new Regex(regex, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }
    }
}
