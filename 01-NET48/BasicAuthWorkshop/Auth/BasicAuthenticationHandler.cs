using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace BasicAuthWorkshop.Auth;

public sealed class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Basic";

    private readonly IWorkshopUserStore _users;

    public BasicAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IWorkshopUserStore users) : base(options, logger, encoder)
    {
        _users = users;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authorization))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var header = authorization.ToString();
        if (!header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.Fail("Unsupported authorization scheme."));
        }

        var encoded = header["Basic ".Length..].Trim();

        string username;
        string password;

        try
        {
            var decodedBytes = Convert.FromBase64String(encoded);
            var decodedCredentials = Encoding.UTF8.GetString(decodedBytes);
            var separator = decodedCredentials.IndexOf(':');
            if (separator <= 0)
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid Basic token format."));
            }

            username = decodedCredentials[..separator];
            password = decodedCredentials[(separator + 1)..];
        }
        catch (FormatException)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Base64 value."));
        }

        var user = _users.Validate(username, password);
        if (user is null)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid credentials."));
        }

        var claims = new List<Claim> { new(ClaimTypes.Name, user.Username) };
        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.WWWAuthenticate = "Basic realm=\"BasicAuthWorkshop\"";
        return base.HandleChallengeAsync(properties);
    }
}
