namespace SecurityMonitoringLab.Monitoring;

public sealed class SecurityAlertService
{
    private readonly Dictionary<string, int> _failedLoginCount = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _alerts = [];

    public void ReportFailedLogin(string username)
    {
        if (!_failedLoginCount.TryAdd(username, 1))
        {
            _failedLoginCount[username]++;
        }

        if (_failedLoginCount[username] >= 3)
        {
            var alert = $"multiple_failed_logins:{username}";
            if (!_alerts.Contains(alert, StringComparer.Ordinal))
            {
                _alerts.Add(alert);
            }
        }
    }

    public IReadOnlyList<string> GetCurrentAlerts() => _alerts.AsReadOnly();

    public void Reset()
    {
        _failedLoginCount.Clear();
        _alerts.Clear();
    }
}
