using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AppSecWorkshop03.Tests;

public sealed class AppSecWorkshop03Tests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AppSecWorkshop03Tests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Session_SecureProfile_ShouldRequireTokenAndMatchingUserAgent()
    {
        var loginRequest = new HttpRequestMessage(HttpMethod.Post, "/secure/session/login")
        {
            Content = JsonContent.Create(new { username = "alice" })
        };
        loginRequest.Headers.TryAddWithoutValidation("User-Agent", "WorkshopAgent/1.0");

        var loginResponse = await _client.SendAsync(loginRequest);
        loginResponse.EnsureSuccessStatusCode();

        var token = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync())
            .RootElement.GetProperty("token").GetString();

        using var missingToken = new HttpRequestMessage(HttpMethod.Get, "/secure/session/profile");
        var missingTokenResponse = await _client.SendAsync(missingToken);
        Assert.Equal(HttpStatusCode.Unauthorized, missingTokenResponse.StatusCode);

        using var validProfile = new HttpRequestMessage(HttpMethod.Get, "/secure/session/profile");
        validProfile.Headers.TryAddWithoutValidation("X-Session-Token", token);
        validProfile.Headers.TryAddWithoutValidation("User-Agent", "WorkshopAgent/1.0");

        var validProfileResponse = await _client.SendAsync(validProfile);
        Assert.Equal(HttpStatusCode.OK, validProfileResponse.StatusCode);

        using var wrongUaProfile = new HttpRequestMessage(HttpMethod.Get, "/secure/session/profile");
        wrongUaProfile.Headers.TryAddWithoutValidation("X-Session-Token", token);
        wrongUaProfile.Headers.TryAddWithoutValidation("User-Agent", "AnotherAgent/2.0");

        var wrongUaResponse = await _client.SendAsync(wrongUaProfile);
        Assert.Equal(HttpStatusCode.Unauthorized, wrongUaResponse.StatusCode);
    }

    [Fact]
    public async Task Deserialization_Secure_ShouldOnlyAllowEchoAction()
    {
        var allowed = await _client.PostAsJsonAsync("/secure/deserialization/execute", new { action = "echo", message = "hello" });
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);

        var denied = await _client.PostAsJsonAsync("/secure/deserialization/execute", new { action = "delete-all", message = "x" });
        Assert.Equal(HttpStatusCode.BadRequest, denied.StatusCode);
    }

    [Fact]
    public async Task Idor_Secure_ShouldEnforceOwnershipOrAdmin()
    {
        var denied = await _client.GetAsync("/secure/idor/orders/1002?username=alice");
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);

        var allowed = await _client.GetAsync("/secure/idor/orders/1002?username=bob");
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
    }

    [Fact]
    public async Task VulnerableSession_ShouldAcceptPredictableToken()
    {
        var token = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("alice:workshop-session"));
        var response = await _client.GetAsync($"/vuln/session/profile?token={Uri.EscapeDataString(token)}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}