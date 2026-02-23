using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AuthzHardeningLab.Tests;

public sealed class AuthzHardeningTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AuthzHardeningTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SecureDocs_ShouldRejectTamperedToken()
    {
        var issue = await _client.PostAsJsonAsync("/secure/auth/token", new { username = "alice", scope = "docs.read" });
        var payload = JsonDocument.Parse(await issue.Content.ReadAsStringAsync());
        var token = payload.RootElement.GetProperty("token").GetString()!;
        var tampered = token + "aa";

        using var request = new HttpRequestMessage(HttpMethod.Get, "/secure/docs/1");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tampered);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SecureDocs_ShouldEnforceObjectAuthorization()
    {
        var issue = await _client.PostAsJsonAsync("/secure/auth/token", new { username = "alice", scope = "docs.read" });
        var payload = JsonDocument.Parse(await issue.Content.ReadAsStringAsync());
        var token = payload.RootElement.GetProperty("token").GetString()!;

        using var request = new HttpRequestMessage(HttpMethod.Get, "/secure/docs/2");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SecurePublish_ShouldRequireScopeAndOwnership()
    {
        var issueWithoutPublish = await _client.PostAsJsonAsync("/secure/auth/token", new { username = "bob", scope = "docs.read" });
        var tokenWithoutPublish = JsonDocument.Parse(await issueWithoutPublish.Content.ReadAsStringAsync()).RootElement.GetProperty("token").GetString()!;

        using var deniedReq = new HttpRequestMessage(HttpMethod.Post, "/secure/docs/2/publish");
        deniedReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenWithoutPublish);
        var denied = await _client.SendAsync(deniedReq);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);

        var issueWithPublish = await _client.PostAsJsonAsync("/secure/auth/token", new { username = "bob", scope = "docs.read docs.publish" });
        var tokenWithPublish = JsonDocument.Parse(await issueWithPublish.Content.ReadAsStringAsync()).RootElement.GetProperty("token").GetString()!;

        using var okReq = new HttpRequestMessage(HttpMethod.Post, "/secure/docs/2/publish");
        okReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenWithPublish);
        var ok = await _client.SendAsync(okReq);
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
    }

    [Fact]
    public async Task VulnerableDocs_ShouldAllowIdorStyleAccess()
    {
        var response = await _client.GetAsync("/vuln/docs/2?username=alice");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
