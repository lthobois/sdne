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
        new Endpoint("GET", "/vuln/sql/users"),
        new Endpoint("GET", "/secure/sql/users"),
        new Endpoint("GET", "/vuln/xss"),
        new Endpoint("GET", "/secure/xss"),
        new Endpoint("POST", "/auth/login"),
        new Endpoint("POST", "/vuln/csrf/transfer"),
        new Endpoint("POST", "/secure/csrf/transfer"),
        new Endpoint("GET", "/vuln/ssrf/fetch"),
        new Endpoint("GET", "/secure/ssrf/fetch")
    };

    private static readonly User[] Users =
    {
        new User(1, "alice", "User"),
        new User(2, "bob", "Admin"),
        new User(3, "charlie", "User")
    };

    private static readonly Dictionary<string, SessionInfo> Sessions = new Dictionary<string, SessionInfo>(StringComparer.Ordinal);

    private static void Main(string[] args)
    {
        var urlsArg = args.FirstOrDefault(a => a.StartsWith("--urls=", StringComparison.OrdinalIgnoreCase));
        var urls = urlsArg == null
            ? new[] { "http://localhost:5102" }
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
            listener.Prefixes.Add("http://localhost:5102/");
        }

        listener.Start();
        Console.WriteLine("AppSecWorkshop02 NET48 compat host listening on: " + string.Join(", ", listener.Prefixes.Cast<string>()));

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

        var endpoint = Endpoints.FirstOrDefault(e => e.Method == method && e.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
        if (endpoint == null)
        {
            WriteJson(ctx.Response, 404, "{\"error\":\"not-found\",\"method\":\"" + Escape(method) + "\",\"path\":\"" + Escape(path) + "\"}");
            return;
        }

        if (path == "/")
        {
            WriteJson(ctx.Response, 200,
                "{\"workshop\":\"02-NET48\",\"application\":\"AppSecWorkshop02\",\"net48Compat\":true,\"modules\":[\"SQLi\",\"XSS\",\"CSRF\",\"SSRF\"],\"usage\":\"Utilisez /vuln/* puis /secure/* pour comparer.\"}");
            return;
        }

        if (path == "/vuln/sql/users")
        {
            HandleVulnSql(ctx);
            return;
        }

        if (path == "/secure/sql/users")
        {
            HandleSecureSql(ctx);
            return;
        }

        if (path == "/vuln/xss")
        {
            var input = ReadQuery(ctx, "input", string.Empty);
            var html = "<html><body><h2>Commentaire utilisateur</h2><div>" + input + "</div></body></html>";
            WriteHtml(ctx.Response, 200, html);
            return;
        }

        if (path == "/secure/xss")
        {
            var input = ReadQuery(ctx, "input", string.Empty);
            var safe = WebUtility.HtmlEncode(input);
            var html = "<html><body><h2>Commentaire utilisateur</h2><div>" + safe + "</div></body></html>";
            WriteHtml(ctx.Response, 200, html);
            return;
        }

        var body = ReadBody(ctx.Request);

        if (path == "/auth/login")
        {
            var username = ReadJsonString(body, "username") ?? "anonymous";
            var sessionId = Guid.NewGuid().ToString("N");
            var csrfToken = Guid.NewGuid().ToString("N");

            Sessions[sessionId] = new SessionInfo(username, csrfToken);
            ctx.Response.Headers.Add("Set-Cookie", "session-id=" + sessionId + "; path=/; HttpOnly; SameSite=Lax");

            WriteJson(ctx.Response, 200,
                "{\"message\":\"Session creee\",\"username\":\"" + Escape(username) + "\",\"csrfToken\":\"" + csrfToken + "\"}");
            return;
        }

        if (path == "/vuln/csrf/transfer")
        {
            SessionInfo session;
            if (!TryGetSession(ctx.Request, out session))
            {
                WriteJson(ctx.Response, 401, "{\"error\":\"unauthorized\"}");
                return;
            }

            var transfer = ReadTransfer(body);
            WriteJson(ctx.Response, 200,
                "{\"mode\":\"vulnerable\",\"from\":\"" + Escape(session.Username) + "\",\"to\":\"" + Escape(transfer.To) + "\",\"amount\":" + transfer.Amount + ",\"warning\":\"Aucune verification anti-CSRF n'est appliquee.\"}");
            return;
        }

        if (path == "/secure/csrf/transfer")
        {
            SessionInfo session;
            if (!TryGetSession(ctx.Request, out session))
            {
                WriteJson(ctx.Response, 401, "{\"error\":\"unauthorized\"}");
                return;
            }

            var requestToken = ctx.Request.Headers["X-CSRF-Token"];
            if (string.IsNullOrWhiteSpace(requestToken) || !string.Equals(requestToken, session.CsrfToken, StringComparison.Ordinal))
            {
                WriteJson(ctx.Response, 403, "{\"error\":\"invalid-csrf-token\"}");
                return;
            }

            var transfer = ReadTransfer(body);
            WriteJson(ctx.Response, 200,
                "{\"mode\":\"secure\",\"from\":\"" + Escape(session.Username) + "\",\"to\":\"" + Escape(transfer.To) + "\",\"amount\":" + transfer.Amount + "}");
            return;
        }

        if (path == "/vuln/ssrf/fetch")
        {
            var url = ReadQuery(ctx, "url", string.Empty);
            WriteJson(ctx.Response, 200,
                "{\"mode\":\"vulnerable\",\"url\":\"" + Escape(url) + "\",\"excerpt\":\"Simulated outbound fetch (no SSRF guard).\"}");
            return;
        }

        if (path == "/secure/ssrf/fetch")
        {
            var rawUrl = ReadQuery(ctx, "url", string.Empty);
            Uri uri;
            if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out uri))
            {
                WriteJson(ctx.Response, 400, "{\"error\":\"url-invalide\"}");
                return;
            }

            if (!string.Equals(uri.Scheme, "http", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
            {
                WriteJson(ctx.Response, 400, "{\"error\":\"schema-non-autorise\"}");
                return;
            }

            var host = uri.Host.ToLowerInvariant();
            if (host == "localhost" || host == "127.0.0.1" || host == "::1" || host.StartsWith("169.254."))
            {
                WriteJson(ctx.Response, 400, "{\"error\":\"ssrf-blocked\"}");
                return;
            }

            WriteJson(ctx.Response, 200,
                "{\"mode\":\"secure\",\"url\":\"" + Escape(uri.ToString()) + "\",\"excerpt\":\"Simulated outbound fetch after SSRF validation.\"}");
        }
    }

    private static void HandleVulnSql(HttpListenerContext ctx)
    {
        var username = ReadQuery(ctx, "username", string.Empty);
        var query = "SELECT id, username, role FROM users WHERE username = '" + username + "'";

        var injected = username.IndexOf("'", StringComparison.Ordinal) >= 0
                       && username.IndexOf("or", StringComparison.OrdinalIgnoreCase) >= 0;

        var users = injected
            ? Users
            : Users.Where(u => string.Equals(u.Username, username, StringComparison.Ordinal));

        WriteJson(ctx.Response, 200,
            "{\"mode\":\"vulnerable\",\"query\":\"" + Escape(query) + "\",\"users\":" + UsersToJson(users) + "}");
    }

    private static void HandleSecureSql(HttpListenerContext ctx)
    {
        var username = ReadQuery(ctx, "username", string.Empty);
        var users = Users.Where(u => string.Equals(u.Username, username, StringComparison.Ordinal));

        WriteJson(ctx.Response, 200,
            "{\"mode\":\"secure\",\"query\":\"SELECT id, username, role FROM users WHERE username = @username\",\"users\":" + UsersToJson(users) + "}");
    }

    private static string UsersToJson(IEnumerable<User> users)
    {
        return "[" + string.Join(",", users.Select(u => "{\"id\":" + u.Id + ",\"username\":\"" + Escape(u.Username) + "\",\"role\":\"" + Escape(u.Role) + "\"}")) + "]";
    }

    private static string ReadBody(HttpListenerRequest request)
    {
        using (var reader = new StreamReader(request.InputStream, request.ContentEncoding ?? Encoding.UTF8))
        {
            return reader.ReadToEnd();
        }
    }

    private static bool TryGetSession(HttpListenerRequest request, out SessionInfo session)
    {
        session = null;
        var cookieHeader = request.Headers["Cookie"] ?? string.Empty;
        var match = Regex.Match(cookieHeader, @"(?:^|;\s*)session-id=([^;]+)", RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return false;
        }

        var sessionId = match.Groups[1].Value;
        return Sessions.TryGetValue(sessionId, out session);
    }

    private static Transfer ReadTransfer(string body)
    {
        var to = ReadJsonString(body, "to") ?? "unknown";
        var amountRaw = ReadJsonNumber(body, "amount") ?? "0";
        decimal amount;
        if (!decimal.TryParse(amountRaw, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out amount))
        {
            amount = 0;
        }

        return new Transfer(to, amount.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    private static string ReadJsonString(string json, string key)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        var m = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"(?<v>(?:\\\\.|[^\"])*)\"", RegexOptions.IgnoreCase);
        if (!m.Success)
        {
            return null;
        }

        return Regex.Unescape(m.Groups["v"].Value);
    }

    private static string ReadJsonNumber(string json, string key)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        var m = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*(?<v>-?[0-9]+(?:\\.[0-9]+)?)", RegexOptions.IgnoreCase);
        return m.Success ? m.Groups["v"].Value : null;
    }

    private static string ReadQuery(HttpListenerContext ctx, string key, string defaultValue)
    {
        var value = ctx.Request.QueryString[key];
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
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

    private sealed class User
    {
        public User(int id, string username, string role)
        {
            Id = id;
            Username = username;
            Role = role;
        }

        public int Id { get; private set; }
        public string Username { get; private set; }
        public string Role { get; private set; }
    }

    private sealed class SessionInfo
    {
        public SessionInfo(string username, string csrfToken)
        {
            Username = username;
            CsrfToken = csrfToken;
        }

        public string Username { get; private set; }
        public string CsrfToken { get; private set; }
    }

    private sealed class Transfer
    {
        public Transfer(string to, string amount)
        {
            To = to;
            Amount = amount;
        }

        public string To { get; private set; }
        public string Amount { get; private set; }
    }
}
