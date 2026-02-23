namespace AppSecWorkshop02.Security;

public sealed class SessionStore
{
    private readonly Dictionary<string, SessionInfo> _sessions = new(StringComparer.Ordinal);

    public void Create(string sessionId, string csrfToken, string username)
    {
        _sessions[sessionId] = new SessionInfo(sessionId, csrfToken, username);
    }

    public bool TryGet(string sessionId, out SessionInfo session)
    {
        return _sessions.TryGetValue(sessionId, out session!);
    }
}

public sealed record SessionInfo(string SessionId, string CsrfToken, string Username);
