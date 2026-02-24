using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "safe-files"));
File.WriteAllText(Path.Combine(app.Environment.ContentRootPath, "safe-files", "public-note.txt"), "Public workshop note.");

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
    workshop = "Atelier 04 - Secure Code et Durcissement",
    modules = new[]
    {
        "Input validation",
        "Path traversal",
        "Open redirect",
        "Error handling"
    }
}));

app.MapPost("/vuln/register", (RegisterRequest request, ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("VulnerableRegister");

    // Intentional anti-pattern: logs secrets and accepts weak input.
    logger.LogWarning("Attempt register username={Username} password={Password}", request.Username, request.Password);

    return Results.Ok(new
    {
        mode = "vulnerable",
        message = "Compte cree (sans validation robuste)."
    });
});

app.MapPost("/secure/register", (RegisterRequest request) =>
{
    var errors = new List<string>();

    if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Length < 4)
    {
        errors.Add("username: minimum 4 caracteres.");
    }

    if (!Regex.IsMatch(request.Username, "^[a-zA-Z0-9_.-]+$"))
    {
        errors.Add("username: caracteres non autorises.");
    }

    if (!PasswordPolicy.IsValid(request.Password))
    {
        errors.Add("password: minimum 12 caracteres avec majuscule, minuscule, chiffre et caractere special.");
    }

    if (errors.Count > 0)
    {
        return Results.BadRequest(new { mode = "secure", errors });
    }

    return Results.Ok(new
    {
        mode = "secure",
        message = "Compte cree avec validation.",
        note = "Ne jamais journaliser les secrets."
    });
});

app.MapGet("/vuln/files/read", (string path, IWebHostEnvironment env) =>
{
    var fullPath = Path.Combine(env.ContentRootPath, path);
    if (!File.Exists(fullPath))
    {
        return Results.NotFound(new { error = "Fichier introuvable." });
    }

    var content = File.ReadAllText(fullPath);
    return Results.Ok(new
    {
        mode = "vulnerable",
        path = fullPath,
        content
    });
});

app.MapGet("/secure/files/read", (string fileName, IWebHostEnvironment env) =>
{
    if (!Regex.IsMatch(fileName, "^[a-zA-Z0-9_.-]+$"))
    {
        return Results.BadRequest(new { mode = "secure", error = "Nom de fichier invalide." });
    }

    var safeFolder = Path.Combine(env.ContentRootPath, "safe-files");
    var fullPath = Path.GetFullPath(Path.Combine(safeFolder, fileName));

    if (!fullPath.StartsWith(safeFolder, StringComparison.Ordinal))
    {
        return Results.BadRequest(new { mode = "secure", error = "Tentative de traversal detectee." });
    }

    if (!File.Exists(fullPath))
    {
        return Results.NotFound(new { mode = "secure", error = "Fichier introuvable." });
    }

    var content = File.ReadAllText(fullPath);
    return Results.Ok(new { mode = "secure", fileName, content });
});

app.MapGet("/vuln/redirect", (string returnUrl) =>
{
    return Results.Redirect(returnUrl);
});

app.MapGet("/secure/redirect", (string returnUrl) =>
{
    if (!Uri.TryCreate(returnUrl, UriKind.Relative, out _))
    {
        return Results.BadRequest(new { mode = "secure", error = "URL externe refusee." });
    }

    return Results.Ok(new
    {
        mode = "secure",
        message = "Redirection validee.",
        target = returnUrl
    });
});

app.MapGet("/vuln/errors/divide-by-zero", () =>
{
    var x = 0;
    var value = 10 / x;
    return Results.Ok(new { value });
});

app.MapGet("/secure/errors/divide-by-zero", () =>
{
    try
    {
        var x = 0;
        var value = 10 / x;
        return Results.Ok(new { value });
    }
    catch
    {
        return Results.Problem(
            title: "Operation failed.",
            detail: "Unexpected arithmetic error.",
            statusCode: StatusCodes.Status500InternalServerError);
    }
});

app.Run();

public sealed record RegisterRequest(string Username, string Password);
public partial class Program;

public static class PasswordPolicy
{
    public static bool IsValid(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 12)
        {
            return false;
        }

        var hasUpper = password.Any(char.IsUpper);
        var hasLower = password.Any(char.IsLower);
        var hasDigit = password.Any(char.IsDigit);
        var hasSpecial = password.Any(c => !char.IsLetterOrDigit(c));

        return hasUpper && hasLower && hasDigit && hasSpecial;
    }
}
