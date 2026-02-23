using SecurityMonitoringLab.Monitoring;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddSingleton<AuditStore>();
builder.Services.AddSingleton<SecurityAlertService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-ID"].ToString();
    if (string.IsNullOrWhiteSpace(correlationId))
    {
        correlationId = Guid.NewGuid().ToString("N");
    }

    context.Items["correlation-id"] = correlationId;
    context.Response.Headers["X-Correlation-ID"] = correlationId;

    await next();
});

app.MapGet("/", () => Results.Ok(new
{
    workshop = "Atelier 08 - Monitoring securite",
    modules = new[] { "Correlation ID", "Audit trail", "Alerting", "Safe logging" }
}));

app.MapPost("/vuln/login", (LoginRequest request, ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("VulnerableLogin");

    // Intentional anti-pattern: logs password in plain text.
    logger.LogWarning("login attempt user={User} password={Password}", request.Username, request.Password);

    return Results.Ok(new
    {
        mode = "vulnerable",
        authenticated = string.Equals(request.Password, "Password123!", StringComparison.Ordinal)
    });
});

app.MapPost("/secure/login", (LoginRequest request, HttpContext httpContext, AuditStore auditStore, SecurityAlertService alerts) =>
{
    var correlationId = httpContext.Items["correlation-id"]?.ToString() ?? "n/a";
    var authenticated = string.Equals(request.Password, "Password123!", StringComparison.Ordinal);

    var eventType = authenticated ? "auth.success" : "auth.failure";
    auditStore.Append(new AuditEvent(
        DateTimeOffset.UtcNow,
        eventType,
        request.Username,
        correlationId));

    if (!authenticated)
    {
        alerts.ReportFailedLogin(request.Username);
    }

    return Results.Ok(new
    {
        mode = "secure",
        authenticated,
        correlationId
    });
});

app.MapGet("/secure/audit/events", (AuditStore auditStore) => Results.Ok(new
{
    events = auditStore.GetAll()
}));

app.MapGet("/secure/alerts", (SecurityAlertService alerts) => Results.Ok(new
{
    alerts = alerts.GetCurrentAlerts()
}));

app.MapPost("/vuln/admin/reset-alerts", () => Results.Ok(new
{
    mode = "vulnerable",
    message = "Alerts reset endpoint exposed without access control."
}));

app.MapPost("/secure/admin/reset-alerts", (HttpContext httpContext, SecurityAlertService alerts, IConfiguration config) =>
{
    var expected = config["SOC_ADMIN_KEY"] ?? "soc-admin-key";
    var provided = httpContext.Request.Headers["X-SOC-Key"].ToString();
    if (!string.Equals(expected, provided, StringComparison.Ordinal))
    {
        return Results.Unauthorized();
    }

    alerts.Reset();
    return Results.Ok(new { mode = "secure", message = "Alerts reset done." });
});

app.Run();

public sealed record LoginRequest(string Username, string Password);
public partial class Program;
