# Alfred

A personal AI agent for Windows, built on [Microsoft Semantic Kernel](https://github.com/microsoft/semantic-kernel) with an [OpenRouter](https://openrouter.ai) backend. Alfred acts as your digital butler — accepting commands from the console or Telegram (including voice messages), autonomously calling tools, and keeping you informed.

## Features

- **Multi-channel input** — interact via the interactive console or a Telegram bot
- **Voice messages** — Telegram voice notes are transcribed via Whisper (Groq) before processing
- **Autonomous tool use** — the LLM decides when to call tools; no manual dispatch required
- **File system access** — read, write, append, list, and search files within whitelisted directories
- **Web browsing** — fetch and read any public URL as plain text
- **Push notifications** — send urgent alerts to your devices via [ntfy.sh](https://ntfy.sh)
- **Telegram messaging** — Alfred can send messages back to you proactively
- **Scheduler** — create cron-based scheduled prompts that Alfred executes automatically
- **Persistent chat history** — full conversation context is maintained for the duration of a session

##Screenshot
****<img width="1600" height="499" alt="image" src="https://github.com/user-attachments/assets/bb9b26c2-d33e-422a-8fe7-984235a2692e" />

## Architecture

```
Alfred.slnx
└── src/Alfred.Agent/          ← Console app (.NET 8)
    ├── Program.cs             ← Host setup, SK kernel, console chat loop
    ├── appsettings.json       ← Configuration (model, API keys, whitelists)
    ├── Configuration/         ← Strongly-typed options classes
    ├── Services/              ← AgentService, TelegramPolling, Scheduler, AudioTranscription
    ├── Tools/                 ← SK plugin classes (one class = one plugin)
    ├── Models/                ← Domain models (ScheduleEntry, etc.)
    ├── Helpers/               ← PathGuard and other utilities
    └── UI/                    ← ConsoleUI (Spectre.Console rendering)
```

**Request flow:**
1. Input arrives from the console loop or the Telegram polling background service.
2. Voice messages are transcribed via Whisper before being passed to the agent.
3. `AgentService` appends the message to `ChatHistory` and calls the LLM via Semantic Kernel.
4. The LLM autonomously invokes tools (`ToolCallBehavior.AutoInvokeKernelFunctions`) as needed.
5. The response is rendered in the console and, for Telegram input, sent back to the user.

## Tools

| Plugin | Functions | Description |
|--------|-----------|-------------|
| `FileReaderTool` | `read_file`, `list_files`, `search_files` | Read and search files within whitelisted paths |
| `FileWriterTool` | `write_file`, `append_file` | Write/append files within whitelisted paths and extensions |
| `WebFetchTool` | `get_web_page` | Fetch a URL and return it as plain text |
| `NotificationTool` | `send_notification` | Push a notification via ntfy.sh |
| `TelegramTool` | `send_telegram_message` | Send a Telegram message to the user |
| `SchedulerTool` | `list_schedules`, `add_schedule`, `update_schedule`, `delete_schedule` | Manage cron-based scheduled prompts |

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- An [OpenRouter](https://openrouter.ai) API key
- *(Optional)* A Telegram bot token + your chat ID
- *(Optional)* A [Groq](https://console.groq.com) API key for voice transcription
- *(Optional)* An [ntfy.sh](https://ntfy.sh) topic for push notifications

## Getting Started

### 1. Clone and build

```bash
git clone https://github.com/michalczerwinski/alfred.git
cd Alfred
dotnet build
```

### 2. Configure

All configuration lives under the `Alfred` key. The recommended approach is **user secrets** (keeps secrets out of source control):

```bash
cd src/Alfred.Agent
dotnet user-secrets set "Alfred:OpenRouter:ApiKey" "sk-or-..."
dotnet user-secrets set "Alfred:Telegram:BotToken" "123456:ABC-..."
dotnet user-secrets set "Alfred:Telegram:ChatId"   "987654321"
dotnet user-secrets set "Alfred:Ntfy:Topic"        "your-ntfy-topic"
```

Non-secret settings can go in `appsettings.json`:

```json
{
  "Alfred": {
    "OpenRouter": {
      "ModelId": "google/gemini-2.0-flash-001",
      "Endpoint": "https://openrouter.ai/api/v1"
    },
    "Whisper": {
      "ModelId": "whisper-large-v3-turbo",
      "Endpoint": "https://api.groq.com/openai/v1/"
    },
    "Agent": {
      "SystemMessage": "You are Alfred, a helpful personal assistant."
    },
    "FileReader": {
      "AllowedPaths": [ "~\\Documents" ]
    },
    "FileWriter": {
      "AllowedPaths": [ "~\\Documents\\Alfred" ],
      "AllowedExtensions": [ ".txt", ".md", ".json", ".csv" ]
    },
    "Scheduler": {
      "StorePath": "~\\.alfred\\schedules.json"
    }
  }
}
```

### 3. Run

```bash
dotnet run --project src/Alfred.Agent
```

Type your message and press Enter. Type `exit` or press Ctrl+C to quit.

## Configuration Reference

| Key | Required | Description |
|-----|----------|-------------|
| `Alfred:OpenRouter:ApiKey` | ✅ | OpenRouter API key |
| `Alfred:OpenRouter:ModelId` | ✅ | Model ID (e.g. `google/gemini-2.0-flash-001`) |
| `Alfred:OpenRouter:Endpoint` | ✅ | API base URL (e.g. `https://openrouter.ai/api/v1`) |
| `Alfred:Agent:SystemMessage` | ✅ | System prompt sent to the LLM |
| `Alfred:Telegram:BotToken` | ➖ | Telegram bot token — enables Telegram input/output |
| `Alfred:Telegram:ChatId` | ➖ | Your Telegram chat ID — restricts bot to this chat |
| `Alfred:Whisper:Endpoint` | ➖ | Whisper-compatible transcription endpoint |
| `Alfred:Whisper:ModelId` | ➖ | Whisper model ID |
| `Alfred:Ntfy:Topic` | ➖ | ntfy.sh topic for push notifications |
| `Alfred:FileReader:AllowedPaths` | ➖ | Whitelisted directory prefixes for file reading |
| `Alfred:FileWriter:AllowedPaths` | ➖ | Whitelisted directory prefixes for file writing |
| `Alfred:FileWriter:AllowedExtensions` | ➖ | Allowed file extensions for writing |
| `Alfred:Scheduler:StorePath` | ➖ | Path to the JSON file storing scheduled entries |

`~` in path values expands to the current user's home directory.

## Security

- **Path traversal prevention** — `PathGuard` resolves all paths with `Path.GetFullPath` before comparing against whitelists, preventing `../` escape attacks.
- **Extension allowlist** — file writes are further restricted to a configurable set of extensions.
- **Telegram chat lock** — when `ChatId` is configured, the bot ignores messages from any other chat.

## Adding New Tools

1. Create a class in `src/Alfred.Agent/Tools/`:

```csharp
namespace Alfred.Agent.Tools;

public sealed class MyTool
{
    [KernelFunction("my_function")]
    [Description("What this does — shown to the LLM.")]
    public async Task<string> DoSomethingAsync(
        [Description("Parameter description.")] string input)
    {
        // Return a string result or an error message — never throw.
        return "done";
    }
}
```

2. Register it in `Program.cs`:

```csharp
kernelBuilder.Plugins.AddFromObject(new MyTool(), "MyPlugin");
```

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `Microsoft.SemanticKernel` | 1.72.0 | LLM orchestration and tool calling |
| `Microsoft.SemanticKernel.Connectors.Google` | 1.72.0-alpha | OpenAI-compatible connector |
| `Microsoft.Extensions.Hosting` | 10.0.x | Background services, configuration |
| `Spectre.Console` | 0.54.x | Rich console rendering |
| `Cronos` | 0.11.x | Cron expression parsing for the scheduler |
