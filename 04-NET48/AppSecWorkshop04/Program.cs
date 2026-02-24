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
        new Endpoint("POST", "/vuln/register"),
        new Endpoint("POST", "/secure/register"),
        new Endpoint("GET", "/vuln/files/read"),
        new Endpoint("GET", "/secure/files/read"),
        new Endpoint("GET", "/vuln/redirect"),
        new Endpoint("GET", "/secure/redirect"),
        new Endpoint("GET", "/vuln/errors/divide-by-zero"),
        new Endpoint("GET", "/secure/errors/divide-by-zero")
    };

    private static string _contentRoot = AppDomain.CurrentDomain.BaseDirectory;
    private static string _safeFolder = AppDomain.CurrentDomain.BaseDirectory;

    private static void Main(string[] args)
    {
        _contentRoot = AppDomain.CurrentDomain.BaseDirectory;
        _safeFolder = Path.Combine(_contentRoot, "safe-files");
        Directory.CreateDirectory(_safeFolder);
        File.WriteAllText(Path.Combine(_safeFolder, "public-note.txt"), "Public workshop note.");

        var urlsArg = args.FirstOrDefault(a => a.StartsWith("--urls=", StringComparison.OrdinalIgnoreCase));
        var urls = urlsArg == null
            ? new[] { "http://localhost:5104" }
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
            listener.Prefixes.Add("http://localhost:5104/");
        }

        listener.Start();
        Console.WriteLine("AppSecWorkshop04 NET48 compat host listening on: " + string.Join(", ", listener.Prefixes.Cast<string>()));

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
                "{\"workshop\":\"04-NET48\",\"application\":\"AppSecWorkshop04\",\"net48Compat\":true,\"modules\":[\"Input validation\",\"Path traversal\",\"Open redirect\",\"Error handling\"]}");
            return;
        }

        if (path == "/vuln/register")
        {
            var body = ReadBody(ctx.Request);
            var username = ReadJsonString(body, "username") ?? string.Empty;
            var password = ReadJsonString(body, "password") ?? string.Empty;

            Console.WriteLine("[WARN] vuln/register username=" + username + " password=" + password);
            WriteJson(ctx.Response, 200,
                "{\"mode\":\"vulnerable\",\"message\":\"Compte cree (sans validation robuste).\"}");
            return;
        }

        if (path == "/secure/register")
        {
            var body = ReadBody(ctx.Request);
            var username = ReadJsonString(body, "username") ?? string.Empty;
            var password = ReadJsonString(body, "password") ?? string.Empty;

            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(username) || username.Length < 4)
            {
                errors.Add("username: minimum 4 caracteres.");
            }

            if (!Regex.IsMatch(username, "^[a-zA-Z0-9_.-]+$"))
            {
                errors.Add("username: caracteres non autorises.");
            }

            if (!IsStrongPassword(password))
            {
                errors.Add("password: minimum 12 caracteres avec majuscule, minuscule, chiffre et caractere special.");
            }

            if (errors.Count > 0)
            {
                var errorsJson = "[" + string.Join(",", errors.Select(e => "\"" + Escape(e) + "\"")) + "]";
                WriteJson(ctx.Response, 400, "{\"mode\":\"secure\",\"errors\":" + errorsJson + "}");
                return;
            }

            WriteJson(ctx.Response, 200,
                "{\"mode\":\"secure\",\"message\":\"Compte cree avec validation.\",\"note\":\"Ne jamais journaliser les secrets.\"}");
            return;
        }

        if (path == "/vuln/files/read")
        {
            var requestedPath = ReadQuery(ctx, "path", string.Empty);
            var fullPath = Path.Combine(_contentRoot, requestedPath);
            if (!File.Exists(fullPath))
            {
                WriteJson(ctx.Response, 404, "{\"error\":\"Fichier introuvable.\"}");
                return;
            }

            var content = File.ReadAllText(fullPath);
            WriteJson(ctx.Response, 200,
                "{\"mode\":\"vulnerable\",\"path\":\"" + Escape(fullPath) + "\",\"content\":\"" + Escape(content) + "\"}");
            return;
        }

        if (path == "/secure/files/read")
        {
            var fileName = ReadQuery(ctx, "fileName", string.Empty);
            if (!Regex.IsMatch(fileName, "^[a-zA-Z0-9_.-]+$"))
            {
                WriteJson(ctx.Response, 400, "{\"mode\":\"secure\",\"error\":\"Nom de fichier invalide.\"}");
                return;
            }

            var fullPath = Path.GetFullPath(Path.Combine(_safeFolder, fileName));
            if (!fullPath.StartsWith(_safeFolder, StringComparison.OrdinalIgnoreCase))
            {
                WriteJson(ctx.Response, 400, "{\"mode\":\"secure\",\"error\":\"Tentative de traversal detectee.\"}");
                return;
            }

            if (!File.Exists(fullPath))
            {
                WriteJson(ctx.Response, 404, "{\"mode\":\"secure\",\"error\":\"Fichier introuvable.\"}");
                return;
            }

            var content = File.ReadAllText(fullPath);
            WriteJson(ctx.Response, 200,
                "{\"mode\":\"secure\",\"fileName\":\"" + Escape(fileName) + "\",\"content\":\"" + Escape(content) + "\"}");
            return;
        }

        if (path == "/vuln/redirect")
        {
            var returnUrl = ReadQuery(ctx, "returnUrl", "/");
            ctx.Response.StatusCode = 302;
            ctx.Response.RedirectLocation = returnUrl;
            ctx.Response.OutputStream.Close();
            return;
        }

        if (path == "/secure/redirect")
        {
            var returnUrl = ReadQuery(ctx, "returnUrl", "/");
            if (returnUrl.IndexOf("://", StringComparison.Ordinal) >= 0 || !returnUrl.StartsWith("/", StringComparison.Ordinal))
            {
                WriteJson(ctx.Response, 400, "{\"mode\":\"secure\",\"error\":\"URL externe refusee.\"}");
                return;
            }

            WriteJson(ctx.Response, 200,
                "{\"mode\":\"secure\",\"message\":\"Redirection validee.\",\"target\":\"" + Escape(returnUrl) + "\"}");
            return;
        }

        if (path == "/vuln/errors/divide-by-zero")
        {
            var x = 0;
            var value = 10 / x;
            WriteJson(ctx.Response, 200, "{\"value\":" + value + "}");
            return;
        }

        if (path == "/secure/errors/divide-by-zero")
        {
            try
            {
                var x = 0;
                var value = 10 / x;
                WriteJson(ctx.Response, 200, "{\"value\":" + value + "}");
            }
            catch
            {
                WriteJson(ctx.Response, 500,
                    "{\"type\":\"about:blank\",\"title\":\"Operation failed.\",\"status\":500,\"detail\":\"Unexpected arithmetic error.\"}");
            }
        }
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
