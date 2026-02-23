using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PerimeterValidationLab.Tests;

public sealed class PerimeterValidationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public PerimeterValidationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task VulnerableResetLink_ShouldTrustForwardedHeaders()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/vuln/links/reset-password?user=alice");
        request.Headers.TryAddWithoutValidation("X-Forwarded-Host", "evil.example");
        request.Headers.TryAddWithoutValidation("X-Forwarded-Proto", "http");

        var response = await _client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("http://evil.example/reset", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SecureResetLink_ShouldRejectUntrustedHost()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/secure/links/reset-password?user=alice");
        request.Headers.Host = "evil.example";

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SecureResetLink_ShouldAcceptAllowlistedHostWithHttps()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/secure/links/reset-password?user=alice");
        request.Headers.Host = "app.contoso.local";
        request.Headers.TryAddWithoutValidation("X-Forwarded-Proto", "https");

        var response = await _client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("https://app.contoso.local/reset", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SecureTenantEndpoint_ShouldRejectUnknownTenant()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/secure/tenant/home");
        request.Headers.Host = "unknown.contoso.local";

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
