using System.Text.Json;
using Alfred.Agent.Configuration;

namespace Alfred.Agent.Services;

/// <summary>
/// Encapsulates all Telegram Bot API interactions: long-polling for incoming
/// messages and sending outbound messages.
/// </summary>
public sealed class TelegramClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _token;
    private readonly string _chatId;

    /// <summary>True when a token is configured and the client can make API calls.</summary>
    public bool IsConfigured => !string.IsNullOrEmpty(_token);

    public TelegramClient(TelegramOptions options)
    {
        _token = options.Token;
        _chatId = options.ChatId;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
    }

    /// <summary>
    /// Polls the Telegram Bot API for new updates starting at <paramref name="offset"/>.
    /// Only text messages are returned; other update types are silently skipped.
    /// </summary>
    /// <exception cref="HttpRequestException">Thrown when the API returns a non-success status code.</exception>
    public async Task<IReadOnlyList<TelegramUpdate>> GetUpdatesAsync(
        long offset,
        CancellationToken cancellationToken = default)
    {
        var url = $"https://api.telegram.org/bot{_token}/getUpdates?timeout=30&offset={offset}";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.GetProperty("ok").GetBoolean())
            return [];

        var updates = new List<TelegramUpdate>();
        foreach (var update in root.GetProperty("result").EnumerateArray())
        {
            var updateId = update.GetProperty("update_id").GetInt64();

            if (!update.TryGetProperty("message", out var message)) continue;

            if (message.TryGetProperty("text", out var textProp))
            {
                var text = textProp.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                    updates.Add(new TelegramUpdate(updateId, text, null));
                continue;
            }

            if (message.TryGetProperty("voice", out var voice))
            {
                var fileId = voice.GetProperty("file_id").GetString();
                if (!string.IsNullOrWhiteSpace(fileId))
                    updates.Add(new TelegramUpdate(updateId, null, fileId));
            }
        }

        return updates;
    }

    /// <summary>
    /// Sends a text message to the configured chat.
    /// </summary>
    /// <returns>A human-readable result string suitable for returning to an AI kernel function.</returns>
    public async Task<string> SendMessageAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        var url = $"https://api.telegram.org/bot{_token}/sendMessage";
        var payload = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("chat_id", _chatId),
            new KeyValuePair<string, string>("text", text)
        ]);

        var response = await _httpClient.PostAsync(url, payload, cancellationToken);

        return response.IsSuccessStatusCode
            ? "Telegram message sent successfully."
            : $"Failed to send Telegram message. Status: {(int)response.StatusCode} ({response.ReasonPhrase})";
    }

    /// <summary>
    /// Resolves a Telegram <paramref name="fileId"/> to its download URL and returns the raw bytes.
    /// Telegram voice messages are always OGG/OPUS.
    /// </summary>
    public async Task<byte[]> DownloadVoiceAsync(string fileId, CancellationToken cancellationToken = default)
    {
        var getFileUrl = $"https://api.telegram.org/bot{_token}/getFile?file_id={fileId}";
        var fileResponse = await _httpClient.GetAsync(getFileUrl, cancellationToken);
        fileResponse.EnsureSuccessStatusCode();

        var json = await fileResponse.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        var filePath = doc.RootElement.GetProperty("result").GetProperty("file_path").GetString()!;

        var downloadUrl = $"https://api.telegram.org/file/bot{_token}/{filePath}";
        return await _httpClient.GetByteArrayAsync(downloadUrl, cancellationToken);
    }

    public void Dispose() => _httpClient.Dispose();
}

/// <summary>A parsed Telegram update containing only the fields Alfred needs.</summary>
public sealed record TelegramUpdate(long UpdateId, string? Text, string? VoiceFileId)
{
    public bool IsVoice => VoiceFileId is not null;
}
