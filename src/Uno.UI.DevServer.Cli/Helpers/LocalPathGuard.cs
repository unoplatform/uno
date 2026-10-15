namespace Uno.UI.DevServer.Cli.Helpers;

/// <summary>
/// Refuses paths an agent passes to the bridge that would reach another machine or a device before any file-system
/// access: probing a UNC or WebDAV path (\\server\share, \\server@SSL\DavWWWRoot) authenticates the user to that server,
/// and accepting one would load code from it.
/// </summary>
internal static class LocalPathGuard
{
	/// <summary>
	/// <see langword="false"/> on Windows for a UNC path or a Win32 device path (\\?\, \\.\), whose root starts with two
	/// separators; <see langword="true"/> otherwise. On Unix a network mount is a local path.
	/// </summary>
	public static bool IsLocalPath(string fullPath)
	{
		if (!OperatingSystem.IsWindows())
		{
			return true;
		}

		var root = Path.GetPathRoot(fullPath);
		return root is null
			|| !(root.StartsWith(@"\\", StringComparison.Ordinal) || root.StartsWith("//", StringComparison.Ordinal));
	}
}
