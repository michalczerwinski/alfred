using Alfred.Agent.UI;
using Microsoft.Extensions.Hosting;
using Spectre.Console;

namespace Alfred.Agent.Services;

/// <summary>
/// Hosted background service that runs the Telegram long-poll loop.
/// Receives text and voice messages, transcribes voice, and forwards
/// every message to <see cref="AgentService"/> for processing.
/// </summary>
public sealed class TelegramPollingService : BackgroundService
{
    private readonly TelegramClient _telegram;
    private readonly AudioTranscriptionService _audio;
    private readonly AgentService _agent;

    public TelegramPollingService(
        TelegramClient telegram,
        AudioTranscriptionService audio,
        AgentService agent)
    {
        _telegram = telegram;
        _audio = audio;
        _agent = agent;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_telegram.IsConfigured) return;

        long offset = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var updates = await _telegram.GetUpdatesAsync(offset, stoppingToken);
                foreach (var update in updates)
                {
                    offset = update.UpdateId + 1;

                    string messageText;
                    if (update.IsVoice)
                    {
                        ConsoleUI.ShowVoiceTranscribing();
                        var audioBytes = await _telegram.DownloadVoiceAsync(update.VoiceFileId!, stoppingToken);
                        messageText = await ConsoleUI.RunWithSpinnerAsync(
                            "Transcribing voice…",
                            () => _audio.TranscribeAsync(audioBytes, stoppingToken));
                        ConsoleUI.ShowTranscript(messageText);
                    }
                    else
                    {
                        messageText = update.Text!;
                        ConsoleUI.ShowTelegramTextMessage(messageText);
                    }

                    var reply = await ConsoleUI.RunWithSpinnerAsync(
                        "Alfred is thinking…",
                        () => _agent.ProcessMessageAsync(messageText));
                    ConsoleUI.WriteAlfredReply(reply);
                    await _telegram.SendMessageAsync(reply, stoppingToken);

                    AnsiConsole.Markup("\n[bold yellow]You »[/] ");
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                ConsoleUI.ShowError("Telegram polling error", ex.Message);
                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}

