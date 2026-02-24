using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

internal static class Program
{
    private static readonly Endpoint[] Endpoints =
    {
        new Endpoint("GET", "/"),
        new Endpoint("POST", "/vuln/session/login"),
        new Endpoint("GET", "/vuln/session/profile"),
        new Endpoint("POST", "/secure/session/login"),
        new Endpoint("GET", "/secure/session/profile"),
        new Endpoint("POST", "/vuln/deserialization/execute"),
        new Endpoint("POST", "/secure/deserialization/execute"),
        new Endpoint("GET", "/vuln/idor/orders/{id:int}"),
        new Endpoint("GET", "/secure/idor/orders/{id:int}")
    };

    private static readonly OrderRecord[] Orders =
    {
        new OrderRecord(1001, "alice", "149.90", "Headphones"),
        new OrderRecord(1002, "charlie", "79.00", "Webcam"),
        new OrderRecord(1003, "bob", "599.00", "Laptop")
    };

    private static readonly UserRecord[] Users =
    {
        new UserRecord("alice", false),
        new UserRecord("bob", true),
        new UserRecord("charlie", false)
    };

    private static readonly Dictionary<string, SecureSessionInfo> SecureSessions = new Dictionary<string, SecureSessionInfo>(StringComparer.Ordinal);

    private static void Main(string[] args)
    {
        var urlsArg = args.FirstOrDefault(a => a.StartsWith("--urls=", StringComparison.OrdinalIgnoreCase));
        var urls = urlsArg == null
            ? new[] { "http://localhost:5103" }
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
            listener.Prefixes.Add("http://localhost:5103/");
        }

        listener.Start();
        Console.WriteLine("AppSecWorkshop03 NET48 compat host listening on: " + string.Join(", ", listener.Prefixes.Cast<string>()));

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
        var method = ctx.Request.HttpMethod.ToUpperInvariant();
        var path = ctx.Request.Url == null ? "/" : ctx.Request.Url.AbsolutePath;

        var endpoint = Endpoints.FirstOrDefault(e => e.Method == method && e.Match(path));
        if (endpoint == null)
        {
            WriteJson(ctx.Response, 404, "{\"error\":\"not-found\",\"method\":\"" + Escape(method) + "\",\"path\":\"" + Escape(path) + "\"}");
            return;
        }

        if (path == "/")
        {
            WriteJson(ctx.Response, 200,
                "{\"workshop\":\"03-NET48\",\"application\":\"AppSecWorkshop03\",\"net48Compat\":true,\"modules\":[\"Session Theft\",\"Insecure Deserialization\",\"IDOR\"],\"usage\":\"Tester les endpoints /vuln/* puis /secure/*.\"}");
            return;
        }

        if (path == "/vuln/session/login")
        {
            var body = ReadBody(ctx.Request);
            var username = ReadJsonString(body, "username") ?? "anonymous";
            var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(username + ":workshop-session"));
            WriteJson(ctx.Response, 200, "{\"mode\":\"vulnerable\",\"token\":\"" + Escape(token) + "\"}");
            return;
        }

        if (path == "/vuln/session/profile")
        {
            var token = ReadQuery(ctx, "token", string.Empty);
            string username;
            if (!TryReadVulnerableToken(token, out username))
            {
                WriteJson(ctx.Response, 401, "{\"error\":\"unauthorized\"}");
                return;
            }

            WriteJson(ctx.Response, 200,
                "{\"mode\":\"vulnerable\",\"username\":\"" + Escape(username) + "\",\"warning\":\"Token previsible et reutilisable.\"}");
            return;
        }

        if (path == "/secure/session/login")
        {
            var body = ReadBody(ctx.Request);
            var username = ReadJsonString(body, "username") ?? "anonymous";
            var userAgent = ctx.Request.Headers["User-Agent"] ?? string.Empty;

            var token = CreateSecureToken();
            SecureSessions[token] = new SecureSessionInfo(username, userAgent, DateTime.UtcNow.AddMinutes(30));

            WriteJson(ctx.Response, 200,
                "{\"mode\":\"secure\",\"token\":\"" + token + "\",\"expiresInSeconds\":1800}");
            return;
        }

        if (path == "/secure/session/profile")
        {
            var token = ctx.Request.Headers["X-Session-Token"] ?? string.Empty;
            var userAgent = ctx.Request.Headers["User-Agent"] ?? string.Empty;

            if (string.IsNullOrWhiteSpace(token))
            {
                WriteJson(ctx.Response, 401, "{\"error\":\"unauthorized\"}");
                return;
            }

            SecureSessionInfo session;
            if (!SecureSessions.TryGetValue(token, out session))
            {
                WriteJson(ctx.Response, 401, "{\"error\":\"unauthorized\"}");
                return;
            }

            if (DateTime.UtcNow > session.ExpiresAtUtc)
            {
                SecureSessions.Remove(token);
                WriteJson(ctx.Response, 401, "{\"error\":\"session-expired\"}");
                return;
            }

            if (!string.Equals(session.UserAgent, userAgent, StringComparison.Ordinal))
            {
                WriteJson(ctx.Response, 401, "{\"error\":\"unauthorized\"}");
                return;
            }

            WriteJson(ctx.Response, 200, "{\"mode\":\"secure\",\"username\":\"" + Escape(session.Username) + "\"}");
            return;
        }

        if (path == "/vuln/deserialization/execute")
        {
            var body = ReadBody(ctx.Request);
            var typeName = ReadJsonString(body, "$type") ?? string.Empty;

            if (typeName.IndexOf("DangerousAction", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var fileName = ReadJsonString(body, "FileName") ?? "owned-by-deserialization.txt";
                var content = ReadJsonString(body, "Content") ?? "Payload deserialize";
                var outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
                File.WriteAllText(outputPath, content);

                WriteJson(ctx.Response, 200,
                    "{\"mode\":\"vulnerable\",\"result\":\"Dangerous action executed.\",\"writtenFile\":\"" + Escape(outputPath) + "\"}");
                return;
            }

            var message = ReadJsonString(body, "Message") ?? ReadJsonString(body, "message") ?? "";
            WriteJson(ctx.Response, 200, "{\"mode\":\"vulnerable\",\"result\":\"Echo: " + Escape(message) + "\"}");
            return;
        }

        if (path == "/secure/deserialization/execute")
        {
            var body = ReadBody(ctx.Request);
            var action = ReadJsonString(body, "action") ?? string.Empty;
            var message = ReadJsonString(body, "message") ?? string.Empty;

            if (!string.Equals(action, "echo", StringComparison.OrdinalIgnoreCase))
            {
                WriteJson(ctx.Response, 400, "{\"error\":\"Action non autorisee.\"}");
                return;
            }

            WriteJson(ctx.Response, 200, "{\"mode\":\"secure\",\"result\":\"Echo: " + Escape(message) + "\"}");
            return;
        }

        if (path.StartsWith("/vuln/idor/orders/", StringComparison.OrdinalIgnoreCase))
        {
            var id = ReadPathId(path);
            var username = ReadQuery(ctx, "username", string.Empty);

            var order = Orders.FirstOrDefault(o => o.Id == id);
            if (order == null)
            {
                WriteJson(ctx.Response, 404, "{\"error\":\"not-found\"}");
                return;
            }

            WriteJson(ctx.Response, 200,
                "{\"mode\":\"vulnerable\",\"requester\":\"" + Escape(username) + "\",\"order\":" + OrderToJson(order) + "}");
            return;
        }

        if (path.StartsWith("/secure/idor/orders/", StringComparison.OrdinalIgnoreCase))
        {
            var id = ReadPathId(path);
            var username = ReadQuery(ctx, "username", string.Empty);

            var order = Orders.FirstOrDefault(o => o.Id == id);
            if (order == null)
            {
                WriteJson(ctx.Response, 404, "{\"error\":\"not-found\"}");
                return;
            }

            var requester = Users.FirstOrDefault(u => string.Equals(u.Username, username, StringComparison.Ordinal));
            if (requester == null)
            {
                WriteJson(ctx.Response, 401, "{\"error\":\"unauthorized\"}");
                return;
            }

            if (!string.Equals(order.Owner, requester.Username, StringComparison.Ordinal) && !requester.IsAdmin)
            {
                WriteJson(ctx.Response, 403, "{\"error\":\"forbidden\"}");
                return;
            }

            WriteJson(ctx.Response, 200,
                "{\"mode\":\"secure\",\"requester\":\"" + Escape(requester.Username) + "\",\"order\":" + OrderToJson(order) + "}");
        }
    }

    private static string OrderToJson(OrderRecord order)
    {
        return "{" +
               "\"id\":" + order.Id + "," +
               "\"owner\":\"" + Escape(order.Owner) + "\"," +
               "\"amount\":" + order.Amount + "," +
               "\"description\":\"" + Escape(order.Description) + "\"" +
               "}";
    }

    private static string CreateSecureToken()
    {
        var bytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }

        var sb = new StringBuilder(bytes.Length * 2);
        for (var i = 0; i < bytes.Length; i++)
        {
            sb.Append(bytes[i].ToString("X2"));
        }

        return sb.ToString();
    }

    private static bool TryReadVulnerableToken(string token, out string username)
    {
        username = null;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            var raw = Encoding.UTF8.GetString(Convert.FromBase64String(token));
            var parts = raw.Split(new[] { ':' }, 2);
            if (parts.Length != 2)
            {
                return false;
            }

            if (!string.Equals(parts[1], "workshop-session", StringComparison.Ordinal))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(parts[0]))
            {
                return false;
            }

            username = parts[0];
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static int ReadPathId(string path)
    {
        int id;
        return int.TryParse(path.Substring(path.LastIndexOf('/') + 1), out id) ? id : -1;
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

    private sealed class SecureSessionInfo
    {
        public SecureSessionInfo(string username, string userAgent, DateTime expiresAtUtc)
        {
            Username = username;
            UserAgent = userAgent;
            ExpiresAtUtc = expiresAtUtc;
        }

        public string Username { get; private set; }
        public string UserAgent { get; private set; }
        public DateTime ExpiresAtUtc { get; private set; }
    }

    private sealed class OrderRecord
    {
        public OrderRecord(int id, string owner, string amount, string description)
        {
            Id = id;
            Owner = owner;
            Amount = amount;
            Description = description;
        }

        public int Id { get; private set; }
        public string Owner { get; private set; }
        public string Amount { get; private set; }
        public string Description { get; private set; }
    }

    private sealed class UserRecord
    {
        public UserRecord(string username, bool isAdmin)
        {
            Username = username;
            IsAdmin = isAdmin;
        }

        public string Username { get; private set; }
        public bool IsAdmin { get; private set; }
    }
}
