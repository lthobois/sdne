using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SecurityValidationLab.Tests;

public sealed class SecurityRegressionTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SecurityRegressionTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SecureXss_ShouldEncodePayload()
    {
        var response = await _client.GetAsync("/secure/xss?input=<script>alert(1)</script>");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("<script>", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("&lt;script&gt;", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SecureOpenRedirect_ShouldRejectAbsoluteUrls()
    {
        var response = await _client.GetAsync("/secure/open-redirect?returnUrl=https://evil.example");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SecureRegister_ShouldRejectWeakPassword()
    {
        var payload = new { username = "alice", password = "weak" };
        var response = await _client.PostAsJsonAsync("/secure/register", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SecurityHeaders_ShouldBePresent()
    {
        var response = await _client.GetAsync("/");

        Assert.True(response.Headers.TryGetValues("X-Content-Type-Options", out var nosniffValues));
        Assert.Contains("nosniff", nosniffValues);
        Assert.True(response.Headers.Contains("X-Frame-Options"));
        Assert.True(response.Headers.Contains("Content-Security-Policy"));
    }
}
