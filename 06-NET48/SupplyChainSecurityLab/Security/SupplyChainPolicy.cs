namespace SupplyChainSecurityLab.Security;

public static class SupplyChainPolicy
{
    public static readonly HashSet<string> TrustedHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "api.nuget.org",
        "www.nuget.org",
        "jsonplaceholder.typicode.com"
    };

    public static readonly string[] ApprovedPackages =
    [
        "Newtonsoft.Json",
        "Serilog",
        "Polly"
    ];
}
