using System.Security.Cryptography;
using System.Text.RegularExpressions;
using SupplyChainSecurityLab.Security;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddSingleton<OutboundRequestGuard>();
builder.Services.AddHttpClient("safe-outbound", client =>
{
    client.Timeout = TimeSpan.FromSeconds(3);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => Results.Ok(new
{
    workshop = "Atelier 06 - Securite du code externe",
    modules = new[] { "Secrets", "Outbound API", "Dependency provenance", "SBOM/SCA" }
}));

app.MapGet("/vuln/config/secret", () => Results.Ok(new
{
    mode = "vulnerable",
    externalApiKey = "dev-hardcoded-api-key"
}));

app.MapGet("/secure/config/secret", (IConfiguration configuration) =>
{
    var key = configuration["UPSTREAM_API_KEY"];
    return Results.Ok(new
    {
        mode = "secure",
        keyConfigured = !string.IsNullOrWhiteSpace(key),
        source = "environment_or_secret_store"
    });
});

app.MapGet("/vuln/outbound/fetch", async (string url, IHttpClientFactory clientFactory) =>
{
    var client = clientFactory.CreateClient();
    var body = await client.GetStringAsync(url);
    return Results.Ok(new
    {
        mode = "vulnerable",
        url,
        excerpt = body[..Math.Min(200, body.Length)]
    });
});

app.MapGet("/secure/outbound/fetch", async (string url, IHttpClientFactory clientFactory, OutboundRequestGuard guard) =>
{
    if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
    {
        return Results.BadRequest(new { error = "Invalid URL." });
    }

    var validation = guard.Validate(uri);
    if (!validation.Allowed)
    {
        return Results.BadRequest(new { error = validation.Reason });
    }

    var client = clientFactory.CreateClient("safe-outbound");
    using var request = new HttpRequestMessage(HttpMethod.Get, uri);
    using var response = await client.SendAsync(request);
    var body = await response.Content.ReadAsStringAsync();

    return Results.Ok(new
    {
        mode = "secure",
        status = (int)response.StatusCode,
        url = uri.ToString(),
        excerpt = body[..Math.Min(200, body.Length)]
    });
});

app.MapPost("/vuln/dependency/approve", (DependencyApprovalRequest request) => Results.Ok(new
{
    mode = "vulnerable",
    approved = true,
    reason = "No provenance verification.",
    request.PackageId,
    request.SourceUrl,
    request.Sha256
}));

app.MapPost("/secure/dependency/approve", (DependencyApprovalRequest request) =>
{
    if (!Regex.IsMatch(request.PackageId ?? string.Empty, "^[A-Za-z0-9_.-]{3,80}$"))
    {
        return Results.BadRequest(new { error = "Invalid package id format." });
    }

    if (!Uri.TryCreate(request.SourceUrl, UriKind.Absolute, out var sourceUri))
    {
        return Results.BadRequest(new { error = "Invalid source URL." });
    }

    if (!SupplyChainPolicy.TrustedHosts.Contains(sourceUri.Host))
    {
        return Results.BadRequest(new { error = "Untrusted source host." });
    }

    if (!Regex.IsMatch(request.Sha256 ?? string.Empty, "^[a-fA-F0-9]{64}$"))
    {
        return Results.BadRequest(new { error = "Invalid SHA-256 digest format." });
    }

    var approved = SupplyChainPolicy.ApprovedPackages.Contains(request.PackageId, StringComparer.OrdinalIgnoreCase);
    if (!approved)
    {
        return Results.BadRequest(new { error = "Package not in approved allowlist." });
    }

    return Results.Ok(new
    {
        mode = "secure",
        approved = true,
        controls = new[]
        {
            "allowlisted package",
            "allowlisted host",
            "sha256 format validated"
        }
    });
});

app.MapPost("/secure/dependency/sha256", (ChecksumRequest request) =>
{
    var payloadBytes = System.Text.Encoding.UTF8.GetBytes(request.Payload ?? string.Empty);
    var digestBytes = SHA256.HashData(payloadBytes);
    return Results.Ok(new
    {
        sha256 = Convert.ToHexString(digestBytes).ToLowerInvariant()
    });
});

app.Run();

public sealed record DependencyApprovalRequest(string PackageId, string SourceUrl, string Sha256);
public sealed record ChecksumRequest(string Payload);
public partial class Program;
