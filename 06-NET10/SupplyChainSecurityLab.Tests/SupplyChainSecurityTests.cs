using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SupplyChainSecurityLab.Tests;

public sealed class SupplyChainSecurityTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SupplyChainSecurityTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SecureOutbound_ShouldRejectHttpScheme()
    {
        var response = await _client.GetAsync("/secure/outbound/fetch?url=http://example.com");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SecureOutbound_ShouldRejectUntrustedHost()
    {
        var response = await _client.GetAsync("/secure/outbound/fetch?url=https://evil.example");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SecureDependencyApprove_ShouldRejectWeakDigest()
    {
        var payload = new
        {
            packageId = "Polly",
            sourceUrl = "https://api.nuget.org/v3/index.json",
            sha256 = "1234"
        };

        var response = await _client.PostAsJsonAsync("/secure/dependency/approve", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SecureDependencyApprove_ShouldAcceptApprovedPackage()
    {
        var payload = new
        {
            packageId = "Polly",
            sourceUrl = "https://api.nuget.org/v3/index.json",
            sha256 = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
        };

        var response = await _client.PostAsJsonAsync("/secure/dependency/approve", payload);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
