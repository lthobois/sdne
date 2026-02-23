using System.Diagnostics;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddSingleton<ResourceLimiter>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var trustedDllRoot = Path.Combine(AppContext.BaseDirectory, "trusted-dll");
Directory.CreateDirectory(trustedDllRoot);
var trustedDllPlaceholder = Path.Combine(trustedDllRoot, "safe-demo.dll");
if (!File.Exists(trustedDllPlaceholder))
{
    File.WriteAllText(trustedDllPlaceholder, "placeholder");
}

app.MapGet("/", () => Results.Ok(new
{
    workshop = "Atelier 00 - Rappels securite applicative .NET",
    demos = new[]
    {
        "/runtime/stack-depth",
        "/vuln/clickjacking/page",
        "/secure/clickjacking/page",
        "/vuln/session/login",
        "/secure/session/login",
        "/vuln/resource/cpu",
        "/secure/resource/cpu",
        "/vuln/dll/search-order",
        "/secure/dll/search-order",
        "/secure/assembly/integrity"
    }
}));

app.MapGet("/runtime/stack-depth", (int depth) =>
{
    var frames = CountFrames(depth);
    return Results.Ok(new
    {
        requestedDepth = depth,
        observedFrames = frames,
        safe = true,
        note = "Demonstration controlee de recursion (sans stack overflow)."
    });
});


app.MapGet("/vuln/clickjacking/page", () =>
{
    var html = "<html><body><h2>Zone sensible</h2><button>Transferer</button></body></html>";
    return Results.Content(html, "text/html");
});

app.MapGet("/secure/clickjacking/page", (HttpContext context) =>
{
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Content-Security-Policy"] = "frame-ancestors 'none'; default-src 'self'";

    var html = "<html><body><h2>Zone sensible</h2><button>Transferer</button></body></html>";
    return Results.Content(html, "text/html");
});

app.MapPost("/vuln/session/login", (HttpContext context, LoginRequest request) =>
{
    var session = Guid.NewGuid().ToString("N");
    context.Response.Cookies.Append("session-id", session, new CookieOptions
    {
        HttpOnly = false,
        Secure = false,
        SameSite = SameSiteMode.None
    });

    return Results.Ok(new
    {
        mode = "vulnerable",
        user = request.Username,
        warning = "Cookie session exposable via XSS/canal non protege."
    });
});

app.MapPost("/secure/session/login", (HttpContext context, LoginRequest request) =>
{
    var session = Guid.NewGuid().ToString("N");
    context.Response.Cookies.Append("session-id", session, new CookieOptions
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict
    });

    return Results.Ok(new
    {
        mode = "secure",
        user = request.Username,
        controls = new[] { "HttpOnly", "Secure", "SameSite=Strict" }
    });
});

app.MapGet("/vuln/resource/cpu", (int seconds) =>
{
    if (seconds < 1 || seconds > 5)
    {
        return Results.BadRequest(new { error = "seconds doit etre entre 1 et 5." });
    }

    var sw = Stopwatch.StartNew();
    while (sw.Elapsed < TimeSpan.FromSeconds(seconds))
    {
        _ = Math.Sqrt(Random.Shared.NextDouble() * 1000);
    }

    return Results.Ok(new
    {
        mode = "vulnerable",
        consumedSeconds = seconds,
        warning = "Aucune limitation de charge appliquee."
    });
});

app.MapGet("/secure/resource/cpu", (int seconds, ResourceLimiter limiter) =>
{
    if (seconds < 1 || seconds > 2)
    {
        return Results.BadRequest(new { error = "seconds doit etre entre 1 et 2 en mode secure." });
    }

    if (!limiter.TryEnter())
    {
        return Results.StatusCode(StatusCodes.Status429TooManyRequests);
    }

    try
    {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < TimeSpan.FromSeconds(seconds))
        {
            _ = Math.Sqrt(Random.Shared.NextDouble() * 1000);
        }

        return Results.Ok(new
        {
            mode = "secure",
            consumedSeconds = seconds,
            controls = new[] { "quotas", "duree max", "throttling" }
        });
    }
    finally
    {
        limiter.Exit();
    }
});

app.MapGet("/vuln/dll/search-order", (string dllName) =>
{
    var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
    var firstPathEntry = path.Split(';', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "(empty)";

    return Results.Ok(new
    {
        mode = "vulnerable",
        dllName,
        wouldProbe = firstPathEntry,
        warning = "Recherche dependante de l'environnement (risque DLL hijacking)."
    });
});

app.MapGet("/secure/dll/search-order", (string fullPath) =>
{
    if (!Path.IsPathFullyQualified(fullPath))
    {
        return Results.BadRequest(new { error = "Chemin absolu requis." });
    }

    var normalized = Path.GetFullPath(fullPath);
    var trustedRoot = Path.GetFullPath(trustedDllRoot);

    if (!normalized.StartsWith(trustedRoot, StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new { error = "DLL hors repertoire de confiance." });
    }

    if (!Regex.IsMatch(normalized, @"\.dll$", RegexOptions.IgnoreCase))
    {
        return Results.BadRequest(new { error = "Extension .dll attendue." });
    }

    return Results.Ok(new
    {
        mode = "secure",
        normalized,
        controls = new[] { "chemin absolu", "repertoire de confiance", "validation extension" }
    });
});

app.MapGet("/secure/assembly/integrity", () =>
{
    var assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
    var assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName();

    return Results.Ok(new
    {
        assembly = assemblyName.Name,
        version = assemblyName.Version?.ToString(),
        hasPublicKeyToken = assemblyName.GetPublicKeyToken() is { Length: > 0 },
        note = "Strong name et Authenticode renforcent l'integrite et la provenance."
    });
});

app.Run();

static int CountFrames(int depth)
{
    if (depth == 0)
    {
        return 1;
    }

    return 1 + CountFrames(depth - 1);
}

public sealed record LoginRequest(string Username);

public sealed class ResourceLimiter
{
    private int _active;
    private const int MaxConcurrent = 2;

    public bool TryEnter()
    {
        var newValue = Interlocked.Increment(ref _active);
        if (newValue <= MaxConcurrent)
        {
            return true;
        }

        Interlocked.Decrement(ref _active);
        return false;
    }

    public void Exit() => Interlocked.Decrement(ref _active);
}

public partial class Program;
