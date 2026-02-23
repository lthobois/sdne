namespace BasicAuthWorkshop.Auth;

public sealed class InMemoryWorkshopUserStore : IWorkshopUserStore
{
    // Intentionally simple and in-memory for workshop demonstration.
    private static readonly WorkshopUser[] Users =
    [
        new("alice", "P@ssw0rd!", ["User"]),
        new("bob", "Admin123!", ["User", "Admin"])
    ];

    public WorkshopUser? Validate(string username, string password)
    {
        return Users.FirstOrDefault(u =>
            string.Equals(u.Username, username, StringComparison.Ordinal) &&
            string.Equals(u.Password, password, StringComparison.Ordinal));
    }
}
