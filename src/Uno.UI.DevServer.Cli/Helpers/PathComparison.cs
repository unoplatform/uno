namespace Uno.UI.DevServer.Cli.Helpers;

internal static class PathComparison
{
	public static readonly StringComparer FileSystemComparer =
		IsCaseInsensitiveFileSystem()
			? StringComparer.OrdinalIgnoreCase
			: StringComparer.Ordinal;

	public static bool PathsEqual(string? left, string? right)
	{
		if (left is null || right is null)
		{
			return left is null && right is null;
		}

		return string.Equals(
			Normalize(left),
			Normalize(right),
			StringComparison.Ordinal);
	}

	public static string Normalize(string path)
	{
		var normalized = Path.GetFullPath(path).Replace('\\', '/');
		if (IsCaseInsensitivePath(normalized))
		{
			normalized = normalized.ToLowerInvariant();
		}

		return normalized;
	}

	private static bool IsCaseInsensitiveFileSystem()
		=> OperatingSystem.IsWindows() || OperatingSystem.IsMacOS();

	private static bool IsCaseInsensitivePath(string normalizedPath)
		=> IsCaseInsensitiveFileSystem()
			|| normalizedPath.StartsWith("/mnt/", StringComparison.Ordinal);
}
