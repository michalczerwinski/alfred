using System.ComponentModel;
using Alfred.Agent.Configuration;
using Alfred.Agent.Helpers;
using Microsoft.SemanticKernel;

namespace Alfred.Agent.Tools;

public sealed class FileReaderTool
{
    private readonly FileReaderOptions _options;

    public FileReaderTool(FileReaderOptions options)
    {
        _options = options;
    }

    [KernelFunction("read_file")]
    [Description("Reads the full text contents of a file at the given absolute path. Only paths under whitelisted directories are allowed.")]
    public async Task<string> ReadFileAsync(
        [Description("The absolute path of the file to read.")] string filePath)
    {
        if (!PathGuard.IsPathAllowed(filePath, _options.AllowedPaths))
            return $"Access denied: '{filePath}' is not under any whitelisted directory. Allowed prefixes: {string.Join(", ", _options.AllowedPaths)}";

        if (!File.Exists(filePath))
            return $"File not found: '{filePath}'";

        try
        {
            return await File.ReadAllTextAsync(filePath);
        }
        catch (Exception ex)
        {
            return $"Error reading file: {ex.Message}";
        }
    }

    [KernelFunction("list_files")]
    [Description("Lists files in a directory, optionally filtered by a wildcard pattern (e.g. '*.txt'). Only paths under whitelisted directories are allowed.")]
    public Task<string> ListFilesAsync(
        [Description("The absolute path of the directory to list.")] string directoryPath,
        [Description("Optional wildcard pattern to filter files, e.g. '*.txt'. Omit or pass '*' to list all files.")] string pattern = "*")
    {
        if (!PathGuard.IsPathAllowed(directoryPath, _options.AllowedPaths))
            return Task.FromResult($"Access denied: '{directoryPath}' is not under any whitelisted directory. Allowed prefixes: {string.Join(", ", _options.AllowedPaths)}");

        if (!Directory.Exists(directoryPath))
            return Task.FromResult($"Directory not found: '{directoryPath}'");

        try
        {
            var files = Directory.EnumerateFiles(directoryPath, pattern, SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return files.Count == 0
                ? Task.FromResult($"No files matching '{pattern}' found in '{directoryPath}'.")
                : Task.FromResult(string.Join(Environment.NewLine, files));
        }
        catch (Exception ex)
        {
            return Task.FromResult($"Error listing directory: {ex.Message}");
        }
    }

    [KernelFunction("search_files")]
    [Description("Searches for files under a directory by wildcard pattern and optionally by text content. Returns matching file paths with a snippet of the matching line when content search is used. Only paths under whitelisted directories are allowed.")]
    public Task<string> SearchFilesAsync(
        [Description("The absolute path of the directory to search (searches recursively).")] string directoryPath,
        [Description("Wildcard pattern to match file names, e.g. '*.md'. Use '*' to search all files.")] string pattern = "*",
        [Description("Optional text to search for inside each file. Omit to match by file name only.")] string? contentQuery = null)
    {
        if (!PathGuard.IsPathAllowed(directoryPath, _options.AllowedPaths))
            return Task.FromResult($"Access denied: '{directoryPath}' is not under any whitelisted directory. Allowed prefixes: {string.Join(", ", _options.AllowedPaths)}");

        if (!Directory.Exists(directoryPath))
            return Task.FromResult($"Directory not found: '{directoryPath}'");

        try
        {
            var files = Directory.EnumerateFiles(directoryPath, pattern, SearchOption.AllDirectories);
            var results = new List<string>();

            foreach (var file in files)
            {
                if (contentQuery is null)
                {
                    results.Add(file);
                    continue;
                }

                var lines = File.ReadLines(file);
                foreach (var (line, index) in lines.Select((l, i) => (l, i + 1)))
                {
                    if (line.Contains(contentQuery, StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add($"{file}:{index}: {line.Trim()}");
                        break; // one match per file is enough for discovery
                    }
                }
            }

            if (results.Count == 0)
            {
                var description = contentQuery is null
                    ? $"No files matching '{pattern}' found under '{directoryPath}'."
                    : $"No files matching '{pattern}' containing '{contentQuery}' found under '{directoryPath}'.";
                return Task.FromResult(description);
            }

            return Task.FromResult(string.Join(Environment.NewLine, results));
        }
        catch (Exception ex)
        {
            return Task.FromResult($"Error searching files: {ex.Message}");
        }
    }
}

