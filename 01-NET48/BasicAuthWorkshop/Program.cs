using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

internal static class Program
{
    private static readonly Dictionary<string, User> Users = new Dictionary<string, User>(StringComparer.Ordinal)
    {
        { "analyst", new User("analyst", "Passw0rd!", new[] { "User" }) },
        { "admin", new User("admin", "Adm1nPass!", new[] { "User", "Admin" }) }
    };

    private static readonly Endpoint[] Endpoints = new[]
    {
        new Endpoint("GET", "/"),
        new Endpoint("GET", "/public"),
        new Endpoint("GET", "/secure/profile"),
        new Endpoint("GET", "/secure/admin")
    };

    private static void Main(string[] args)
    {
        var urlsArg = args.FirstOrDefault(a => a.StartsWith("--urls=", StringComparison.OrdinalIgnoreCase));
        var urls = urlsArg == null
            ? new[] { "http://localhost:5101" }
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
            listener.Prefixes.Add("http://localhost:5101/");
        }

        listener.Start();
        Console.WriteLine("BasicAuthWorkshop NET48 compat host listening on: " + string.Join(", ", listener.Prefixes.Cast<string>()));

        while (true)
        {
            var ctx = listener.GetContext();
            Handle(ctx);
        }
    }

    private static void Handle(HttpListenerContext ctx)
    {
        var method = ctx.Request.HttpMethod.ToUpperInvariant();
        var path = ctx.Request.Url == null ? "/" : ctx.Request.Url.AbsolutePath;

        var endpoint = Endpoints.FirstOrDefault(e => e.Method == method && e.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
        if (endpoint == null)
        {
            WriteJson(ctx.Response, 404, "{\"error\":\"not-found\"}");
            return;
        }

        if (path == "/")
        {
            WriteJson(ctx.Response, 200, "{\"workshop\":\"01-NET48\",\"application\":\"BasicAuthWorkshop\",\"message\":\"Utilisez /public sans authentification et /secure/profile avec Basic Auth.\"}");
            return;
        }

        if (path == "/public")
        {
            WriteJson(ctx.Response, 200, "{\"resource\":\"public\",\"info\":\"Aucune authentification requise.\"}");
            return;
        }

        User user;
        if (!TryAuthenticateBasic(ctx.Request, out user))
        {
            ctx.Response.Headers["WWW-Authenticate"] = "Basic realm=\"BasicAuthWorkshop\"";
            WriteJson(ctx.Response, 401, "{\"error\":\"unauthorized\"}");
            return;
        }

        if (path == "/secure/profile")
        {
            var rolesJson = string.Join(",", user.Roles.Select(r => "\"" + Escape(r) + "\""));
            WriteJson(ctx.Response, 200, "{\"resource\":\"secure/profile\",\"user\":\"" + Escape(user.Username) + "\",\"roles\":[" + rolesJson + "]}");
            return;
        }

        if (!user.Roles.Contains("Admin", StringComparer.Ordinal))
        {
            WriteJson(ctx.Response, 403, "{\"error\":\"forbidden\",\"requiredRole\":\"Admin\"}");
            return;
        }

        WriteJson(ctx.Response, 200, "{\"resource\":\"secure/admin\",\"message\":\"Acces autorise pour le role Admin.\"}");
    }

    private static bool TryAuthenticateBasic(HttpListenerRequest request, out User user)
    {
        user = null;
        var header = request.Headers["Authorization"];
        if (string.IsNullOrWhiteSpace(header) || !header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string decoded;
        try
        {
            var encoded = header.Substring("Basic ".Length).Trim();
            decoded = Encoding.ASCII.GetString(Convert.FromBase64String(encoded));
        }
        catch
        {
            return false;
        }

        var sep = decoded.IndexOf(':');
        if (sep <= 0)
        {
            return false;
        }

        var username = decoded.Substring(0, sep);
        var password = decoded.Substring(sep + 1);

        User candidate;
        if (!Users.TryGetValue(username, out candidate))
        {
            return false;
        }

        if (!string.Equals(candidate.Password, password, StringComparison.Ordinal))
        {
            return false;
        }

        user = candidate;
        return true;
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
            .Replace("\"", "\\\"");
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
        public User(string username, string password, string[] roles)
        {
            Username = username;
            Password = password;
            Roles = roles;
        }

        public string Username { get; private set; }
        public string Password { get; private set; }
        public string[] Roles { get; private set; }
    }
}
