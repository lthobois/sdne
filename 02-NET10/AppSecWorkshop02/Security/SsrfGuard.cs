using System.Net;
using System.Net.Sockets;

namespace AppSecWorkshop02.Security;

public sealed class SsrfGuard
{
    private static readonly HashSet<string> AllowedHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "example.com",
        "jsonplaceholder.typicode.com"
    };

    public async Task<SsrfValidationResult> ValidateAsync(Uri uri)
    {
        if (uri.Scheme is not ("http" or "https"))
        {
            return SsrfValidationResult.Deny("Schema non autorise.");
        }

        if (!AllowedHosts.Contains(uri.Host))
        {
            return SsrfValidationResult.Deny("Host non autorise.");
        }

        var addresses = await Dns.GetHostAddressesAsync(uri.Host);
        if (addresses.Length == 0)
        {
            return SsrfValidationResult.Deny("Resolution DNS impossible.");
        }

        if (addresses.Any(IsSensitiveIp))
        {
            return SsrfValidationResult.Deny("Adresse IP interne ou sensible detectee.");
        }

        return SsrfValidationResult.Allow();
    }

    private static bool IsSensitiveIp(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip))
        {
            return true;
        }

        if (ip.AddressFamily == AddressFamily.InterNetworkV6 && ip.IsIPv6LinkLocal)
        {
            return true;
        }

        if (ip.AddressFamily != AddressFamily.InterNetwork)
        {
            return false;
        }

        var bytes = ip.GetAddressBytes();

        // RFC1918 + link-local.
        return bytes[0] == 10
               || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
               || (bytes[0] == 192 && bytes[1] == 168)
               || (bytes[0] == 169 && bytes[1] == 254);
    }
}

public sealed record SsrfValidationResult(bool Allowed, string? Reason)
{
    public static SsrfValidationResult Allow() => new(true, null);
    public static SsrfValidationResult Deny(string reason) => new(false, reason);
}
