using System.Net.Http.Headers;
using Alfred.Agent.Configuration;

namespace Alfred.Agent.Services;

/// <summary>
/// Transcribes audio bytes to text using an OpenAI-compatible
/// <c>/audio/transcriptions</c> endpoint (e.g. OpenAI Whisper).
/// </summary>
public sealed class AudioTranscriptionService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _modelId;

    public AudioTranscriptionService(OpenRouterOptions openRouter, WhisperOptions whisper)
    {
        _modelId = whisper.ModelId;

        var apiKey = string.IsNullOrEmpty(whisper.ApiKey) ? openRouter.ApiKey : whisper.ApiKey;

        _httpClient = new HttpClient { BaseAddress = new Uri(whisper.Endpoint.TrimEnd('/') + "/") };
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    }

    /// <summary>
    /// Sends raw OGG/OPUS audio bytes (as produced by Telegram voice messages) to the
    /// Whisper transcription endpoint and returns the transcript.
    /// </summary>
    public async Task<string> TranscribeAsync(byte[] audioBytes, CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();

        var audioContent = new ByteArrayContent(audioBytes);
        audioContent.Headers.ContentType = new MediaTypeHeaderValue("audio/ogg");
        content.Add(audioContent, "file", "voice.ogg");
        content.Add(new StringContent(_modelId), "model");

        var response = await _httpClient.PostAsync("audio/transcriptions", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("text").GetString() ?? string.Empty;
    }

    public void Dispose() => _httpClient.Dispose();
}
