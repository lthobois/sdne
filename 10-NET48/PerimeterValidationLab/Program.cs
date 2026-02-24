using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

internal static class Program
{
    private static readonly Endpoint[] Endpoints =
    {
        new Endpoint("GET", "/"),
        new Endpoint("GET", "/vuln/links/reset-password"),
        new Endpoint("GET", "/secure/links/reset-password"),
        new Endpoint("GET", "/vuln/tenant/home"),
        new Endpoint("GET", "/secure/tenant/home"),
        new Endpoint("GET", "/secure/diagnostics/request-meta")
    };

    private static readonly HashSet<string> AllowedHosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "app.contoso.local",
        "admin.contoso.local"
    };

    private static void Main(string[] args)
    {
        var urlsArg = args.FirstOrDefault(a => a.StartsWith("--urls=", StringComparison.OrdinalIgnoreCase));
        var urls = urlsArg == null
            ? new[] { "http://localhost:5110" }
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
            listener.Prefixes.Add("http://localhost:5110/");
        }

        listener.Start();
        Console.WriteLine("PerimeterValidationLab NET48 compat host listening on: " + string.Join(", ", listener.Prefixes.Cast<string>()));

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
                "{\"workshop\":\"10-NET48\",\"application\":\"PerimeterValidationLab\",\"net48Compat\":true,\"modules\":[\"Header injection\",\"Forwarded headers hardening\",\"Tenant resolution\"]}");
            return;
        }

        if (path == "/vuln/links/reset-password")
        {
            var user = ReadQuery(ctx, "user", "anonymous");
            var host = FirstNonEmpty(ctx.Request.Headers["X-Forwarded-Host"], ctx.Request.UserHostName, "localhost");
            var scheme = FirstNonEmpty(ctx.Request.Headers["X-Forwarded-Proto"], "http");
            var resetLink = scheme + "://" + StripPort(host) + "/reset?user=" + Uri.EscapeDataString(user);

            WriteJson(ctx.Response, 200,
                "{\"mode\":\"vulnerable\",\"resetLink\":\"" + Escape(resetLink) + "\",\"warning\":\"Host and scheme are trusted from user-controlled headers.\"}");
            return;
        }

        if (path == "/secure/links/reset-password")
        {
            var resolved = ResolveExternalOrigin(ctx.Request);
            if (!resolved.Valid)
            {
                WriteJson(ctx.Response, 400, "{\"error\":\"" + Escape(resolved.Reason) + "\"}");
                return;
            }

            var user = ReadQuery(ctx, "user", "anonymous");
            var resetLink = resolved.Scheme + "://" + resolved.Host + "/reset?user=" + Uri.EscapeDataString(user);
            WriteJson(ctx.Response, 200,
                "{\"mode\":\"secure\",\"resetLink\":\"" + Escape(resetLink) + "\"}");
            return;
        }

        if (path == "/vuln/tenant/home")
        {
            var tenantHost = FirstNonEmpty(ctx.Request.Headers["X-Forwarded-Host"], ctx.Request.UserHostName, "localhost");
            WriteJson(ctx.Response, 200,
                "{\"mode\":\"vulnerable\",\"tenantHost\":\"" + Escape(StripPort(tenantHost)) + "\",\"note\":\"Header injection can force tenant resolution.\"}");
            return;
        }

        if (path == "/secure/tenant/home")
        {
            var resolved = ResolveExternalOrigin(ctx.Request);
            if (!resolved.Valid)
            {
                WriteJson(ctx.Response, 400, "{\"error\":\"" + Escape(resolved.Reason) + "\"}");
                return;
            }

            if (!AllowedHosts.Contains(resolved.Host))
            {
                WriteJson(ctx.Response, 403, "{\"error\":\"unknown-tenant\"}");
                return;
            }

            WriteJson(ctx.Response, 200,
                "{\"mode\":\"secure\",\"tenantHost\":\"" + Escape(resolved.Host) + "\"}");
            return;
        }

        if (path == "/secure/diagnostics/request-meta")
        {
            var resolved = ResolveExternalOrigin(ctx.Request);
            var requestHost = StripPort(FirstNonEmpty(ctx.Request.UserHostName, "localhost"));
            var forwardedHost = StripPort(ctx.Request.Headers["X-Forwarded-Host"] ?? string.Empty);
            var forwardedProto = ctx.Request.Headers["X-Forwarded-Proto"] ?? string.Empty;
            var remoteIp = ctx.Request.RemoteEndPoint == null ? string.Empty : ctx.Request.RemoteEndPoint.Address.ToString();

            WriteJson(ctx.Response, 200,
                "{" +
                "\"remoteIp\":\"" + Escape(remoteIp) + "\"," +
                "\"host\":\"" + Escape(requestHost) + "\"," +
                "\"forwardedHost\":\"" + Escape(forwardedHost) + "\"," +
                "\"forwardedProto\":\"" + Escape(forwardedProto) + "\"," +
                "\"resolved\":{" +
                    "\"valid\":" + (resolved.Valid ? "true" : "false") + "," +
                    "\"host\":\"" + Escape(resolved.Host) + "\"," +
                    "\"scheme\":\"" + Escape(resolved.Scheme) + "\"," +
                    "\"reason\":\"" + Escape(resolved.Reason) + "\"" +
                "}" +
                "}");
        }
    }

    private static OriginResolution ResolveExternalOrigin(HttpListenerRequest request)
    {
        var remoteIp = request.RemoteEndPoint == null ? string.Empty : request.RemoteEndPoint.Address.ToString();
        var fromTrustedProxy = string.IsNullOrWhiteSpace(remoteIp)
                               || string.Equals(remoteIp, "127.0.0.1", StringComparison.Ordinal)
                               || string.Equals(remoteIp, "::1", StringComparison.Ordinal);

        var requestHost = StripPort(FirstNonEmpty(request.UserHostName, "localhost"));
        var requestScheme = "http";

        var candidateHost = requestHost;
        var candidateScheme = requestScheme;

        if (fromTrustedProxy)
        {
            candidateHost = StripPort(FirstNonEmpty(request.Headers["X-Forwarded-Host"], candidateHost));
            candidateScheme = FirstNonEmpty(request.Headers["X-Forwarded-Proto"], candidateScheme);
        }

        if (!AllowedHosts.Contains(candidateHost))
        {
            return OriginResolution.Deny("Host is not in allowlist.");
        }

        if (!string.Equals(candidateScheme, "https", StringComparison.OrdinalIgnoreCase))
        {
            return OriginResolution.Deny("External scheme must be https.");
        }

        return OriginResolution.Allow(candidateHost, "https");
    }

    private static string FirstNonEmpty(params string[] values)
    {
        return values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;
    }

    private static string StripPort(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return string.Empty;
        }

        var value = host.Trim();
        var idx = value.IndexOf(':');
        return idx > 0 ? value.Substring(0, idx) : value;
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

    private sealed class OriginResolution
    {
        public bool Valid { get; private set; }
        public string Host { get; private set; }
        public string Scheme { get; private set; }
        public string Reason { get; private set; }

        private OriginResolution(bool valid, string host, string scheme, string reason)
        {
            Valid = valid;
            Host = host;
            Scheme = scheme;
            Reason = reason;
        }

        public static OriginResolution Allow(string host, string scheme)
        {
            return new OriginResolution(true, host, scheme, string.Empty);
        }

        public static OriginResolution Deny(string reason)
        {
            return new OriginResolution(false, string.Empty, string.Empty, reason);
        }
    }
}