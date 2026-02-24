using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

internal static class Program
{
    private static readonly Endpoint[] Endpoints =
    {
        new Endpoint("GET", "/"),
        new Endpoint("GET", "/vuln/xss"),
        new Endpoint("GET", "/secure/xss"),
        new Endpoint("GET", "/vuln/open-redirect"),
        new Endpoint("GET", "/secure/open-redirect"),
        new Endpoint("POST", "/secure/register")
    };

    private static void Main(string[] args)
    {
        var urlsArg = args.FirstOrDefault(a => a.StartsWith("--urls=", StringComparison.OrdinalIgnoreCase));
        var urls = urlsArg == null
            ? new[] { "http://localhost:5105" }
            : urlsArg.Substring(7).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

        var listener = new HttpListener();
        foreach (var raw in urls)
        {
            Uri uri;
            if (!Uri.TryCreate(raw.Trim(), UriKind.Absolute, out uri))
            {
                continue;
            }

            var path = uri.AbsolutePath;
            if (string.IsNullOrWhiteSpace(path))
            {
                path = "/";
            }
            if (!path.EndsWith("/"))
            {
                path += "/";
            }

            listener.Prefixes.Add(uri.Scheme + "://" + uri.Host + ":" + uri.Port + path);
        }

        if (listener.Prefixes.Count == 0)
        {
            listener.Prefixes.Add("http://localhost:5105/");
        }

        listener.Start();
        Console.WriteLine("SecurityValidationLab NET48 compat host listening on: " + string.Join(", ", listener.Prefixes.Cast<string>()));

        while (true)
        {
            var ctx = listener.GetContext();
            try
            {
                ApplySecurityHeaders(ctx.Response);
                Handle(ctx);
            }
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

        var endpoint = Endpoints.FirstOrDefault(e => e.Method == method && e.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
        if (endpoint == null)
        {
            WriteJson(ctx.Response, 404, "{\"error\":\"not-found\",\"method\":\"" + Escape(method) + "\",\"path\":\"" + Escape(path) + "\"}");
            return;
        }

        if (path == "/")
        {
            WriteJson(ctx.Response, 200,
                "{\"workshop\":\"05-NET48\",\"application\":\"SecurityValidationLab\",\"net48Compat\":true,\"modules\":[\"Regression Tests\",\"SAST\",\"DAST\"]}");
            return;
        }

        if (path == "/vuln/xss")
        {
            var input = ReadQuery(ctx, "input", string.Empty);
            var html = "<html><body><div>" + input + "</div></body></html>";
            WriteHtml(ctx.Response, 200, html);
            return;
        }

        if (path == "/secure/xss")
        {
            var input = ReadQuery(ctx, "input", string.Empty);
            var safe = WebUtility.HtmlEncode(input);
            var html = "<html><body><div>" + safe + "</div></body></html>";
            WriteHtml(ctx.Response, 200, html);
            return;
        }

        if (path == "/vuln/open-redirect")
        {
            var returnUrl = ReadQuery(ctx, "returnUrl", "/");
            ctx.Response.StatusCode = 302;
            ctx.Response.RedirectLocation = returnUrl;
            ctx.Response.OutputStream.Close();
            return;
        }

        if (path == "/secure/open-redirect")
        {
            var returnUrl = ReadQuery(ctx, "returnUrl", string.Empty);
            if (!IsRelativeReturnUrl(returnUrl))
            {
                WriteJson(ctx.Response, 400, "{\"error\":\"Only relative returnUrl is allowed.\"}");
                return;
            }

            WriteJson(ctx.Response, 200, "{\"redirectTarget\":\"" + Escape(returnUrl) + "\"}");
            return;
        }

        var body = ReadBody(ctx.Request);
        var username = ReadJsonString(body, "username") ?? string.Empty;
        var password = ReadJsonString(body, "password") ?? string.Empty;

        var errors = new List<string>();
        if (!Regex.IsMatch(username, "^[a-zA-Z0-9_.-]{4,30}$"))
        {
            errors.Add("Invalid username format.");
        }

        if (!IsStrongPassword(password))
        {
            errors.Add("Weak password.");
        }

        if (errors.Count > 0)
        {
            var errorsJson = "[" + string.Join(",", errors.Select(e => "\"" + Escape(e) + "\"")) + "]";
            WriteJson(ctx.Response, 400, "{\"errors\":" + errorsJson + "}");
            return;
        }

        WriteJson(ctx.Response, 200, "{\"message\":\"Account validated.\"}");
    }

    private static bool IsRelativeReturnUrl(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (!value.StartsWith("/", StringComparison.Ordinal))
        {
            return false;
        }

        if (value.StartsWith("//", StringComparison.Ordinal))
        {
            return false;
        }

        return value.IndexOf("://", StringComparison.Ordinal) < 0;
    }

    private static bool IsStrongPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 12)
        {
            return false;
        }

        var hasUpper = password.Any(char.IsUpper);
        var hasLower = password.Any(char.IsLower);
        var hasDigit = password.Any(char.IsDigit);
        var hasSpecial = password.Any(c => !char.IsLetterOrDigit(c));
        return hasUpper && hasLower && hasDigit && hasSpecial;
    }

    private static string ReadBody(HttpListenerRequest request)
    {
        using (var reader = new StreamReader(request.InputStream, request.ContentEncoding ?? Encoding.UTF8))
        {
            return reader.ReadToEnd();
        }
    }

    private static string ReadJsonString(string json, string key)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        var m = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"(?<v>(?:\\\\.|[^\"])*)\"", RegexOptions.IgnoreCase);
        return m.Success ? Regex.Unescape(m.Groups["v"].Value) : null;
    }

    private static string ReadQuery(HttpListenerContext ctx, string key, string defaultValue)
    {
        var value = ctx.Request.QueryString[key];
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
    }

    private static void ApplySecurityHeaders(HttpListenerResponse response)
    {
        response.Headers["X-Content-Type-Options"] = "nosniff";
        response.Headers["X-Frame-Options"] = "DENY";
        response.Headers["Referrer-Policy"] = "no-referrer";
        response.Headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'; base-uri 'none';";
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
        return (s ?? string.Empty)
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", string.Empty)
            .Replace("\n", " ");
    }

    private sealed class Endpoint
    {
        public Endpoint(string method, string path)
        {
            Method = method;
            Path = path;
        }

        public string Method { get; private set; }
        public string Path { get; private set; }
    }
}
