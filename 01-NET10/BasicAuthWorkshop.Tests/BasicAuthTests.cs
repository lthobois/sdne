using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BasicAuthWorkshop.Tests;

public sealed class BasicAuthTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public BasicAuthTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Public_ShouldReturnOk()
    {
        var response = await _client.GetAsync("/public");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SecureProfile_WithoutCredentials_ShouldReturnUnauthorized()
    {
        var response = await _client.GetAsync("/secure/profile");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("Basic", string.Join(',', response.Headers.WwwAuthenticate.Select(h => h.Scheme)), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SecureProfile_WithAliceCredentials_ShouldReturnOk()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/secure/profile");
        request.Headers.Authorization = BuildBasicHeader("alice", "P@ssw0rd!");

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SecureAdmin_WithAlice_ShouldReturnForbidden_AndWithBob_ShouldReturnOk()
    {
        using var forbiddenRequest = new HttpRequestMessage(HttpMethod.Get, "/secure/admin");
        forbiddenRequest.Headers.Authorization = BuildBasicHeader("alice", "P@ssw0rd!");

        var forbidden = await _client.SendAsync(forbiddenRequest);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        using var okRequest = new HttpRequestMessage(HttpMethod.Get, "/secure/admin");
        okRequest.Headers.Authorization = BuildBasicHeader("bob", "Admin123!");

        var ok = await _client.SendAsync(okRequest);
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
    }

    private static AuthenticationHeaderValue BuildBasicHeader(string username, string password)
    {
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        return new AuthenticationHeaderValue("Basic", token);
    }
}