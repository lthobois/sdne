namespace BasicAuthWorkshop.Auth;

public sealed record WorkshopUser(string Username, string Password, string[] Roles);
