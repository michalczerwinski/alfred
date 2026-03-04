using System.Text.Json;
using System.Text.Json.Serialization;
using Alfred.Agent.Configuration;
using Alfred.Agent.Helpers;
using Alfred.Agent.Models;

namespace Alfred.Agent.Services;

/// <summary>
/// Thread-safe persistence layer for <see cref="ScheduleEntry"/> records.
/// All reads and writes go through a semaphore so the background service
/// and the kernel tool can operate concurrently without corruption.
/// </summary>
public sealed class SchedulerStore
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public SchedulerStore(SchedulerOptions options)
    {
        _filePath = PathGuard.ExpandHome(options.StorePath);
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
    }

    public async Task<List<ScheduleEntry>> LoadAsync()
    {
        await _lock.WaitAsync();
        try { return await LoadCoreAsync(); }
        finally { _lock.Release(); }
    }

    public async Task AddAsync(ScheduleEntry entry)
    {
        await _lock.WaitAsync();
        try
        {
            var all = await LoadCoreAsync();
            all.Add(entry);
            await SaveCoreAsync(all);
        }
        finally { _lock.Release(); }
    }

    public async Task<bool> DeleteAsync(string id)
    {
        await _lock.WaitAsync();
        try
        {
            var all = await LoadCoreAsync();
            var removed = all.RemoveAll(s => s.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (removed > 0) await SaveCoreAsync(all);
            return removed > 0;
        }
        finally { _lock.Release(); }
    }

    public async Task<ScheduleEntry?> GetByIdAsync(string id)
    {
        await _lock.WaitAsync();
        try
        {
            var all = await LoadCoreAsync();
            return all.FirstOrDefault(s => s.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        }
        finally { _lock.Release(); }
    }

    public async Task<bool> UpdateAsync(ScheduleEntry updated)
    {
        await _lock.WaitAsync();
        try
        {
            var all = await LoadCoreAsync();
            var index = all.FindIndex(s => s.Id.Equals(updated.Id, StringComparison.OrdinalIgnoreCase));
            if (index < 0) return false;
            all[index] = updated;
            await SaveCoreAsync(all);
            return true;
        }
        finally { _lock.Release(); }
    }

    private async Task<List<ScheduleEntry>> LoadCoreAsync()
    {
        if (!File.Exists(_filePath)) return [];
        var json = await File.ReadAllTextAsync(_filePath);
        return JsonSerializer.Deserialize<List<ScheduleEntry>>(json, _jsonOptions) ?? [];
    }

    private async Task SaveCoreAsync(List<ScheduleEntry> schedules)
    {
        var json = JsonSerializer.Serialize(schedules, _jsonOptions);
        await File.WriteAllTextAsync(_filePath, json);
    }
}
