using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ExposureDefenseLab.Tests;

public sealed class ExposureDefenseTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ExposureDefenseTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SecureAdmin_ShouldRequireApiKey()
    {
        var response = await _client.GetAsync("/secure/admin/ping");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SecureAdmin_WithApiKey_ShouldSucceed()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/secure/admin/ping");
        request.Headers.Add("X-Admin-Key", "workshop-admin-key");

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task WafMiddleware_ShouldBlockScriptPattern()
    {
        var response = await _client.GetAsync("/secure/search?q=<script>alert(1)</script>");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SecureUpload_ShouldRejectExecutableContentType()
    {
        var payload = new
        {
            fileName = "payload.exe",
            contentType = "application/x-msdownload",
            size = 1234
        };

        var response = await _client.PostAsJsonAsync("/secure/upload/meta", payload);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
