namespace Alfred.Agent.Configuration;

public sealed class TelegramOptions
{
    public string Token { get; init; } = string.Empty;
    public string ChatId { get; init; } = string.Empty;
}
