namespace Alfred.Agent.Configuration;

public sealed class OpenRouterOptions
{
    public string ApiKey { get; init; } = string.Empty;
    public string ModelId { get; init; } = "arcee-ai/trinity-large-preview:free";
    public string Endpoint { get; init; } = "https://openrouter.ai/api/v1";
}
