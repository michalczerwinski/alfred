# Alfred — Copilot Instructions

Alfred is a personal AI agent for Windows built on **Microsoft Semantic Kernel** (v1.72+) with a **Google Gemini** backend. The solution uses the new `.slnx` (XML) format.

## Build & Run

```bash
# Build
dotnet build

# Run Alfred interactively
dotnet run --project src/Alfred.Agent

# Build a single project
dotnet build src/Alfred.Agent
```

There are no automated tests yet. Type `exit` to quit the interactive agent loop.

## Architecture

```
Alfred.slnx
└── src/Alfred.Agent/          ← Single console app; entry point is Program.cs
    ├── Program.cs             ← Bootstraps config, SK kernel, and the chat loop
    ├── appsettings.json       ← Runtime config (API key, model, whitelists)
    └── Tools/                 ← Semantic Kernel plugin classes (one class = one plugin)
```

**Request flow:**
1. `Program.cs` builds an SK `Kernel` with `AddGoogleAIGeminiChatCompletion` and registers all plugins via `builder.Plugins.AddFromObject(...)`.
2. Each turn calls `chatService.GetChatMessageContentAsync` with `GeminiToolCallBehavior.AutoInvokeKernelFunctions` — Gemini decides autonomously whether to call tools.
3. Full `ChatHistory` is maintained in memory for the session; there is no persistence between runs.

## Adding New Tools

All tools live in `src/Alfred.Agent/Tools/`. Each tool is a plain C# class:

```csharp
namespace Alfred.Agent.Tools;

public sealed class MyTool
{
    [KernelFunction("function_name")]
    [Description("What this function does — this text is sent to Gemini.")]
    public async Task<string> DoSomethingAsync(
        [Description("Describe this parameter for Gemini.")] string input)
    { ... }
}
```

Register the new tool in `Program.cs`:
```csharp
builder.Plugins.AddFromObject(new MyTool(/* inject config/deps */), "PluginName");
```

Tool methods must return `string` or `Task<string>`. Return error messages as strings — do not throw; Gemini receives the return value and decides how to proceed.

## Configuration (`appsettings.json`)

| Key | Purpose |
|-----|---------|
| `Gemini:ApiKey` | Google AI Studio API key |
| `Gemini:ModelId` | Gemini model name (default: `gemini-2.0-flash`) |
| `FileReader:AllowedPaths` | String array of directory prefixes the `read_file` tool may access |

Environment variables override `appsettings.json` values (standard `Microsoft.Extensions.Configuration` layering). The file is copied to the output directory on build (`PreserveNewest`).

## Key Conventions

- **Security boundary for file access**: `FileReaderTool.IsPathAllowed` resolves both the requested path and each whitelist entry with `Path.GetFullPath` before comparing, preventing path traversal attacks. New tools with file/system access must follow the same pattern.
- **Nullable reference types are enabled** (`<Nullable>enable</Nullable>`); avoid `null`-returning methods — prefer returning descriptive error strings or using `?` types explicitly.
- **No DI container**: Dependencies are manually constructed in `Program.cs` and passed to tool constructors. Keep constructors simple; inject only `IConfiguration` and other lightweight dependencies.
- **`GeminiConnectors` package is pre-release** (`1.72.0-alpha`): use `--prerelease` flag when updating it with `dotnet add package`.
