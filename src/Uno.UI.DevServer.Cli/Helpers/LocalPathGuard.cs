namespace Uno.UI.DevServer.Cli.Helpers;

/// <summary>
/// Refuses paths an agent passes to the bridge that would reach another machine or a device before any file-system
/// access: probing a UNC or WebDAV path (\\server\share, \\server@SSL\DavWWWRoot) authenticates the user to that server,
/// and accepting one would load code from it.
/// </summary>
internal static class LocalPathGuard
{
	/// <summary>
	/// On Windows, <see langword="true"/> only for a drive-rooted path (C:\...): an allowlist, because a share is
	/// reachable through more prefixes (\\, //, \\?\UNC\, \??\UNC\) than a blocklist can name. A drive the user mapped
	/// to a share is their own configuration and stays local. Always <see langword="true"/> on Unix, where a network
	/// mount is a local path.
	/// </summary>
	public static bool IsLocalPath(string fullPath)
	{
		if (!OperatingSystem.IsWindows())
		{
			return true;
		}

		return Path.GetPathRoot(fullPath.AsSpan()) is [var drive, ':', '\\' or '/'] && char.IsAsciiLetter(drive);
	}
}
