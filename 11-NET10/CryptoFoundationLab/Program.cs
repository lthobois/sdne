using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

var app = WebApplication.CreateBuilder(args).Build();

app.MapGet("/", () => Results.Ok(new
{
    workshop = "11-NET10",
    application = "CryptoFoundationLab",
    theme = "Chiffrement avec C#",
    agenda = new[]
    {
        "Bases du chiffrement",
        "Fonctions de hash",
        "AES symetrique",
        "RSA asymetrique",
        "DPAPI et File.Encrypt",
        "Generation de cles",
        "Generation de certificats"
    }
}));

app.MapGet("/secure/crypto/concepts", () => Results.Ok(new
{
    encryptionVsHash = "reversible vs irreversible",
    objectives = new[] { "confidentialite", "integrite", "authentification" },
    families = new[] { "symetrique", "asymetrique", "hybride" },
    hashes = new[] { "SHA-256", "SHA-3", "BLAKE2" }
}));

app.MapPost("/secure/hash/sha256", (HashRequest request) =>
{
    var input = request.Input ?? string.Empty;
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
    return Results.Ok(new
    {
        algorithm = "SHA-256",
        inputLength = input.Length,
        hashHex = Convert.ToHexString(bytes)
    });
});

app.MapPost("/secure/hash/password", (PasswordRequest request) =>
{
    var password = request.Password ?? string.Empty;
    var salt = RandomNumberGenerator.GetBytes(16);
    var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 120000, HashAlgorithmName.SHA256, 32);

    return Results.Ok(new
    {
        algorithm = "PBKDF2-SHA256",
        iterations = 120000,
        saltBase64 = Convert.ToBase64String(salt),
        hashBase64 = Convert.ToBase64String(hash)
    });
});

app.MapPost("/secure/aes/roundtrip", (MessageRequest request) =>
{
    var plaintext = request.Message ?? string.Empty;
    using var aes = Aes.Create();
    aes.KeySize = 256;
    aes.GenerateKey();
    aes.GenerateIV();

    var plainBytes = Encoding.UTF8.GetBytes(plaintext);
    byte[] cipherBytes;
    using (var encryptor = aes.CreateEncryptor())
    {
        cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
    }

    byte[] decryptedBytes;
    using (var decryptor = aes.CreateDecryptor())
    {
        decryptedBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
    }

    return Results.Ok(new
    {
        algorithm = "AES-256-CBC",
        keyBase64 = Convert.ToBase64String(aes.Key),
        ivBase64 = Convert.ToBase64String(aes.IV),
        ciphertextBase64 = Convert.ToBase64String(cipherBytes),
        decryptedText = Encoding.UTF8.GetString(decryptedBytes)
    });
});

app.MapPost("/secure/rsa/keypair", () =>
{
    using var rsa = RSA.Create(2048);
    return Results.Ok(new
    {
        algorithm = "RSA-2048",
        publicKeyBase64 = Convert.ToBase64String(rsa.ExportRSAPublicKey()),
        privateKeyBase64 = Convert.ToBase64String(rsa.ExportRSAPrivateKey())
    });
});

app.MapPost("/secure/rsa/roundtrip", (MessageRequest request) =>
{
    var plaintext = request.Message ?? string.Empty;
    using var rsa = RSA.Create(2048);
    var cipher = rsa.Encrypt(Encoding.UTF8.GetBytes(plaintext), RSAEncryptionPadding.OaepSHA256);
    var clear = rsa.Decrypt(cipher, RSAEncryptionPadding.OaepSHA256);

    return Results.Ok(new
    {
        algorithm = "RSA-OAEP-SHA256",
        ciphertextBase64 = Convert.ToBase64String(cipher),
        decryptedText = Encoding.UTF8.GetString(clear)
    });
});

app.MapPost("/secure/windows/dpapi/roundtrip", (MessageRequest request) =>
{
    var plaintext = request.Message ?? string.Empty;

    try
    {
        var data = Encoding.UTF8.GetBytes(plaintext);
        var protectedData = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
        var restored = ProtectedData.Unprotect(protectedData, null, DataProtectionScope.CurrentUser);

        return Results.Ok(new
        {
            mode = "dpapi-current-user",
            encryptedBase64 = Convert.ToBase64String(protectedData),
            decryptedText = Encoding.UTF8.GetString(restored)
        });
    }
    catch (PlatformNotSupportedException ex)
    {
        return Results.Json(new
        {
            mode = "dpapi-current-user",
            error = "platform-not-supported",
            detail = ex.Message
        }, statusCode: StatusCodes.Status501NotImplemented);
    }
});

app.MapPost("/secure/windows/file-encrypt/demo", () =>
{
    var path = Path.Combine(Path.GetTempPath(), "crypto-workshop-efs-demo.txt");
    File.WriteAllText(path, "demo-efs-content");

    try
    {
        File.Encrypt(path);
        File.Decrypt(path);

        return Results.Ok(new
        {
            mode = "efs-demo",
            filePath = path,
            encrypted = true,
            decrypted = true
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new
        {
            mode = "efs-demo",
            filePath = path,
            error = "efs-not-available",
            detail = ex.Message
        }, statusCode: StatusCodes.Status501NotImplemented);
    }
});

app.MapPost("/secure/cert/self-signed", (CertRequest request) =>
{
    var subject = string.IsNullOrWhiteSpace(request.Subject) ? "CN=CryptoWorkshop" : request.Subject.Trim();

    using var rsa = RSA.Create(2048);
    var certificateRequest = new CertificateRequest(
        new X500DistinguishedName(subject),
        rsa,
        HashAlgorithmName.SHA256,
        RSASignaturePadding.Pkcs1);

    certificateRequest.CertificateExtensions.Add(
        new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, false));

    var notBefore = DateTimeOffset.UtcNow.AddMinutes(-5);
    var notAfter = DateTimeOffset.UtcNow.AddYears(1);
    using var cert = certificateRequest.CreateSelfSigned(notBefore, notAfter);

    return Results.Ok(new
    {
        subject = cert.Subject,
        thumbprint = cert.Thumbprint,
        notBefore = cert.NotBefore,
        notAfter = cert.NotAfter,
        hasPrivateKey = cert.HasPrivateKey
    });
});

app.Run();

public partial class Program;

public sealed record HashRequest(string? Input);
public sealed record PasswordRequest(string? Password);
public sealed record MessageRequest(string? Message);
public sealed record CertRequest(string? Subject);
