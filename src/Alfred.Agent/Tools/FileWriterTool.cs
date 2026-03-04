using Alfred.Agent.Configuration;
using Alfred.Agent.Helpers;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace Alfred.Agent.Tools;

public sealed class FileWriterTool
{
	private readonly FileWriterOptions _options;

	public FileWriterTool(FileWriterOptions options)
	{
		_options = options;
	}

	[KernelFunction("write_file")]
	[Description("Writes text content to a file at the given absolute path, overwriting it if it already exists. Only paths under whitelisted directories and with allowed extensions are permitted.")]
	public async Task<string> WriteFileAsync(
		[Description("The absolute path of the file to write.")] string filePath,
		[Description("The text content to write to the file.")] string content)
	{
		if (!PathGuard.IsPathAllowed(filePath, _options.AllowedPaths))
			return $"Access denied: '{filePath}' is not under any whitelisted directory. Allowed prefixes: {string.Join(", ", _options.AllowedPaths)}";

		if (!PathGuard.IsExtensionAllowed(filePath, _options.AllowedExtensions))
			return $"Access denied: '{Path.GetExtension(filePath)}' is not an allowed file extension. Allowed extensions: {string.Join(", ", _options.AllowedExtensions)}";

		try
		{
			var directory = Path.GetDirectoryName(filePath);
			if (!string.IsNullOrEmpty(directory))
				Directory.CreateDirectory(directory);

			await File.WriteAllTextAsync(filePath, content);
			return $"File written successfully: '{filePath}'";
		}
		catch (Exception ex)
		{
			return $"Error writing file: {ex.Message}";
		}
	}

	[KernelFunction("append_file")]
	[Description("Appends text content to a file at the given absolute path, creating it if it does not exist. Only paths under whitelisted directories and with allowed extensions are permitted.")]
	public async Task<string> AppendFileAsync(
		[Description("The absolute path of the file to append to.")] string filePath,
		[Description("The text content to append to the file.")] string content)
	{
		if (!PathGuard.IsPathAllowed(filePath, _options.AllowedPaths))
			return $"Access denied: '{filePath}' is not under any whitelisted directory. Allowed prefixes: {string.Join(", ", _options.AllowedPaths)}";

		if (!PathGuard.IsExtensionAllowed(filePath, _options.AllowedExtensions))
			return $"Access denied: '{Path.GetExtension(filePath)}' is not an allowed file extension. Allowed extensions: {string.Join(", ", _options.AllowedExtensions)}";

		try
		{
			var directory = Path.GetDirectoryName(filePath);
			if (!string.IsNullOrEmpty(directory))
				Directory.CreateDirectory(directory);

			await File.AppendAllTextAsync(filePath, content);
			return $"Content appended successfully: '{filePath}'";
		}
		catch (Exception ex)
		{
			return $"Error appending to file: {ex.Message}";
		}
	}
}
