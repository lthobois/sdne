using AuthzHardeningLab.Security;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<DocumentStore>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => Results.Ok(new
{
    workshop = "Atelier 09 - AuthN/AuthZ hardening",
    modules = new[] { "Token integrity", "Scope checks", "Object-level authorization" }
}));

app.MapPost("/vuln/auth/token", (TokenRequest request) => Results.Ok(new
{
    mode = "vulnerable",
    token = $"{request.Username}|{request.Scope}|{DateTimeOffset.UtcNow.AddMinutes(30):O}"
}));

app.MapPost("/secure/auth/token", (TokenRequest request, TokenService tokenService) =>
{
    var token = tokenService.Issue(request.Username, request.Scope);
    return Results.Ok(new { mode = "secure", token });
});

app.MapGet("/vuln/docs/{id:int}", (int id, string username, DocumentStore store) =>
{
    var document = store.GetById(id);
    if (document is null)
    {
        return Results.NotFound();
    }

    return Results.Ok(new
    {
        mode = "vulnerable",
        requester = username,
        document
    });
});

app.MapGet("/secure/docs/{id:int}", (int id, HttpContext context, TokenService tokenService, DocumentStore store) =>
{
    if (!TryGetBearer(context, out var bearer))
    {
        return Results.Unauthorized();
    }

    var principal = tokenService.Validate(bearer);
    if (principal is null)
    {
        return Results.Unauthorized();
    }

    var document = store.GetById(id);
    if (document is null)
    {
        return Results.NotFound();
    }

    if (!principal.HasScope("docs.read"))
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    var canRead = principal.Username.Equals(document.Owner, StringComparison.Ordinal) || principal.HasScope("docs.read.all");
    if (!canRead)
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    return Results.Ok(new
    {
        mode = "secure",
        requester = principal.Username,
        document
    });
});

app.MapPost("/secure/docs/{id:int}/publish", (int id, HttpContext context, TokenService tokenService, DocumentStore store) =>
{
    if (!TryGetBearer(context, out var bearer))
    {
        return Results.Unauthorized();
    }

    var principal = tokenService.Validate(bearer);
    if (principal is null || !principal.HasScope("docs.publish"))
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    var document = store.GetById(id);
    if (document is null)
    {
        return Results.NotFound();
    }

    if (!string.Equals(document.Owner, principal.Username, StringComparison.Ordinal))
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    document.Published = true;
    return Results.Ok(new { mode = "secure", message = "Document published.", id = document.Id });
});

app.Run();

static bool TryGetBearer(HttpContext context, out string token)
{
    token = string.Empty;
    var header = context.Request.Headers.Authorization.ToString();
    if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
    {
        return false;
    }

    token = header["Bearer ".Length..].Trim();
    return token.Length > 0;
}

public sealed record TokenRequest(string Username, string Scope);
public partial class Program;
