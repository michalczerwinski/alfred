using System.ComponentModel;
using Alfred.Agent.Services;
using Microsoft.SemanticKernel;

namespace Alfred.Agent.Tools;

public sealed class TelegramTool
{
    private readonly TelegramClient _client;

    public TelegramTool(TelegramClient client)
    {
        _client = client;
    }

    [KernelFunction("send_telegram_message")]
    [Description("Sends a message to the user via Telegram. Use this to inform the user about completed tasks, important findings, or anything that warrants their attention.")]
    public async Task<string> SendMessageAsync(
        [Description("The message text to send via Telegram.")] string message)
    {
        try
        {
            return await _client.SendMessageAsync(message);
        }
        catch (Exception ex)
        {
            return $"Error sending Telegram message: {ex.Message}";
        }
    }
}
