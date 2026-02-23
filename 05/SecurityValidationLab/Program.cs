using System.Text.Encodings.Web;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'; base-uri 'none';";
    await next();
});

app.MapGet("/", () => Results.Ok(new
{
    workshop = "Atelier 05 - Validation automatisee",
    modules = new[] { "Regression Tests", "SAST", "DAST" }
}));

app.MapGet("/vuln/xss", (string input) =>
{
    var html = $"<html><body><div>{input}</div></body></html>";
    return Results.Content(html, "text/html");
});

app.MapGet("/secure/xss", (string input) =>
{
    var safe = HtmlEncoder.Default.Encode(input);
    var html = $"<html><body><div>{safe}</div></body></html>";
    return Results.Content(html, "text/html");
});

app.MapGet("/vuln/open-redirect", (string returnUrl) => Results.Redirect(returnUrl));

app.MapGet("/secure/open-redirect", (string returnUrl) =>
{
    if (!Uri.TryCreate(returnUrl, UriKind.Relative, out _))
    {
        return Results.BadRequest(new { error = "Only relative returnUrl is allowed." });
    }

    return Results.Ok(new { redirectTarget = returnUrl });
});

app.MapPost("/secure/register", (RegisterRequest request) =>
{
    var errors = new List<string>();

    if (!Regex.IsMatch(request.Username ?? string.Empty, "^[a-zA-Z0-9_.-]{4,30}$"))
    {
        errors.Add("Invalid username format.");
    }

    var password = request.Password ?? string.Empty;
    var strong =
        password.Length >= 12 &&
        password.Any(char.IsUpper) &&
        password.Any(char.IsLower) &&
        password.Any(char.IsDigit) &&
        password.Any(c => !char.IsLetterOrDigit(c));

    if (!strong)
    {
        errors.Add("Weak password.");
    }

    if (errors.Count > 0)
    {
        return Results.BadRequest(new { errors });
    }

    return Results.Ok(new { message = "Account validated." });
});

app.Run();

public sealed record RegisterRequest(string Username, string Password);
public partial class Program;
