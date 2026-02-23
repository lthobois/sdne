namespace PerimeterValidationLab.Security;

public sealed class TrustedProxyPolicy
{
    private static readonly HashSet<string> AllowedHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "app.contoso.local",
        "admin.contoso.local"
    };

    public OriginResolution ResolveExternalOrigin(HttpContext context)
    {
        // Workshop simplification: trust forwarded headers only from localhost proxy.
        var remote = context.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        var fromTrustedProxy = string.IsNullOrWhiteSpace(remote) || remote is "127.0.0.1" or "::1";

        var forwardedHost = context.Request.Headers["X-Forwarded-Host"].FirstOrDefault();
        var forwardedProto = context.Request.Headers["X-Forwarded-Proto"].FirstOrDefault();

        var candidateHost = context.Request.Host.Value ?? string.Empty;
        var candidateScheme = context.Request.Scheme;

        if (fromTrustedProxy && !string.IsNullOrWhiteSpace(forwardedHost))
        {
            candidateHost = forwardedHost.Trim();
        }

        if (fromTrustedProxy && !string.IsNullOrWhiteSpace(forwardedProto))
        {
            candidateScheme = forwardedProto.Trim();
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

    public bool IsKnownTenant(string host) => AllowedHosts.Contains(host);
}

public sealed record OriginResolution(bool Valid, string Host, string Scheme, string? Reason)
{
    public static OriginResolution Allow(string host, string scheme) => new(true, host, scheme, null);
    public static OriginResolution Deny(string reason) => new(false, string.Empty, string.Empty, reason);
}
