using BasicAuthWorkshop.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton<IWorkshopUserStore, InMemoryWorkshopUserStore>();

builder.Services
    .AddAuthentication(BasicAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>(
        BasicAuthenticationHandler.SchemeName,
        _ => { });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Admin");
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new
{
    workshop = "Atelier 01 - HTTP Basic",
    message = "Utilisez /public sans authentification et /secure/profile avec Basic Auth."
}));

app.MapGet("/public", () => Results.Ok(new
{
    resource = "public",
    info = "Aucune authentification requise."
}));

app.MapGet("/secure/profile", [Authorize] (HttpContext httpContext) =>
{
    var user = httpContext.User;
    return Results.Ok(new
    {
        resource = "secure/profile",
        user = user.Identity?.Name,
        roles = user.Claims
            .Where(c => c.Type == "role" || c.Type.EndsWith("/role", StringComparison.OrdinalIgnoreCase))
            .Select(c => c.Value)
            .ToArray()
    });
});

app.MapGet("/secure/admin", [Authorize(Policy = "AdminOnly")] () => Results.Ok(new
{
    resource = "secure/admin",
    message = "Acces autorise pour le role Admin."
}));

app.Run();

public partial class Program;