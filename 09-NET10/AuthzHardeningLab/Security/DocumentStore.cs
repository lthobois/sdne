namespace AuthzHardeningLab.Security;

public sealed class DocumentStore
{
    private readonly List<DocumentRecord> _documents =
    [
        new() { Id = 1, Owner = "alice", Title = "Project Plan", Published = false },
        new() { Id = 2, Owner = "bob", Title = "Budget", Published = false },
        new() { Id = 3, Owner = "charlie", Title = "Incident Report", Published = false }
    ];

    public DocumentRecord? GetById(int id) => _documents.FirstOrDefault(d => d.Id == id);
}

public sealed class DocumentRecord
{
    public int Id { get; init; }
    public string Owner { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public bool Published { get; set; }
}
