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
        new Endpoint("POST", "/vuln/login"),
        new Endpoint("POST", "/secure/login"),
        new Endpoint("GET", "/secure/audit/events"),
        new Endpoint("GET", "/secure/alerts"),
        new Endpoint("POST", "/vuln/admin/reset-alerts"),
        new Endpoint("POST", "/secure/admin/reset-alerts")
    };

    private static readonly object Sync = new object();
    private static readonly List<AuditEvent> AuditEvents = new List<AuditEvent>();
    private static readonly Dictionary<string, int> FailedLogins = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    private static readonly List<string> Alerts = new List<string>();

    private static void Main(string[] args)
    {
        var urlsArg = args.FirstOrDefault(a => a.StartsWith("--urls=", StringComparison.OrdinalIgnoreCase));
        var urls = urlsArg == null
            ? new[] { "http://localhost:5108" }
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
            listener.Prefixes.Add("http://localhost:5108/");
        }

        listener.Start();
        Console.WriteLine("SecurityMonitoringLab NET48 compat host listening on: " + string.Join(", ", listener.Prefixes.Cast<string>()));

        while (true)
        {
            var ctx = listener.GetContext();
            try
            {
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
        var correlationId = ctx.Request.Headers["X-Correlation-ID"];
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("N");
        }
        ctx.Response.Headers["X-Correlation-ID"] = correlationId;

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
                "{\"workshop\":\"08-NET48\",\"application\":\"SecurityMonitoringLab\",\"net48Compat\":true,\"modules\":[\"Correlation ID\",\"Audit trail\",\"Alerting\",\"Safe logging\"]}");
            return;
        }

        var body = ReadBody(ctx.Request);

        if (path == "/vuln/login")
        {
            var username = ReadJsonString(body, "username") ?? string.Empty;
            var password = ReadJsonString(body, "password") ?? string.Empty;

            Console.WriteLine("[WARN] vuln login attempt user=" + username + " password=" + password);

            var authenticated = string.Equals(password, "Password123!", StringComparison.Ordinal);
            WriteJson(ctx.Response, 200,
                "{\"mode\":\"vulnerable\",\"authenticated\":" + (authenticated ? "true" : "false") + "}");
            return;
        }

        if (path == "/secure/login")
        {
            var username = ReadJsonString(body, "username") ?? string.Empty;
            var password = ReadJsonString(body, "password") ?? string.Empty;
            var authenticated = string.Equals(password, "Password123!", StringComparison.Ordinal);

            var eventType = authenticated ? "auth.success" : "auth.failure";
            lock (Sync)
            {
                AuditEvents.Add(new AuditEvent(DateTime.UtcNow, eventType, username, correlationId));

                if (!authenticated)
                {
                    int current;
                    if (!FailedLogins.TryGetValue(username, out current))
                    {
                        current = 0;
                    }

                    current++;
                    FailedLogins[username] = current;

                    if (current >= 3)
                    {
                        var alert = "multiple_failed_logins:" + username;
                        if (!Alerts.Contains(alert, StringComparer.Ordinal))
                        {
                            Alerts.Add(alert);
                        }
                    }
                }
            }

            WriteJson(ctx.Response, 200,
                "{\"mode\":\"secure\",\"authenticated\":" + (authenticated ? "true" : "false") + ",\"correlationId\":\"" + Escape(correlationId) + "\"}");
            return;
        }

        if (path == "/secure/audit/events")
        {
            string eventsJson;
            lock (Sync)
            {
                eventsJson = "[" + string.Join(",", AuditEvents.Select(e =>
                    "{" +
                    "\"timestamp\":\"" + e.TimestampUtc.ToString("o") + "\"," +
                    "\"eventType\":\"" + Escape(e.EventType) + "\"," +
                    "\"username\":\"" + Escape(e.Username) + "\"," +
                    "\"correlationId\":\"" + Escape(e.CorrelationId) + "\"" +
                    "}")) + "]";
            }

            WriteJson(ctx.Response, 200, "{\"events\":" + eventsJson + "}");
            return;
        }

        if (path == "/secure/alerts")
        {
            string alertsJson;
            lock (Sync)
            {
                alertsJson = "[" + string.Join(",", Alerts.Select(a => "\"" + Escape(a) + "\"")) + "]";
            }

            WriteJson(ctx.Response, 200, "{\"alerts\":" + alertsJson + "}");
            return;
        }

        if (path == "/vuln/admin/reset-alerts")
        {
            WriteJson(ctx.Response, 200,
                "{\"mode\":\"vulnerable\",\"message\":\"Alerts reset endpoint exposed without access control.\"}");
            return;
        }

        var expected = Environment.GetEnvironmentVariable("SOC_ADMIN_KEY") ?? "soc-admin-key";
        var provided = ctx.Request.Headers["X-SOC-Key"] ?? string.Empty;
        if (!string.Equals(expected, provided, StringComparison.Ordinal))
        {
            WriteJson(ctx.Response, 401, "{\"error\":\"unauthorized\"}");
            return;
        }

        lock (Sync)
        {
            FailedLogins.Clear();
            Alerts.Clear();
        }

        WriteJson(ctx.Response, 200, "{\"mode\":\"secure\",\"message\":\"Alerts reset done.\"}");
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

    private sealed class AuditEvent
    {
        public AuditEvent(DateTime timestampUtc, string eventType, string username, string correlationId)
        {
            TimestampUtc = timestampUtc;
            EventType = eventType;
            Username = username;
            CorrelationId = correlationId;
        }

        public DateTime TimestampUtc { get; private set; }
        public string EventType { get; private set; }
        public string Username { get; private set; }
        public string CorrelationId { get; private set; }
    }
}
