using System.Text;

namespace AppSecWorkshop03.Security;

public sealed class VulnerableSessionService
{
    public string Login(string username)
    {
        // Intentionally weak and predictable token for workshop demo.
        var raw = $"{username}:workshop-session";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
    }

    public bool TryGetUser(string token, out string username)
    {
        username = string.Empty;

        try
        {
            var raw = Encoding.UTF8.GetString(Convert.FromBase64String(token));
            var parts = raw.Split(':', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2 || !string.Equals(parts[1], "workshop-session", StringComparison.Ordinal))
            {
                return false;
            }

            username = parts[0];
            return !string.IsNullOrWhiteSpace(username);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
