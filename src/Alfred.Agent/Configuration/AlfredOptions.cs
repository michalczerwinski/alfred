using System.ComponentModel.DataAnnotations;

namespace Alfred.Agent.Configuration;

public sealed class AlfredOptions : IValidatableObject
{
    public OpenRouterOptions OpenRouter { get; init; } = new();
    public AgentOptions Agent { get; init; } = new();
    public WhisperOptions Whisper { get; init; } = new();
    public FileReaderOptions FileReader { get; init; } = new();
    public FileWriterOptions FileWriter { get; init; } = new();
    public NtfyOptions Ntfy { get; init; } = new();
    public TelegramOptions Telegram { get; init; } = new();
    public SchedulerOptions Scheduler { get; init; } = new();

    /// <summary>Throws <see cref="InvalidDataException"/> if required configuration values are missing.</summary>
    public void Validate()
    {
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(this, new ValidationContext(this), results, validateAllProperties: true))
            throw new InvalidDataException(string.Join(Environment.NewLine, results.Select(r => r.ErrorMessage)));
    }

    IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrEmpty(OpenRouter.ApiKey))
            yield return new ValidationResult(
                "Alfred:OpenRouter:ApiKey is required.",
                [nameof(OpenRouter)]);

        if (string.IsNullOrEmpty(Agent.SystemMessage))
            yield return new ValidationResult(
                "Alfred:Agent:SystemMessage is required.",
                [nameof(Agent)]);
    }
}
