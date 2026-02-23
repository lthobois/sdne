namespace SupplyChainSecurityLab.Security;

public sealed class OutboundRequestGuard
{
    public OutboundValidationResult Validate(Uri uri)
    {
        if (uri.Scheme is not ("https"))
        {
            return OutboundValidationResult.Deny("Only HTTPS URLs are allowed.");
        }

        if (!SupplyChainPolicy.TrustedHosts.Contains(uri.Host))
        {
            return OutboundValidationResult.Deny("Host is not allowlisted.");
        }

        if (uri.IsLoopback)
        {
            return OutboundValidationResult.Deny("Loopback URLs are blocked.");
        }

        return OutboundValidationResult.Allow();
    }
}

public sealed record OutboundValidationResult(bool Allowed, string? Reason)
{
    public static OutboundValidationResult Allow() => new(true, null);
    public static OutboundValidationResult Deny(string reason) => new(false, reason);
}
