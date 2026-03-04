namespace Alfred.Agent.Configuration;

public sealed class FileWriterOptions
{
    public List<string> AllowedPaths { get; init; } = [];
    public List<string> AllowedExtensions { get; init; } = [];
}
