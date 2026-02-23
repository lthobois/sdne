using System.Security.Cryptography;
using System.Text;

namespace AuthzHardeningLab.Security;

public sealed class TokenService
{
    private const string Secret = "workshop-super-secret-signing-key";

    public string Issue(string username, string scope)
    {
        var expiry = DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds();
        var payload = $"{username}|{scope}|{expiry}";
        var signature = Sign(payload);
        return $"{payload}|{signature}";
    }

    public TokenPrincipal? Validate(string token)
    {
        var parts = token.Split('|');
        if (parts.Length != 4)
        {
            return null;
        }

        var username = parts[0];
        var scope = parts[1];
        if (!long.TryParse(parts[2], out var expiryUnix))
        {
            return null;
        }

        var signature = parts[3];
        var payload = $"{username}|{scope}|{expiryUnix}";
        var expectedSignature = Sign(payload);
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(signature),
                Encoding.UTF8.GetBytes(expectedSignature)))
        {
            return null;
        }

        var expiry = DateTimeOffset.FromUnixTimeSeconds(expiryUnix);
        if (DateTimeOffset.UtcNow > expiry)
        {
            return null;
        }

        return new TokenPrincipal(username, scope.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private static string Sign(string payload)
    {
        var key = Encoding.UTF8.GetBytes(Secret);
        var data = Encoding.UTF8.GetBytes(payload);
        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

public sealed record TokenPrincipal(string Username, string[] Scopes)
{
    public bool HasScope(string scope) => Scopes.Contains(scope, StringComparer.Ordinal);
}
