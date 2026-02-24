using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AppSecWorkshop02.Tests;

public sealed class AppSecWorkshop02Tests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AppSecWorkshop02Tests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
    }

    [Fact]
    public async Task SqlInjection_VulnShouldReturnMoreRowsThanSecure()
    {
        var payload = "alice' OR 1=1 --";
        var vuln = await _client.GetFromJsonAsync<JsonElement>($"/vuln/sql/users?username={Uri.EscapeDataString(payload)}");
        var secure = await _client.GetFromJsonAsync<JsonElement>($"/secure/sql/users?username={Uri.EscapeDataString(payload)}");

        var vulnCount = vuln.GetProperty("users").GetArrayLength();
        var secureCount = secure.GetProperty("users").GetArrayLength();

        Assert.True(vulnCount > secureCount, $"Expected vulnCount > secureCount but got {vulnCount} and {secureCount}.");
    }

    [Fact]
    public async Task Xss_SecureShouldEncodePayload()
    {
        var payload = "<script>alert('xss')</script>";

        var vuln = await _client.GetStringAsync($"/vuln/xss?input={Uri.EscapeDataString(payload)}");
        var secure = await _client.GetStringAsync($"/secure/xss?input={Uri.EscapeDataString(payload)}");

        Assert.Contains("<script>alert('xss')</script>", vuln, StringComparison.Ordinal);
        Assert.Contains("&lt;script&gt;alert(&#x27;xss&#x27;)&lt;/script&gt;", secure, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Csrf_SecureShouldRejectWithoutToken_AndAllowWithToken()
    {
        var login = await _client.PostAsJsonAsync("/auth/login", new { username = "alice" });
        login.EnsureSuccessStatusCode();

        var loginJson = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        var token = loginJson.RootElement.GetProperty("csrfToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));

        var transfer = new { to = "bob", amount = 150 };

        var withoutToken = await _client.PostAsJsonAsync("/secure/csrf/transfer", transfer);
        Assert.Equal(HttpStatusCode.Forbidden, withoutToken.StatusCode);

        using var withTokenRequest = new HttpRequestMessage(HttpMethod.Post, "/secure/csrf/transfer")
        {
            Content = JsonContent.Create(transfer)
        };
        withTokenRequest.Headers.TryAddWithoutValidation("X-CSRF-Token", token);

        var withToken = await _client.SendAsync(withTokenRequest);
        Assert.Equal(HttpStatusCode.OK, withToken.StatusCode);
    }

    [Fact]
    public async Task Ssrf_SecureShouldBlockLocalhost()
    {
        var response = await _client.GetAsync($"/secure/ssrf/fetch?url={Uri.EscapeDataString("http://localhost:5102")}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}