using Alfred.Agent.Models;
using Alfred.Agent.UI;
using Cronos;
using Microsoft.Extensions.Hosting;

namespace Alfred.Agent.Services;

/// <summary>
/// Hosted background service that evaluates all enabled schedules each tick,
/// sleeps precisely until the next due schedule, then executes its prompt
/// through <see cref="AgentService"/>.
/// </summary>
public sealed class SchedulerService : BackgroundService
{
	private readonly SchedulerStore _store;
	private readonly AgentService _agent;

	public SchedulerService(SchedulerStore store, AgentService agent)
	{
		_store = store;
		_agent = agent;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			var tickStart = DateTimeOffset.Now;
			var upcoming = await GetUpcomming(tickStart);

			if (upcoming.Count == 0)
			{
				await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken).ConfigureAwait(false);
				continue;
			}

			var nextWake = upcoming[0].Next!.Value;
			var delay = nextWake - DateTimeOffset.Now;

			if (delay > TimeSpan.Zero)
			{
				try { await Task.Delay(delay, stoppingToken).ConfigureAwait(false); }
				catch (OperationCanceledException) { break; }
			}

			upcoming = await GetUpcomming(tickStart);

			// Execute all schedules due within a 5-second grace window
			var fireTime = DateTimeOffset.Now;
			foreach (var (schedule, next) in upcoming.Where(x => x.Next!.Value <= fireTime.AddSeconds(5)))
				_ = Task.Run(() => ExecuteScheduleAsync(schedule, stoppingToken), stoppingToken);
		}
	}

	private async Task<List<(ScheduleEntry Schedule, DateTimeOffset? Next)>> GetUpcomming(DateTimeOffset tickStart)
	{
		var schedules = await _store.LoadAsync();

		var upcoming = schedules
			.Where(s => s.IsEnabled)
			.Select(s => (Schedule: s, Next: GetNextOccurrence(s.CronExpression, tickStart)))
			.Where(x => x.Next.HasValue)
			.OrderBy(x => x.Next!.Value)
			.ToList();
		return upcoming;
	}

	private async Task ExecuteScheduleAsync(ScheduleEntry schedule, CancellationToken cancellationToken)
	{
		try
		{
			ConsoleUI.ShowScheduledPrompt(schedule.Name, schedule.Prompt);

			var reply = await ConsoleUI.RunWithSpinnerAsync(
				$"Executing '{schedule.Name}'…",
				() => _agent.ProcessMessageAsync(schedule.Prompt));

			ConsoleUI.WriteAlfredReply(reply);

			var updated = schedule with
			{
				LastRunAt = DateTimeOffset.UtcNow,
				ExecutionCount = schedule.ExecutionCount + 1
			};

			if (updated.IsExhausted)
			{
				updated = updated with { IsEnabled = false };
				ConsoleUI.ShowError("Scheduler", $"Schedule '{schedule.Name}' reached its execution limit ({schedule.MaxExecutions}) and has been disabled.");
			}

			await _store.UpdateAsync(updated);
		}
		catch (Exception ex)
		{
			ConsoleUI.ShowError("Scheduler", $"Failed to execute '{schedule.Name}': {ex.Message}");
		}
	}

	private static DateTimeOffset? GetNextOccurrence(string cronExpression, DateTimeOffset from)
	{
		try
		{
			return CronExpression.Parse(cronExpression, CronFormat.Standard)
				.GetNextOccurrence(from, TimeZoneInfo.Local);
		}
		catch { return null; }
	}
}
