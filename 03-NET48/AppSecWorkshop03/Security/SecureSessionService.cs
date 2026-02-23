using System.Security.Cryptography;

namespace AppSecWorkshop03.Security;

public sealed class SecureSessionService
{
    private readonly Dictionary<string, SessionInfo> _sessions = new(StringComparer.Ordinal);

    public string Login(string username, string userAgent)
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToHexString(tokenBytes);

        _sessions[token] = new SessionInfo(
            username,
            userAgent,
            DateTimeOffset.UtcNow.AddMinutes(30));

        return token;
    }

    public bool TryValidate(string token, string userAgent, out string username)
    {
        username = string.Empty;

        if (!_sessions.TryGetValue(token, out var session))
        {
            return false;
        }

        if (DateTimeOffset.UtcNow > session.ExpiresAt)
        {
            _sessions.Remove(token);
            return false;
        }

        if (!string.Equals(session.UserAgent, userAgent, StringComparison.Ordinal))
        {
            return false;
        }

        username = session.Username;
        return true;
    }
}

public sealed record SessionInfo(string Username, string UserAgent, DateTimeOffset ExpiresAt);
