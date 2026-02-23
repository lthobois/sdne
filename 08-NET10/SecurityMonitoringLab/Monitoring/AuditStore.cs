namespace SecurityMonitoringLab.Monitoring;

public sealed class AuditStore
{
    private readonly List<AuditEvent> _events = [];

    public void Append(AuditEvent auditEvent)
    {
        _events.Add(auditEvent);
    }

    public IReadOnlyList<AuditEvent> GetAll() => _events.AsReadOnly();
}

public sealed record AuditEvent(
    DateTimeOffset Timestamp,
    string EventType,
    string Username,
    string CorrelationId);
