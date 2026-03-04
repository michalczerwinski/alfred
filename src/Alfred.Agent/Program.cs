using Alfred.Agent.Configuration;
using Alfred.Agent.Services;
using Alfred.Agent.Tools;
using Alfred.Agent.UI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddUserSecrets<Program>(optional: true);

// Bind and validate all configuration up-front so the app fails fast on misconfiguration
var alfred = builder.Configuration.GetSection("Alfred").Get<AlfredOptions>()
	?? throw new InvalidDataException("Missing 'Alfred' configuration section.");
alfred.Validate();

// Build the SK kernel (plugins depend on services that are constructed here)
var telegramClient = new TelegramClient(alfred.Telegram);
var schedulerStore = new SchedulerStore(alfred.Scheduler);

var kernelBuilder = Kernel.CreateBuilder();
kernelBuilder.AddOpenAIChatCompletion(alfred.OpenRouter.ModelId, new Uri(alfred.OpenRouter.Endpoint), alfred.OpenRouter.ApiKey);
kernelBuilder.Plugins.AddFromObject(new FileReaderTool(alfred.FileReader));
kernelBuilder.Plugins.AddFromObject(new FileWriterTool(alfred.FileWriter));
kernelBuilder.Plugins.AddFromObject(new NotificationTool(alfred.Ntfy));
kernelBuilder.Plugins.AddFromObject(new TelegramTool(telegramClient));
kernelBuilder.Plugins.AddFromObject(new WebFetchTool());
kernelBuilder.Plugins.AddFromObject(new SchedulerTool(schedulerStore));

// Register services
builder.Services.AddSingleton(alfred.Agent);
builder.Services.AddSingleton(kernelBuilder.Build());
builder.Services.AddSingleton(telegramClient);
builder.Services.AddSingleton(_ => new AudioTranscriptionService(alfred.OpenRouter, alfred.Whisper));
builder.Services.AddSingleton<AgentService>();
builder.Services.AddSingleton(schedulerStore);
builder.Services.AddHostedService<TelegramPollingService>();
builder.Services.AddHostedService<SchedulerService>();

var host = builder.Build();
await host.StartAsync();

// Console interaction loop
var agentService = host.Services.GetRequiredService<AgentService>();
var stopping = host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;

ConsoleUI.ShowBanner();

while (!stopping.IsCancellationRequested)
{
	try
	{
		var input = await Task.Run(ConsoleUI.PromptUser).WaitAsync(stopping);

		if (string.IsNullOrWhiteSpace(input)) continue;
		if (input.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

		var response = await ConsoleUI.RunWithSpinnerAsync(
			"Alfred is thinking…",
			() => agentService.ProcessMessageAsync(input));
		ConsoleUI.WriteAlfredReply(response);
	}
	catch (OperationCanceledException) { break; }
}

await host.StopAsync();
ConsoleUI.ShowGoodbye();


