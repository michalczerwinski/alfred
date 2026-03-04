namespace Alfred.Agent.Models;

public sealed record ScheduleEntry
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N")[..8];
    public required string Name { get; init; }
    public required string CronExpression { get; init; }
    public required string Prompt { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastRunAt { get; init; }
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// Maximum number of times this schedule may fire.
    /// <c>null</c> means unlimited. Once <see cref="ExecutionCount"/>
    /// reaches this value the schedule is automatically disabled.
    /// </summary>
    public int? MaxExecutions { get; init; }

    /// <summary>How many times this schedule has been executed so far.</summary>
    public int ExecutionCount { get; init; }

    public bool IsExhausted => MaxExecutions.HasValue && ExecutionCount >= MaxExecutions.Value;
}
