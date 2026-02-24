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
        new Endpoint("GET", "/vuln/admin/ping"),
        new Endpoint("GET", "/secure/admin/ping"),
        new Endpoint("GET", "/vuln/search"),
        new Endpoint("GET", "/secure/search"),
        new Endpoint("POST", "/vuln/upload/meta"),
        new Endpoint("POST", "/secure/upload/meta")
    };

    private static readonly object RateLock = new object();
    private static readonly Dictionary<string, RateWindow> RateWindows = new Dictionary<string, RateWindow>(StringComparer.Ordinal);

    private const int PermitLimit = 5;
    private const int WindowSeconds = 10;

    private static void Main(string[] args)
    {
        var urlsArg = args.FirstOrDefault(a => a.StartsWith("--urls=", StringComparison.OrdinalIgnoreCase));
        var urls = urlsArg == null
            ? new[] { "http://localhost:5107" }
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
            listener.Prefixes.Add("http://localhost:5107/");
        }

        listener.Start();
        Console.WriteLine("ExposureDefenseLab NET48 compat host listening on: " + string.Join(", ", listener.Prefixes.Cast<string>()));

        while (true)
        {
            var ctx = listener.GetContext();
            try
            {
                if (!AllowRequest(ctx.Request))
                {
                    WriteJson(ctx.Response, 429, "{\"error\":\"rate-limit-exceeded\",\"windowSeconds\":10,\"permitLimit\":5}");
                    continue;
                }

                if (IsBlockedBySecurityFilter(ctx.Request))
                {
                    WriteJson(ctx.Response, 403, "{\"error\":\"Request blocked by security filter.\"}");
                    continue;
                }

                Handle(ctx);
            }
            catch (Exception ex)
            {
                WriteJson(ctx.Response, 500, "{\"error\":\"internal-error\",\"detail\":\"" + Escape(ex.Message) + "\"}");
            }
        }
    }

    private static bool AllowRequest(HttpListenerRequest request)
    {
        var key = request.RemoteEndPoint == null ? "unknown" : request.RemoteEndPoint.Address.ToString();
        var now = DateTime.UtcNow;

        lock (RateLock)
        {
            RateWindow window;
            if (!RateWindows.TryGetValue(key, out window))
            {
                RateWindows[key] = new RateWindow(now, 1);
                return true;
            }

            if ((now - window.StartUtc).TotalSeconds >= WindowSeconds)
            {
                RateWindows[key] = new RateWindow(now, 1);
                return true;
            }

            if (window.Count >= PermitLimit)
            {
                return false;
            }

            window.Count++;
            return true;
        }
    }

    private static bool IsBlockedBySecurityFilter(HttpListenerRequest request)
    {
        var query = request.Url == null ? string.Empty : request.Url.Query;
        var decoded = Uri.UnescapeDataString(query ?? string.Empty).ToLowerInvariant();
        return decoded.Contains("<script") || decoded.Contains("union select") || decoded.Contains("../");
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
                "{\"workshop\":\"07-NET48\",\"application\":\"ExposureDefenseLab\",\"net48Compat\":true,\"modules\":[\"WAF-style filtering\",\"Admin access control\",\"Upload validation\",\"Rate limiting\"]}");
            return;
        }

        if (path == "/vuln/admin/ping")
        {
            WriteJson(ctx.Response, 200,
                "{\"mode\":\"vulnerable\",\"message\":\"Admin endpoint publicly reachable.\"}");
            return;
        }

        if (path == "/secure/admin/ping")
        {
            var expected = Environment.GetEnvironmentVariable("ADMIN_API_KEY") ?? "workshop-admin-key";
            var provided = ctx.Request.Headers["X-Admin-Key"] ?? string.Empty;
            if (!string.Equals(expected, provided, StringComparison.Ordinal))
            {
                WriteJson(ctx.Response, 401, "{\"error\":\"unauthorized\"}");
                return;
            }

            WriteJson(ctx.Response, 200,
                "{\"mode\":\"secure\",\"message\":\"Admin endpoint protected.\"}");
            return;
        }

        if (path == "/vuln/search")
        {
            var q = ReadQuery(ctx, "q", string.Empty);
            WriteJson(ctx.Response, 200, "{\"mode\":\"vulnerable\",\"query\":\"" + Escape(q) + "\"}");
            return;
        }

        if (path == "/secure/search")
        {
            var q = ReadQuery(ctx, "q", string.Empty);
            WriteJson(ctx.Response, 200,
                "{\"mode\":\"secure\",\"query\":\"" + Escape(q) + "\",\"note\":\"If malicious patterns are detected, middleware blocks the request.\"}");
            return;
        }

        var body = ReadBody(ctx.Request);
        var fileName = ReadJsonString(body, "fileName") ?? string.Empty;
        var contentType = ReadJsonString(body, "contentType") ?? string.Empty;
        var sizeText = ReadJsonNumber(body, "size") ?? "0";

        long size;
        if (!long.TryParse(sizeText, out size))
        {
            size = 0;
        }

        if (path == "/vuln/upload/meta")
        {
            WriteJson(ctx.Response, 200,
                "{\"mode\":\"vulnerable\",\"accepted\":true,\"fileName\":\"" + Escape(fileName) + "\",\"contentType\":\"" + Escape(contentType) + "\",\"size\":" + size + "}");
            return;
        }

        var allowedTypes = new[] { "image/png", "image/jpeg", "application/pdf" };
        if (!allowedTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
        {
            WriteJson(ctx.Response, 400, "{\"error\":\"File type is not allowed.\"}");
            return;
        }

        if (size <= 0 || size > 5000000)
        {
            WriteJson(ctx.Response, 400, "{\"error\":\"Invalid file size.\"}");
            return;
        }

        if (fileName.Contains("..") || fileName.Contains("/") || fileName.Contains("\\"))
        {
            WriteJson(ctx.Response, 400, "{\"error\":\"Invalid file name.\"}");
            return;
        }

        WriteJson(ctx.Response, 200,
            "{\"mode\":\"secure\",\"accepted\":true,\"fileName\":\"" + Escape(fileName) + "\"}");
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

    private static string ReadJsonNumber(string json, string key)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        var m = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*(?<v>-?[0-9]+)", RegexOptions.IgnoreCase);
        return m.Success ? m.Groups["v"].Value : null;
    }

    private static string ReadQuery(HttpListenerContext ctx, string key, string defaultValue)
    {
        var value = ctx.Request.QueryString[key];
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
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

    private sealed class RateWindow
    {
        public RateWindow(DateTime startUtc, int count)
        {
            StartUtc = startUtc;
            Count = count;
        }

        public DateTime StartUtc { get; set; }
        public int Count { get; set; }
    }
}
