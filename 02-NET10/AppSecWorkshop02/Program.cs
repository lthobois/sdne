using System.Net;
using System.Text.Encodings.Web;
using AppSecWorkshop02.Data;
using AppSecWorkshop02.Security;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<SessionStore>();
builder.Services.AddSingleton<SsrfGuard>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

DbInitializer.Initialize("Data Source=workshop.db");

app.MapGet("/", () => Results.Ok(new
{
    workshop = "Atelier 02 - Vulns web",
    modules = new[] { "SQLi", "XSS", "CSRF", "SSRF" },
    usage = "Utilisez /vuln/* puis /secure/* pour comparer."
}));

app.MapGet("/vuln/sql/users", (string username) =>
{
    var users = new List<object>();
    using var connection = new SqliteConnection("Data Source=workshop.db");
    connection.Open();

    var query = $"SELECT id, username, role FROM users WHERE username = '{username}'";
    using var command = connection.CreateCommand();
    command.CommandText = query;

    using var reader = command.ExecuteReader();
    while (reader.Read())
    {
        users.Add(new
        {
            id = reader.GetInt32(0),
            username = reader.GetString(1),
            role = reader.GetString(2)
        });
    }

    return Results.Ok(new { mode = "vulnerable", query, users });
});

app.MapGet("/secure/sql/users", (string username) =>
{
    var users = new List<object>();
    using var connection = new SqliteConnection("Data Source=workshop.db");
    connection.Open();

    using var command = connection.CreateCommand();
    command.CommandText = "SELECT id, username, role FROM users WHERE username = $username";
    command.Parameters.AddWithValue("$username", username);

    using var reader = command.ExecuteReader();
    while (reader.Read())
    {
        users.Add(new
        {
            id = reader.GetInt32(0),
            username = reader.GetString(1),
            role = reader.GetString(2)
        });
    }

    return Results.Ok(new { mode = "secure", users });
});

app.MapGet("/vuln/xss", (string input) =>
{
    var html = $"""
                <html>
                <body>
                    <h2>Commentaire utilisateur</h2>
                    <div>{input}</div>
                </body>
                </html>
                """;
    return Results.Content(html, "text/html");
});

app.MapGet("/secure/xss", (string input) =>
{
    var safe = HtmlEncoder.Default.Encode(input);
    var html = $"""
                <html>
                <body>
                    <h2>Commentaire utilisateur</h2>
                    <div>{safe}</div>
                </body>
                </html>
                """;
    return Results.Content(html, "text/html");
});

app.MapPost("/auth/login", (LoginRequest request, HttpContext httpContext, SessionStore sessions) =>
{
    var sessionId = Guid.NewGuid().ToString("N");
    var csrfToken = Guid.NewGuid().ToString("N");
    sessions.Create(sessionId, csrfToken, request.Username);

    httpContext.Response.Cookies.Append("session-id", sessionId, new CookieOptions
    {
        HttpOnly = true,
        SameSite = SameSiteMode.Lax
    });

    return Results.Ok(new
    {
        message = "Session creee",
        csrfToken
    });
});

app.MapPost("/vuln/csrf/transfer", (TransferRequest request, HttpContext httpContext, SessionStore sessions) =>
{
    if (!httpContext.Request.Cookies.TryGetValue("session-id", out var sessionId))
    {
        return Results.Unauthorized();
    }

    if (!sessions.TryGet(sessionId, out var session))
    {
        return Results.Unauthorized();
    }

    return Results.Ok(new
    {
        mode = "vulnerable",
        from = session.Username,
        to = request.To,
        amount = request.Amount,
        warning = "Aucune verification anti-CSRF n'est appliquee."
    });
});

app.MapPost("/secure/csrf/transfer", (TransferRequest request, HttpContext httpContext, SessionStore sessions) =>
{
    if (!httpContext.Request.Cookies.TryGetValue("session-id", out var sessionId))
    {
        return Results.Unauthorized();
    }

    if (!sessions.TryGet(sessionId, out var session))
    {
        return Results.Unauthorized();
    }

    var requestToken = httpContext.Request.Headers["X-CSRF-Token"].ToString();
    if (string.IsNullOrWhiteSpace(requestToken) || !string.Equals(requestToken, session.CsrfToken, StringComparison.Ordinal))
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    return Results.Ok(new
    {
        mode = "secure",
        from = session.Username,
        to = request.To,
        amount = request.Amount
    });
});

app.MapGet("/vuln/ssrf/fetch", async (string url, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var body = await client.GetStringAsync(url);
    var excerpt = body.Length > 300 ? body[..300] : body;
    return Results.Ok(new { mode = "vulnerable", url, excerpt });
});

app.MapGet("/secure/ssrf/fetch", async (string url, IHttpClientFactory httpClientFactory, SsrfGuard ssrfGuard) =>
{
    if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
    {
        return Results.BadRequest(new { error = "URL invalide." });
    }

    var validation = await ssrfGuard.ValidateAsync(uri);
    if (!validation.Allowed)
    {
        return Results.BadRequest(new { error = validation.Reason });
    }

    var client = httpClientFactory.CreateClient();
    var body = await client.GetStringAsync(uri);
    var excerpt = body.Length > 300 ? body[..300] : body;
    return Results.Ok(new { mode = "secure", url = uri.ToString(), excerpt });
});

app.Run();

public sealed record LoginRequest(string Username);
public sealed record TransferRequest(string To, decimal Amount);
public partial class Program;
