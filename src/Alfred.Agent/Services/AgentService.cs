using Alfred.Agent.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Alfred.Agent.Services;

/// <summary>
/// Owns the SK <see cref="ChatHistory"/> and serialises all chat turns through a
/// single semaphore so the console and Telegram loops can safely call concurrently.
/// </summary>
public sealed class AgentService
{
    private readonly IChatCompletionService _chatService;
    private readonly Kernel _kernel;
    private readonly OpenAIPromptExecutionSettings _executionSettings;
    private readonly ChatHistory _history;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public AgentService(Kernel kernel, AgentOptions options)
    {
        _kernel = kernel;
        _chatService = kernel.GetRequiredService<IChatCompletionService>();
        _executionSettings = new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
        };
        _history = new ChatHistory();
        _history.AddSystemMessage(options.SystemMessage);
    }

    public async Task<string> ProcessMessageAsync(string input)
    {
        await _semaphore.WaitAsync();
        try
        {
            _history.AddUserMessage(input);
            var result = await _chatService.GetChatMessageContentAsync(
                _history,
                executionSettings: _executionSettings,
                kernel: _kernel);
            var response = result.Content ?? string.Empty;
            _history.AddAssistantMessage(response);
            return response;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
