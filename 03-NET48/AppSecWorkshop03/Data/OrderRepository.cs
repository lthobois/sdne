namespace AppSecWorkshop03.Data;

public sealed class OrderRepository
{
    private static readonly List<OrderRecord> Orders =
    [
        new(1001, "alice", 149.90m, "Headphones"),
        new(1002, "charlie", 79.00m, "Webcam"),
        new(1003, "bob", 599.00m, "Laptop")
    ];

    private static readonly List<UserRecord> Users =
    [
        new("alice", false),
        new("bob", true),
        new("charlie", false)
    ];

    public OrderRecord? GetById(int id) => Orders.FirstOrDefault(o => o.Id == id);

    public UserRecord? GetUser(string username) =>
        Users.FirstOrDefault(u => string.Equals(u.Username, username, StringComparison.Ordinal));
}

public sealed record OrderRecord(int Id, string Owner, decimal Amount, string Description);
public sealed record UserRecord(string Username, bool IsAdmin);
