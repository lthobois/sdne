using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

internal static class Program
{
    private static readonly Endpoint[] Endpoints =
    {
        new Endpoint("GET", "/"),
        new Endpoint("GET", "/secure/crypto/concepts"),
        new Endpoint("POST", "/secure/hash/sha256"),
        new Endpoint("POST", "/secure/hash/password"),
        new Endpoint("POST", "/secure/aes/roundtrip"),
        new Endpoint("POST", "/secure/rsa/keypair"),
        new Endpoint("POST", "/secure/rsa/roundtrip"),
        new Endpoint("POST", "/secure/windows/dpapi/roundtrip"),
        new Endpoint("POST", "/secure/windows/file-encrypt/demo"),
        new Endpoint("POST", "/secure/cert/self-signed")
    };

    private static void Main(string[] args)
    {
        var urlsArg = args.FirstOrDefault(a => a.StartsWith("--urls=", StringComparison.OrdinalIgnoreCase));
        var urls = urlsArg == null
            ? new[] { "http://localhost:5111" }
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
            listener.Prefixes.Add("http://localhost:5111/");
        }

        listener.Start();
        Console.WriteLine("CryptoFoundationLab NET48 compat host listening on: " + string.Join(", ", listener.Prefixes.Cast<string>()));

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
                "{\"workshop\":\"11-NET48\",\"application\":\"CryptoFoundationLab\",\"net48Compat\":true,\"theme\":\"Chiffrement avec C#\"}");
            return;
        }

        if (path == "/secure/crypto/concepts")
        {
            WriteJson(ctx.Response, 200,
                "{" +
                "\"encryptionVsHash\":\"reversible vs irreversible\"," +
                "\"objectives\":[\"confidentialite\",\"integrite\",\"authentification\"]," +
                "\"families\":[\"symetrique\",\"asymetrique\",\"hybride\"]," +
                "\"hashes\":[\"SHA-256\",\"SHA-3\",\"BLAKE2\"]" +
                "}");
            return;
        }

        if (path == "/secure/hash/sha256")
        {
            var body = ReadBody(ctx.Request);
            var input = ReadJsonString(body, "input") ?? string.Empty;
            byte[] bytes;

            using (var sha256 = SHA256.Create())
            {
                bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            }

            WriteJson(ctx.Response, 200,
                "{\"algorithm\":\"SHA-256\",\"hashHex\":\"" + BytesToHex(bytes) + "\"}");
            return;
        }

        if (path == "/secure/hash/password")
        {
            var body = ReadBody(ctx.Request);
            var password = ReadJsonString(body, "password") ?? string.Empty;
            var salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            byte[] hash;
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 120000, HashAlgorithmName.SHA256))
            {
                hash = pbkdf2.GetBytes(32);
            }

            WriteJson(ctx.Response, 200,
                "{" +
                "\"algorithm\":\"PBKDF2-SHA256\"," +
                "\"iterations\":120000," +
                "\"saltBase64\":\"" + Escape(Convert.ToBase64String(salt)) + "\"," +
                "\"hashBase64\":\"" + Escape(Convert.ToBase64String(hash)) + "\"" +
                "}");
            return;
        }

        if (path == "/secure/aes/roundtrip")
        {
            var body = ReadBody(ctx.Request);
            var message = ReadJsonString(body, "message") ?? string.Empty;

            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.GenerateKey();
                aes.GenerateIV();

                var plainBytes = Encoding.UTF8.GetBytes(message);
                byte[] cipherBytes;
                using (var encryptor = aes.CreateEncryptor())
                {
                    cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                }

                byte[] clearBytes;
                using (var decryptor = aes.CreateDecryptor())
                {
                    clearBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                }

                WriteJson(ctx.Response, 200,
                    "{" +
                    "\"algorithm\":\"AES-256-CBC\"," +
                    "\"keyBase64\":\"" + Escape(Convert.ToBase64String(aes.Key)) + "\"," +
                    "\"ivBase64\":\"" + Escape(Convert.ToBase64String(aes.IV)) + "\"," +
                    "\"ciphertextBase64\":\"" + Escape(Convert.ToBase64String(cipherBytes)) + "\"," +
                    "\"decryptedText\":\"" + Escape(Encoding.UTF8.GetString(clearBytes)) + "\"" +
                    "}");
            }

            return;
        }

        if (path == "/secure/rsa/keypair")
        {
            using (var rsa = RSA.Create(2048))
            {
                var publicParameters = rsa.ExportParameters(false);
                var privateParameters = rsa.ExportParameters(true);

                WriteJson(ctx.Response, 200,
                    "{" +
                    "\"algorithm\":\"RSA-2048\"," +
                    "\"publicKeyBase64\":\"" + Escape(Convert.ToBase64String(publicParameters.Modulus ?? new byte[0])) + "\"," +
                    "\"privateKeyBase64\":\"" + Escape(Convert.ToBase64String(privateParameters.D ?? new byte[0])) + "\"" +
                    "}");
            }

            return;
        }

        if (path == "/secure/rsa/roundtrip")
        {
            var body = ReadBody(ctx.Request);
            var message = ReadJsonString(body, "message") ?? string.Empty;
            using (var rsa = RSA.Create(2048))
            {
                var cipherBytes = rsa.Encrypt(Encoding.UTF8.GetBytes(message), RSAEncryptionPadding.OaepSHA256);
                var clearBytes = rsa.Decrypt(cipherBytes, RSAEncryptionPadding.OaepSHA256);

                WriteJson(ctx.Response, 200,
                    "{" +
                    "\"algorithm\":\"RSA-OAEP-SHA256\"," +
                    "\"ciphertextBase64\":\"" + Escape(Convert.ToBase64String(cipherBytes)) + "\"," +
                    "\"decryptedText\":\"" + Escape(Encoding.UTF8.GetString(clearBytes)) + "\"" +
                    "}");
            }

            return;
        }

        if (path == "/secure/windows/dpapi/roundtrip")
        {
            var body = ReadBody(ctx.Request);
            var message = ReadJsonString(body, "message") ?? string.Empty;
            var plain = Encoding.UTF8.GetBytes(message);
            var protectedBytes = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);
            var clear = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);

            WriteJson(ctx.Response, 200,
                "{" +
                "\"mode\":\"dpapi-current-user\"," +
                "\"encryptedBase64\":\"" + Escape(Convert.ToBase64String(protectedBytes)) + "\"," +
                "\"decryptedText\":\"" + Escape(Encoding.UTF8.GetString(clear)) + "\"" +
                "}");
            return;
        }

        if (path == "/secure/windows/file-encrypt/demo")
        {
            var filePath = Path.Combine(Path.GetTempPath(), "crypto-workshop-efs-demo.txt");
            File.WriteAllText(filePath, "demo-efs-content");

            try
            {
                File.Encrypt(filePath);
                File.Decrypt(filePath);

                WriteJson(ctx.Response, 200,
                    "{" +
                    "\"mode\":\"efs-demo\"," +
                    "\"filePath\":\"" + Escape(filePath) + "\"," +
                    "\"encrypted\":true," +
                    "\"decrypted\":true" +
                    "}");
            }
            catch (Exception ex)
            {
                WriteJson(ctx.Response, 501,
                    "{" +
                    "\"mode\":\"efs-demo\"," +
                    "\"filePath\":\"" + Escape(filePath) + "\"," +
                    "\"error\":\"efs-not-available\"," +
                    "\"detail\":\"" + Escape(ex.Message) + "\"" +
                    "}");
            }

            return;
        }

        if (path == "/secure/cert/self-signed")
        {
            var body = ReadBody(ctx.Request);
            var subject = ReadJsonString(body, "subject");
            if (string.IsNullOrWhiteSpace(subject))
            {
                subject = "CN=CryptoWorkshop";
            }

            using (var rsa = RSA.Create(2048))
            {
                var request = new CertificateRequest(
                    new X500DistinguishedName(subject),
                    rsa,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                request.CertificateExtensions.Add(
                    new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, false));

                using (var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddYears(1)))
                {
                    WriteJson(ctx.Response, 200,
                        "{" +
                        "\"subject\":\"" + Escape(cert.Subject) + "\"," +
                        "\"thumbprint\":\"" + Escape(cert.Thumbprint) + "\"," +
                        "\"hasPrivateKey\":" + (cert.HasPrivateKey ? "true" : "false") +
                        "}");
                }
            }
        }
    }

    private static string ReadBody(HttpListenerRequest request)
    {
        using (var stream = request.InputStream)
        using (var reader = new StreamReader(stream, request.ContentEncoding ?? Encoding.UTF8))
        {
            return reader.ReadToEnd();
        }
    }

    private static string ReadJsonString(string json, string key)
    {
        if (string.IsNullOrWhiteSpace(json) || string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        var pattern = "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"(?<value>(?:\\\\.|[^\"])*)\"";
        var match = Regex.Match(json, pattern, RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return null;
        }

        return Regex.Unescape(match.Groups["value"].Value);
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

    private static string BytesToHex(byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
        {
            sb.Append(b.ToString("X2"));
        }

        return sb.ToString();
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
            var regex = "^" + Regex.Escape(template) + "$";
            return new Regex(regex, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }
    }
}
