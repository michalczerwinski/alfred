namespace Alfred.Agent.Helpers;

internal static class PathGuard
{
	internal static bool IsPathAllowed(string filePath, IReadOnlyList<string> allowedPaths)
	{
		var fullPath = Path.GetFullPath(ExpandHome(filePath));
		return allowedPaths.Any(allowed =>
			fullPath.StartsWith(Path.GetFullPath(ExpandHome(allowed)), StringComparison.OrdinalIgnoreCase));
	}

	internal static bool IsExtensionAllowed(string filePath, IReadOnlyList<string> allowedExtensions)
	{
		var extension = Path.GetExtension(filePath);
		return allowedExtensions.Any(allowed =>
			extension.Equals(allowed, StringComparison.OrdinalIgnoreCase));
	}

	internal static string ExpandHome(string path) =>
		path.StartsWith('~')
			? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), path[1..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
			: path;
}
