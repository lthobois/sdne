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
    private const string SigningSecret = "workshop-super-secret-signing-key";

    private static readonly Endpoint[] Endpoints =
    {
        new Endpoint("GET", "/"),
        new Endpoint("POST", "/vuln/auth/token"),
        new Endpoint("POST", "/secure/auth/token"),
        new Endpoint("GET", "/vuln/docs/{id:int}"),
        new Endpoint("GET", "/secure/docs/{id:int}"),
        new Endpoint("POST", "/secure/docs/{id:int}/publish")
    };

    private static readonly List<DocumentRecord> Documents = new List<DocumentRecord>
    {
        new DocumentRecord(1, "alice", "Project Plan", false),
        new DocumentRecord(2, "bob", "Budget", false),
        new DocumentRecord(3, "charlie", "Incident Report", false)
    };

    private static void Main(string[] args)
    {
        var urlsArg = args.FirstOrDefault(a => a.StartsWith("--urls=", StringComparison.OrdinalIgnoreCase));
        var urls = urlsArg == null
            ? new[] { "http://localhost:5109" }
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
            listener.Prefixes.Add("http://localhost:5109/");
        }

        listener.Start();
        Console.WriteLine("AuthzHardeningLab NET48 compat host listening on: " + string.Join(", ", listener.Prefixes.Cast<string>()));

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
                "{\"workshop\":\"09-NET48\",\"application\":\"AuthzHardeningLab\",\"net48Compat\":true,\"modules\":[\"Token integrity\",\"Scope checks\",\"Object-level authorization\"]}");
            return;
        }

        if (path == "/vuln/auth/token")
        {
            var body = ReadBody(ctx.Request);
            var username = ReadJsonString(body, "username") ?? string.Empty;
            var scope = ReadJsonString(body, "scope") ?? string.Empty;
            var token = username + "|" + scope + "|" + DateTime.UtcNow.AddMinutes(30).ToString("o");
            WriteJson(ctx.Response, 200, "{\"mode\":\"vulnerable\",\"token\":\"" + Escape(token) + "\"}");
            return;
        }

        if (path == "/secure/auth/token")
        {
            var body = ReadBody(ctx.Request);
            var username = ReadJsonString(body, "username") ?? string.Empty;
            var scope = ReadJsonString(body, "scope") ?? string.Empty;
            var token = IssueToken(username, scope);
            WriteJson(ctx.Response, 200, "{\"mode\":\"secure\",\"token\":\"" + Escape(token) + "\"}");
            return;
        }

        if (path.StartsWith("/vuln/docs/", StringComparison.OrdinalIgnoreCase))
        {
            var id = ReadPathId(path);
            var requester = ReadQuery(ctx, "username", string.Empty);
            var doc = Documents.FirstOrDefault(d => d.Id == id);
            if (doc == null)
            {
                WriteJson(ctx.Response, 404, "{\"error\":\"not-found\"}");
                return;
            }

            WriteJson(ctx.Response, 200,
                "{\"mode\":\"vulnerable\",\"requester\":\"" + Escape(requester) + "\",\"document\":" + DocToJson(doc) + "}");
            return;
        }

        TokenPrincipal principal;
        if (!TryGetPrincipalFromBearer(ctx.Request, out principal))
        {
            WriteJson(ctx.Response, 401, "{\"error\":\"unauthorized\"}");
            return;
        }

        if (path.StartsWith("/secure/docs/") && method == "GET")
        {
            var id = ReadPathId(path);
            var doc = Documents.FirstOrDefault(d => d.Id == id);
            if (doc == null)
            {
                WriteJson(ctx.Response, 404, "{\"error\":\"not-found\"}");
                return;
            }

            if (!principal.HasScope("docs.read"))
            {
                WriteJson(ctx.Response, 403, "{\"error\":\"forbidden\"}");
                return;
            }

            var canRead = string.Equals(principal.Username, doc.Owner, StringComparison.Ordinal)
                          || principal.HasScope("docs.read.all");
            if (!canRead)
            {
                WriteJson(ctx.Response, 403, "{\"error\":\"forbidden\"}");
                return;
            }

            WriteJson(ctx.Response, 200,
                "{\"mode\":\"secure\",\"requester\":\"" + Escape(principal.Username) + "\",\"document\":" + DocToJson(doc) + "}");
            return;
        }

        var publishId = ReadPathId(path.Replace("/publish", string.Empty));
        var publishDoc = Documents.FirstOrDefault(d => d.Id == publishId);
        if (publishDoc == null)
        {
            WriteJson(ctx.Response, 404, "{\"error\":\"not-found\"}");
            return;
        }

        if (!principal.HasScope("docs.publish"))
        {
            WriteJson(ctx.Response, 403, "{\"error\":\"forbidden\"}");
            return;
        }

        if (!string.Equals(publishDoc.Owner, principal.Username, StringComparison.Ordinal))
        {
            WriteJson(ctx.Response, 403, "{\"error\":\"forbidden\"}");
            return;
        }

        publishDoc.Published = true;
        WriteJson(ctx.Response, 200,
            "{\"mode\":\"secure\",\"message\":\"Document published.\",\"id\":" + publishDoc.Id + "}");
    }

    private static string IssueToken(string username, string scope)
    {
        var expiryUnix = ((DateTimeOffset)DateTime.UtcNow.AddMinutes(30)).ToUnixTimeSeconds();
        var payload = username + "|" + scope + "|" + expiryUnix;
        var signature = Sign(payload);
        return payload + "|" + signature;
    }

    private static bool TryGetPrincipalFromBearer(HttpListenerRequest request, out TokenPrincipal principal)
    {
        principal = null;
        var header = request.Headers["Authorization"] ?? string.Empty;
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var token = header.Substring("Bearer ".Length).Trim();
        principal = ValidateToken(token);
        return principal != null;
    }

    private static TokenPrincipal ValidateToken(string token)
    {
        var parts = token.Split('|');
        if (parts.Length != 4)
        {
            return null;
        }

        var username = parts[0];
        var scope = parts[1];
        long expiryUnix;
        if (!long.TryParse(parts[2], out expiryUnix))
        {
            return null;
        }

        var signature = parts[3];
        var payload = username + "|" + scope + "|" + expiryUnix;
        var expected = Sign(payload);
        if (!FixedTimeEqualsHex(signature, expected))
        {
            return null;
        }

        var expiry = DateTimeOffset.FromUnixTimeSeconds(expiryUnix);
        if (DateTimeOffset.UtcNow > expiry)
        {
            return null;
        }

        var scopes = scope.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        return new TokenPrincipal(username, scopes);
    }

    private static string Sign(string payload)
    {
        var key = Encoding.UTF8.GetBytes(SigningSecret);
        var data = Encoding.UTF8.GetBytes(payload);
        using (var hmac = new HMACSHA256(key))
        {
            var hash = hmac.ComputeHash(data);
            var sb = new StringBuilder(hash.Length * 2);
            for (var i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("x2"));
            }

            return sb.ToString();
        }
    }

    private static bool FixedTimeEqualsHex(string a, string b)
    {
        if (a == null || b == null || a.Length != b.Length)
        {
            return false;
        }

        var diff = 0;
        for (var i = 0; i < a.Length; i++)
        {
            diff |= a[i] ^ b[i];
        }

        return diff == 0;
    }

    private static int ReadPathId(string path)
    {
        var idText = path.Substring(path.LastIndexOf('/') + 1);
        int id;
        return int.TryParse(idText, out id) ? id : -1;
    }

    private static string DocToJson(DocumentRecord doc)
    {
        return "{" +
               "\"id\":" + doc.Id + "," +
               "\"owner\":\"" + Escape(doc.Owner) + "\"," +
               "\"title\":\"" + Escape(doc.Title) + "\"," +
               "\"published\":" + (doc.Published ? "true" : "false") +
               "}";
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

    private sealed class TokenPrincipal
    {
        public TokenPrincipal(string username, string[] scopes)
        {
            Username = username;
            Scopes = scopes;
        }

        public string Username { get; private set; }
        public string[] Scopes { get; private set; }

        public bool HasScope(string scope)
        {
            return Scopes.Contains(scope, StringComparer.Ordinal);
        }
    }

    private sealed class DocumentRecord
    {
        public DocumentRecord(int id, string owner, string title, bool published)
        {
            Id = id;
            Owner = owner;
            Title = title;
            Published = published;
        }

        public int Id { get; private set; }
        public string Owner { get; private set; }
        public string Title { get; private set; }
        public bool Published { get; set; }
    }
}
