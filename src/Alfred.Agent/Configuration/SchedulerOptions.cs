namespace Alfred.Agent.Configuration;

public sealed class SchedulerOptions
{
    /// <summary>Path to the JSON file where schedules are persisted. Supports ~ for home directory.</summary>
    public string StorePath { get; init; } = @"~\.alfred\schedules.json";
}
