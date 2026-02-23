using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SecurityMonitoringLab.Tests;

public sealed class SecurityMonitoringTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SecurityMonitoringTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SecureLogin_ShouldReturnCorrelationIdHeader()
    {
        var payload = new { username = "alice", password = "bad-password" };
        var response = await _client.PostAsJsonAsync("/secure/login", payload);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Correlation-ID"));
    }

    [Fact]
    public async Task MultipleFailedLogins_ShouldCreateAlert()
    {
        var payload = new { username = "alice", password = "wrong" };
        for (var i = 0; i < 3; i++)
        {
            await _client.PostAsJsonAsync("/secure/login", payload);
        }

        var alertResponse = await _client.GetAsync("/secure/alerts");
        var body = await alertResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, alertResponse.StatusCode);
        Assert.Contains("multiple_failed_logins:alice", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SecureAdminReset_ShouldRequireSocKey()
    {
        var noKey = await _client.PostAsync("/secure/admin/reset-alerts", null);
        Assert.Equal(HttpStatusCode.Unauthorized, noKey.StatusCode);

        using var withKey = new HttpRequestMessage(HttpMethod.Post, "/secure/admin/reset-alerts");
        withKey.Headers.Add("X-SOC-Key", "soc-admin-key");
        var ok = await _client.SendAsync(withKey);

        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
    }

    [Fact]
    public async Task SecureLogin_ShouldWriteAuditEvent()
    {
        var payload = new { username = "alice", password = "Password123!" };
        await _client.PostAsJsonAsync("/secure/login", payload);

        var auditResponse = await _client.GetAsync("/secure/audit/events");
        var body = await auditResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, auditResponse.StatusCode);
        Assert.Contains("auth.success", body, StringComparison.OrdinalIgnoreCase);
    }
}
