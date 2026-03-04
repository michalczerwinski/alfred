namespace Alfred.Agent.Configuration;

public sealed class WhisperOptions
{
    /// <summary>
    /// API key for the Whisper endpoint. Falls back to <see cref="OpenRouterOptions.ApiKey"/> when empty,
    /// which works if your provider proxies audio transcriptions on the same key.
    /// Set explicitly to use a dedicated OpenAI key against the default OpenAI endpoint.
    /// </summary>
    public string ApiKey { get; init; } = string.Empty;

    public string ModelId { get; init; } = "whisper-1";

    public string Endpoint { get; init; } = "https://api.openai.com/v1/";
}
