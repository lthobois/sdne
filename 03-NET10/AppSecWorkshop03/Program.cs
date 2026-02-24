using AppSecWorkshop03.Data;
using AppSecWorkshop03.Security;
using AppSecWorkshop03.Serialization;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddSingleton<VulnerableSessionService>();
builder.Services.AddSingleton<SecureSessionService>();
builder.Services.AddSingleton<OrderRepository>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => Results.Ok(new
{
    workshop = "Atelier 03 - Attaques avancees",
    modules = new[] { "Session Theft", "Insecure Deserialization", "IDOR" },
    usage = "Tester les endpoints /vuln/* puis /secure/*."
}));

app.MapPost("/vuln/session/login", (LoginRequest request, VulnerableSessionService sessions) =>
{
    var token = sessions.Login(request.Username);
    return Results.Ok(new { mode = "vulnerable", token });
});

app.MapGet("/vuln/session/profile", (string token, VulnerableSessionService sessions) =>
{
    if (!sessions.TryGetUser(token, out var username))
    {
        return Results.Unauthorized();
    }

    return Results.Ok(new
    {
        mode = "vulnerable",
        username,
        warning = "Token previsible et reutilisable."
    });
});

app.MapPost("/secure/session/login", (LoginRequest request, HttpContext httpContext, SecureSessionService sessions) =>
{
    var userAgent = httpContext.Request.Headers.UserAgent.ToString();
    var token = sessions.Login(request.Username, userAgent);
    return Results.Ok(new { mode = "secure", token, expiresInSeconds = 1800 });
});

app.MapGet("/secure/session/profile", (HttpContext httpContext, SecureSessionService sessions) =>
{
    var token = httpContext.Request.Headers["X-Session-Token"].ToString();
    if (string.IsNullOrWhiteSpace(token))
    {
        return Results.Unauthorized();
    }

    var userAgent = httpContext.Request.Headers.UserAgent.ToString();
    if (!sessions.TryValidate(token, userAgent, out var username))
    {
        return Results.Unauthorized();
    }

    return Results.Ok(new
    {
        mode = "secure",
        username
    });
});

app.MapPost("/vuln/deserialization/execute", async (HttpRequest request) =>
{
    using var reader = new StreamReader(request.Body);
    var payload = await reader.ReadToEndAsync();

    var settings = new JsonSerializerSettings
    {
        TypeNameHandling = TypeNameHandling.All
    };

    var obj = JsonConvert.DeserializeObject<object>(payload, settings);
    if (obj is IWorkshopAction action)
    {
        return Results.Ok(new { mode = "vulnerable", result = action.Execute() });
    }

    return Results.BadRequest(new { error = "Payload invalide." });
});

app.MapPost("/secure/deserialization/execute", (SafeActionRequest request) =>
{
    if (!string.Equals(request.Action, "echo", StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new { error = "Action non autorisee." });
    }

    return Results.Ok(new
    {
        mode = "secure",
        result = $"Echo: {request.Message}"
    });
});

app.MapGet("/vuln/idor/orders/{id:int}", (int id, string username, OrderRepository orders) =>
{
    var order = orders.GetById(id);
    if (order is null)
    {
        return Results.NotFound();
    }

    return Results.Ok(new
    {
        mode = "vulnerable",
        requester = username,
        order
    });
});

app.MapGet("/secure/idor/orders/{id:int}", (int id, string username, OrderRepository orders) =>
{
    var order = orders.GetById(id);
    if (order is null)
    {
        return Results.NotFound();
    }

    var requester = orders.GetUser(username);
    if (requester is null)
    {
        return Results.Unauthorized();
    }

    if (!string.Equals(order.Owner, requester.Username, StringComparison.Ordinal) && !requester.IsAdmin)
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    return Results.Ok(new
    {
        mode = "secure",
        requester = requester.Username,
        order
    });
});

app.Run();

public sealed record LoginRequest(string Username);
public sealed record SafeActionRequest(string Action, string Message);
public partial class Program;
