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
        new Endpoint("GET", "/vuln/config/secret"),
        new Endpoint("GET", "/secure/config/secret"),
        new Endpoint("GET", "/vuln/outbound/fetch"),
        new Endpoint("GET", "/secure/outbound/fetch"),
        new Endpoint("POST", "/vuln/dependency/approve"),
        new Endpoint("POST", "/secure/dependency/approve"),
        new Endpoint("POST", "/secure/dependency/sha256")
    };

    private static readonly HashSet<string> TrustedHosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "api.nuget.org",
        "www.nuget.org",
        "jsonplaceholder.typicode.com"
    };

    private static readonly HashSet<string> ApprovedPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Newtonsoft.Json",
        "Serilog",
        "Polly"
    };

    private static void Main(string[] args)
    {
        var urlsArg = args.FirstOrDefault(a => a.StartsWith("--urls=", StringComparison.OrdinalIgnoreCase));
        var urls = urlsArg == null
            ? new[] { "http://localhost:5106" }
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
            listener.Prefixes.Add("http://localhost:5106/");
        }

        listener.Start();
        Console.WriteLine("SupplyChainSecurityLab NET48 compat host listening on: " + string.Join(", ", listener.Prefixes.Cast<string>()));

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
                "{\"workshop\":\"06-NET48\",\"application\":\"SupplyChainSecurityLab\",\"net48Compat\":true,\"modules\":[\"Secrets\",\"Outbound API\",\"Dependency provenance\",\"SBOM/SCA\"]}");
            return;
        }

        if (path == "/vuln/config/secret")
        {
            WriteJson(ctx.Response, 200, "{\"mode\":\"vulnerable\",\"externalApiKey\":\"dev-hardcoded-api-key\"}");
            return;
        }

        if (path == "/secure/config/secret")
        {
            var key = Environment.GetEnvironmentVariable("UPSTREAM_API_KEY");
            var configured = !string.IsNullOrWhiteSpace(key);
            WriteJson(ctx.Response, 200,
                "{\"mode\":\"secure\",\"keyConfigured\":" + (configured ? "true" : "false") + ",\"source\":\"environment_or_secret_store\"}");
            return;
        }

        if (path == "/vuln/outbound/fetch")
        {
            var url = ReadQuery(ctx, "url", string.Empty);
            int status;
            string body;
            string error;
            if (!TryFetch(url, out status, out body, out error))
            {
                WriteJson(ctx.Response, 502, "{\"mode\":\"vulnerable\",\"url\":\"" + Escape(url) + "\",\"error\":\"" + Escape(error) + "\"}");
                return;
            }

            var excerpt = body.Substring(0, Math.Min(200, body.Length));
            WriteJson(ctx.Response, 200,
                "{\"mode\":\"vulnerable\",\"url\":\"" + Escape(url) + "\",\"status\":" + status + ",\"excerpt\":\"" + Escape(excerpt) + "\"}");
            return;
        }

        if (path == "/secure/outbound/fetch")
        {
            var url = ReadQuery(ctx, "url", string.Empty);
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
            {
                WriteJson(ctx.Response, 400, "{\"error\":\"Invalid URL.\"}");
                return;
            }

            string reason;
            if (!ValidateOutboundUri(uri, out reason))
            {
                WriteJson(ctx.Response, 400, "{\"error\":\"" + Escape(reason) + "\"}");
                return;
            }

            int status;
            string body;
            string error;
            if (!TryFetch(uri.ToString(), out status, out body, out error))
            {
                WriteJson(ctx.Response, 502, "{\"mode\":\"secure\",\"url\":\"" + Escape(uri.ToString()) + "\",\"error\":\"" + Escape(error) + "\"}");
                return;
            }

            var excerpt = body.Substring(0, Math.Min(200, body.Length));
            WriteJson(ctx.Response, 200,
                "{\"mode\":\"secure\",\"status\":" + status + ",\"url\":\"" + Escape(uri.ToString()) + "\",\"excerpt\":\"" + Escape(excerpt) + "\"}");
            return;
        }

        var requestBody = ReadBody(ctx.Request);

        if (path == "/vuln/dependency/approve")
        {
            var packageId = ReadJsonString(requestBody, "packageId") ?? string.Empty;
            var sourceUrl = ReadJsonString(requestBody, "sourceUrl") ?? string.Empty;
            var sha256 = ReadJsonString(requestBody, "sha256") ?? string.Empty;

            WriteJson(ctx.Response, 200,
                "{\"mode\":\"vulnerable\",\"approved\":true,\"reason\":\"No provenance verification.\",\"packageId\":\"" + Escape(packageId) + "\",\"sourceUrl\":\"" + Escape(sourceUrl) + "\",\"sha256\":\"" + Escape(sha256) + "\"}");
            return;
        }

        if (path == "/secure/dependency/approve")
        {
            var packageId = ReadJsonString(requestBody, "packageId") ?? string.Empty;
            var sourceUrl = ReadJsonString(requestBody, "sourceUrl") ?? string.Empty;
            var sha256 = ReadJsonString(requestBody, "sha256") ?? string.Empty;

            if (!Regex.IsMatch(packageId, "^[A-Za-z0-9_.-]{3,80}$"))
            {
                WriteJson(ctx.Response, 400, "{\"error\":\"Invalid package id format.\"}");
                return;
            }

            Uri sourceUri;
            if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out sourceUri))
            {
                WriteJson(ctx.Response, 400, "{\"error\":\"Invalid source URL.\"}");
                return;
            }

            if (!TrustedHosts.Contains(sourceUri.Host))
            {
                WriteJson(ctx.Response, 400, "{\"error\":\"Untrusted source host.\"}");
                return;
            }

            if (!Regex.IsMatch(sha256, "^[a-fA-F0-9]{64}$"))
            {
                WriteJson(ctx.Response, 400, "{\"error\":\"Invalid SHA-256 digest format.\"}");
                return;
            }

            if (!ApprovedPackages.Contains(packageId))
            {
                WriteJson(ctx.Response, 400, "{\"error\":\"Package not in approved allowlist.\"}");
                return;
            }

            WriteJson(ctx.Response, 200,
                "{\"mode\":\"secure\",\"approved\":true,\"controls\":[\"allowlisted package\",\"allowlisted host\",\"sha256 format validated\"]}");
            return;
        }

        if (path == "/secure/dependency/sha256")
        {
            var payload = ReadJsonString(requestBody, "payload") ?? string.Empty;
            string sha;
            using (var hasher = SHA256.Create())
            {
                var digest = hasher.ComputeHash(Encoding.UTF8.GetBytes(payload));
                sha = BitConverter.ToString(digest).Replace("-", string.Empty).ToLowerInvariant();
            }

            WriteJson(ctx.Response, 200, "{\"sha256\":\"" + sha + "\"}");
        }
    }

    private static bool ValidateOutboundUri(Uri uri, out string reason)
    {
        reason = string.Empty;

        if (!string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
        {
            reason = "Only HTTPS URLs are allowed.";
            return false;
        }

        if (!TrustedHosts.Contains(uri.Host))
        {
            reason = "Host is not allowlisted.";
            return false;
        }

        if (uri.IsLoopback)
        {
            reason = "Loopback URLs are blocked.";
            return false;
        }

        return true;
    }

    private static bool TryFetch(string rawUrl, out int statusCode, out string body, out string error)
    {
        statusCode = 0;
        body = string.Empty;
        error = string.Empty;

        try
        {
            var request = (HttpWebRequest)WebRequest.Create(rawUrl);
            request.Method = "GET";
            request.Timeout = 3000;
            request.ReadWriteTimeout = 3000;
            request.UserAgent = "SupplyChainSecurityLab-NET48";

            using (var response = (HttpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream ?? Stream.Null, Encoding.UTF8))
            {
                statusCode = (int)response.StatusCode;
                body = reader.ReadToEnd();
                return true;
            }
        }
        catch (WebException ex)
        {
            var httpResponse = ex.Response as HttpWebResponse;
            if (httpResponse != null)
            {
                statusCode = (int)httpResponse.StatusCode;
                try
                {
                    using (var stream = httpResponse.GetResponseStream())
                    using (var reader = new StreamReader(stream ?? Stream.Null, Encoding.UTF8))
                    {
                        body = reader.ReadToEnd();
                    }
                }
                catch
                {
                    body = string.Empty;
                }
            }

            error = ex.Message;
            return false;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
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
        public Endpoint(string method, string path)
        {
            Method = method;
            Path = path;
        }

        public string Method { get; private set; }
        public string Path { get; private set; }
    }
}
