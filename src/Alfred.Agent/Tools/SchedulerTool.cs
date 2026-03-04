using System.ComponentModel;
using System.Text;
using Alfred.Agent.Models;
using Alfred.Agent.Services;
using Cronos;
using Microsoft.SemanticKernel;

namespace Alfred.Agent.Tools;

public sealed class SchedulerTool
{
    private readonly SchedulerStore _store;

    public SchedulerTool(SchedulerStore store)
    {
        _store = store;
    }

    [KernelFunction("list_schedules")]
    [Description("Lists all scheduled prompts with their ID, name, cron expression, prompt text, enabled state, and next/last run times.")]
    public async Task<string> ListSchedulesAsync()
    {
        var schedules = await _store.LoadAsync();
        if (schedules.Count == 0)
            return "No schedules configured.";

        var sb = new StringBuilder();
        foreach (var s in schedules)
        {
            var next = GetNextOccurrence(s.CronExpression);
            sb.AppendLine($"[{s.Id}] {s.Name} ({(s.IsEnabled ? "enabled" : "disabled")})");
            sb.AppendLine($"  Cron   : {s.CronExpression}");
            sb.AppendLine($"  Prompt : {s.Prompt}");
            sb.AppendLine($"  Runs   : {s.ExecutionCount}{(s.MaxExecutions.HasValue ? $" / {s.MaxExecutions}" : " (unlimited)")}");
            sb.AppendLine($"  Next   : {(next.HasValue ? next.Value.ToLocalTime().ToString("g") : "n/a")}");
            if (s.LastRunAt.HasValue)
                sb.AppendLine($"  Last   : {s.LastRunAt.Value.ToLocalTime().ToString("g")}");
        }
        return sb.ToString().TrimEnd();
    }

    [KernelFunction("add_schedule")]
    [Description("Creates a new scheduled prompt and returns its ID. " +
                 "Use standard 5-field cron expressions (minute hour day month weekday), e.g. '0 9 * * 1-5' for 9 am on weekdays.")]
    public async Task<string> AddScheduleAsync(
        [Description("Human-readable name, e.g. 'Morning briefing'.")] string name,
        [Description("5-field cron expression, e.g. '0 9 * * *' for 9 am daily.")] string cronExpression,
        [Description("The prompt Alfred will execute at the scheduled time.")] string prompt,
        [Description("Maximum number of times to execute. Omit or pass null for unlimited.")] int? maxExecutions = null)
    {
        if (!IsValidCron(cronExpression, out var cronError))
            return $"Invalid cron expression '{cronExpression}': {cronError}";

        if (maxExecutions is <= 0)
            return "maxExecutions must be a positive number, or null for unlimited.";

        var entry = new ScheduleEntry { Name = name, CronExpression = cronExpression, Prompt = prompt, MaxExecutions = maxExecutions };
        await _store.AddAsync(entry);

        var next = GetNextOccurrence(cronExpression);
        var limitLabel = maxExecutions.HasValue ? $"{maxExecutions}x" : "unlimited";
        return $"Schedule '{name}' created with ID '{entry.Id}' ({limitLabel}). Next run: {(next.HasValue ? next.Value.ToLocalTime().ToString("g") : "n/a")}.";
    }

    [KernelFunction("delete_schedule")]
    [Description("Permanently deletes a schedule by its ID.")]
    public async Task<string> DeleteScheduleAsync(
        [Description("The 8-character ID of the schedule to delete.")] string id)
    {
        return await _store.DeleteAsync(id)
            ? $"Schedule '{id}' deleted."
            : $"No schedule found with ID '{id}'.";
    }

    [KernelFunction("update_schedule")]
    [Description("Updates one or more fields of an existing schedule. Omit any parameter to leave it unchanged.")]
    public async Task<string> UpdateScheduleAsync(
        [Description("The ID of the schedule to update.")] string id,
        [Description("New name, or null to keep current.")] string? name = null,
        [Description("New 5-field cron expression, or null to keep current.")] string? cronExpression = null,
        [Description("New prompt text, or null to keep current.")] string? prompt = null,
        [Description("True to enable, false to disable, or null to keep current.")] bool? isEnabled = null,
        [Description("New execution limit. Pass 0 to remove the limit (unlimited). Null to keep current.")] int? maxExecutions = null)
    {
        var existing = await _store.GetByIdAsync(id);
        if (existing is null)
            return $"No schedule found with ID '{id}'.";

        if (cronExpression is not null && !IsValidCron(cronExpression, out var cronError))
            return $"Invalid cron expression '{cronExpression}': {cronError}";

        if (maxExecutions is < 0)
            return "maxExecutions must be a positive number, 0 for unlimited, or null to keep current.";

        var updated = existing with
        {
            Name = name ?? existing.Name,
            CronExpression = cronExpression ?? existing.CronExpression,
            Prompt = prompt ?? existing.Prompt,
            IsEnabled = isEnabled ?? existing.IsEnabled,
            MaxExecutions = maxExecutions switch { null => existing.MaxExecutions, 0 => null, _ => maxExecutions }
        };

        await _store.UpdateAsync(updated);

        var next = GetNextOccurrence(updated.CronExpression);
        return $"Schedule '{id}' updated. Next run: {(next.HasValue ? next.Value.ToLocalTime().ToString("g") : "n/a")}.";
    }

    private static DateTimeOffset? GetNextOccurrence(string cronExpression)
    {
        try
        {
            return CronExpression.Parse(cronExpression, CronFormat.Standard)
                .GetNextOccurrence(DateTimeOffset.Now, TimeZoneInfo.Local);
        }
        catch { return null; }
    }

    private static bool IsValidCron(string expression, out string error)
    {
        try
        {
            CronExpression.Parse(expression, CronFormat.Standard);
            error = string.Empty;
            return true;
        }
        catch (CronFormatException ex)
        {
            error = ex.Message;
            return false;
        }
    }
}
