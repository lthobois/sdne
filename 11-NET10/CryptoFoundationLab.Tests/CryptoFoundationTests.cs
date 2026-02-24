using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CryptoFoundationLab.Tests;

public sealed class CryptoFoundationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public CryptoFoundationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HashSha256_ShouldReturn64HexCharacters()
    {
        var response = await _client.PostAsJsonAsync("/secure/hash/sha256", new { input = "abc" });
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("BA7816BF", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AesRoundtrip_ShouldReturnOriginalMessage()
    {
        var response = await _client.PostAsJsonAsync("/secure/aes/roundtrip", new { message = "secret-message" });
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("secret-message", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RsaRoundtrip_ShouldReturnOriginalMessage()
    {
        var response = await _client.PostAsJsonAsync("/secure/rsa/roundtrip", new { message = "hello-rsa" });
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("hello-rsa", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CertificateEndpoint_ShouldReturnSubject()
    {
        var response = await _client.PostAsJsonAsync("/secure/cert/self-signed", new { subject = "CN=Lab11" });
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("CN=Lab11", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DpapiRoundtrip_ShouldReturnOkOrNotImplemented()
    {
        var response = await _client.PostAsJsonAsync("/secure/windows/dpapi/roundtrip", new { message = "dpapi" });

        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotImplemented);
    }
}
