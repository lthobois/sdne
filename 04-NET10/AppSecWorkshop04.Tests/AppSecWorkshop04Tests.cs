using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AppSecWorkshop04.Tests;

public sealed class AppSecWorkshop04Tests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AppSecWorkshop04Tests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Register_Secure_ShouldRejectWeakPassword_AndAcceptStrongPassword()
    {
        var weak = await _client.PostAsJsonAsync("/secure/register", new { username = "aa", password = "1234" });
        Assert.Equal(HttpStatusCode.BadRequest, weak.StatusCode);

        var strong = await _client.PostAsJsonAsync("/secure/register", new { username = "alice.secure", password = "Str0ng!Passw0rd" });
        Assert.Equal(HttpStatusCode.OK, strong.StatusCode);
    }

    [Fact]
    public async Task Files_Secure_ShouldBlockTraversal()
    {
        var ok = await _client.GetAsync("/secure/files/read?fileName=public-note.txt");
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

        var traversal = await _client.GetAsync($"/secure/files/read?fileName={Uri.EscapeDataString("..\\..\\appsettings.json")}");
        Assert.Equal(HttpStatusCode.BadRequest, traversal.StatusCode);
    }

    [Fact]
    public async Task Redirect_Secure_ShouldRejectExternalUrl_AndAllowRelative()
    {
        var denied = await _client.GetAsync($"/secure/redirect?returnUrl={Uri.EscapeDataString("https://evil.example/phishing")}");
        Assert.Equal(HttpStatusCode.BadRequest, denied.StatusCode);

        var allowed = await _client.GetAsync($"/secure/redirect?returnUrl={Uri.EscapeDataString("/dashboard")}");
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
    }

    [Fact]
    public async Task Errors_Secure_ShouldReturnControlledProblem()
    {
        var response = await _client.GetAsync("/secure/errors/divide-by-zero");
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
        Assert.Contains("application/problem+json", contentType, StringComparison.OrdinalIgnoreCase);
    }
}