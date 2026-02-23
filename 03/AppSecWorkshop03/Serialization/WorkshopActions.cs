using System.Runtime.Serialization;

namespace AppSecWorkshop03.Serialization;

public interface IWorkshopAction
{
    string Execute();
}

public sealed class EchoAction : IWorkshopAction
{
    public string Message { get; set; } = string.Empty;

    public string Execute() => $"Echo: {Message}";
}

public sealed class DangerousAction : IWorkshopAction
{
    public string FileName { get; set; } = "compromised.txt";
    public string Content { get; set; } = "This file was created by insecure deserialization.";

    [OnDeserialized]
    internal void OnDeserialized(StreamingContext context)
    {
        var outputPath = Path.Combine(AppContext.BaseDirectory, FileName);
        File.WriteAllText(outputPath, Content);
    }

    public string Execute() => "Dangerous action executed.";
}
