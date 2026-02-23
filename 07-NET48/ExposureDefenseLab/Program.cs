using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var key = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: key,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromSeconds(10),
                QueueLimit = 0
            });
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseRateLimiter();

// Simple WAF-like middleware for workshop demo.
app.Use(async (context, next) =>
{
    var query = context.Request.QueryString.Value ?? string.Empty;
    var decodedQuery = Uri.UnescapeDataString(query);
    var lowered = decodedQuery.ToLowerInvariant();
    if (lowered.Contains("<script") || lowered.Contains("union select") || lowered.Contains("../"))
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new { error = "Request blocked by security filter." });
        return;
    }

    await next();
});

app.MapGet("/", () => Results.Ok(new
{
    workshop = "Atelier 07 - Limiter l'exposition",
    modules = new[] { "WAF-style filtering", "Admin access control", "Upload validation", "Rate limiting" }
}));

app.MapGet("/vuln/admin/ping", () => Results.Ok(new
{
    mode = "vulnerable",
    message = "Admin endpoint publicly reachable."
}));

app.MapGet("/secure/admin/ping", (HttpContext context, IConfiguration config) =>
{
    var expected = config["ADMIN_API_KEY"] ?? "workshop-admin-key";
    var provided = context.Request.Headers["X-Admin-Key"].ToString();
    if (!string.Equals(expected, provided, StringComparison.Ordinal))
    {
        return Results.Unauthorized();
    }

    return Results.Ok(new
    {
        mode = "secure",
        message = "Admin endpoint protected."
    });
});

app.MapGet("/vuln/search", (string q) => Results.Ok(new
{
    mode = "vulnerable",
    query = q
}));

app.MapGet("/secure/search", (string q) => Results.Ok(new
{
    mode = "secure",
    query = q,
    note = "If malicious patterns are detected, middleware blocks the request."
}));

app.MapPost("/vuln/upload/meta", (UploadMetaRequest request) => Results.Ok(new
{
    mode = "vulnerable",
    accepted = true,
    fileName = request.FileName,
    contentType = request.ContentType,
    size = request.Size
}));

app.MapPost("/secure/upload/meta", (UploadMetaRequest request) =>
{
    var allowedTypes = new[] { "image/png", "image/jpeg", "application/pdf" };
    if (!allowedTypes.Contains(request.ContentType, StringComparer.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new { error = "File type is not allowed." });
    }

    if (request.Size <= 0 || request.Size > 5_000_000)
    {
        return Results.BadRequest(new { error = "Invalid file size." });
    }

    if (request.FileName.Contains("..", StringComparison.Ordinal) || request.FileName.Contains('/', StringComparison.Ordinal) || request.FileName.Contains('\\', StringComparison.Ordinal))
    {
        return Results.BadRequest(new { error = "Invalid file name." });
    }

    return Results.Ok(new
    {
        mode = "secure",
        accepted = true,
        fileName = request.FileName
    });
});

app.Run();

public sealed record UploadMetaRequest(string FileName, string ContentType, long Size);
public partial class Program;
