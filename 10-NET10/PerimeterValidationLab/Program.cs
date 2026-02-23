using PerimeterValidationLab.Security;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddSingleton<TrustedProxyPolicy>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => Results.Ok(new
{
    workshop = "Atelier 10 - Validation perimetrique",
    modules = new[] { "Header injection", "Forwarded headers hardening", "Proxy capture playbook", "DMZ mapping" }
}));

app.MapGet("/vuln/links/reset-password", (HttpContext context, string user) =>
{
    var host = context.Request.Headers["X-Forwarded-Host"].FirstOrDefault()
               ?? context.Request.Host.Value;
    var proto = context.Request.Headers["X-Forwarded-Proto"].FirstOrDefault()
                ?? context.Request.Scheme;

    var resetLink = $"{proto}://{host}/reset?user={Uri.EscapeDataString(user)}";
    return Results.Ok(new
    {
        mode = "vulnerable",
        resetLink,
        warning = "Host and scheme are trusted from user-controlled headers."
    });
});

app.MapGet("/secure/links/reset-password", (HttpContext context, string user, TrustedProxyPolicy policy) =>
{
    var resolved = policy.ResolveExternalOrigin(context);
    if (!resolved.Valid)
    {
        return Results.BadRequest(new { error = resolved.Reason });
    }

    var resetLink = $"{resolved.Scheme}://{resolved.Host}/reset?user={Uri.EscapeDataString(user)}";
    return Results.Ok(new
    {
        mode = "secure",
        resetLink
    });
});

app.MapGet("/vuln/tenant/home", (HttpContext context) =>
{
    var tenantHost = context.Request.Headers["X-Forwarded-Host"].FirstOrDefault()
                     ?? context.Request.Host.Value;
    return Results.Ok(new
    {
        mode = "vulnerable",
        tenantHost,
        note = "Header injection can force tenant resolution."
    });
});

app.MapGet("/secure/tenant/home", (HttpContext context, TrustedProxyPolicy policy) =>
{
    var resolved = policy.ResolveExternalOrigin(context);
    if (!resolved.Valid)
    {
        return Results.BadRequest(new { error = resolved.Reason });
    }

    if (!policy.IsKnownTenant(resolved.Host))
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    return Results.Ok(new
    {
        mode = "secure",
        tenantHost = resolved.Host
    });
});

app.MapGet("/secure/diagnostics/request-meta", (HttpContext context, TrustedProxyPolicy policy) =>
{
    var resolved = policy.ResolveExternalOrigin(context);
    return Results.Ok(new
    {
        remoteIp = context.Connection.RemoteIpAddress?.ToString(),
        host = context.Request.Host.Value,
        forwardedHost = context.Request.Headers["X-Forwarded-Host"].ToString(),
        forwardedProto = context.Request.Headers["X-Forwarded-Proto"].ToString(),
        resolved.Valid,
        resolved.Host,
        resolved.Scheme,
        resolved.Reason
    });
});

app.Run();

public partial class Program;
