using System.Text;

namespace Uno.UI.DevServer.Cli.Mcp.Setup;

/// <summary>
/// Production <see cref="IFileSystem"/> backed by <see cref="System.IO"/>.
/// Writes are atomic via temp file + <see cref="File.Move(string, string, bool)"/>.
/// </summary>
internal sealed class FileSystem : IFileSystem
{
	internal static StringComparer PathComparer =>
		GetPathComparer(OperatingSystem.IsWindows());

	internal static StringComparer GetPathComparer(bool isWindows) =>
		isWindows ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

	public bool FileExists(string path) => File.Exists(path);

	public bool DirectoryExists(string path) => Directory.Exists(path);

	public string ReadAllText(string path) => File.ReadAllText(path, Encoding.UTF8);

	public void WriteAllText(string path, string content)
	{
		var dir = Path.GetDirectoryName(path);
		if (!string.IsNullOrEmpty(dir))
		{
			Directory.CreateDirectory(dir);
		}

		var tempPath = Path.Combine(dir ?? ".", Path.GetRandomFileName());
		try
		{
			File.WriteAllText(tempPath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			File.Move(tempPath, path, overwrite: true);
		}
		catch
		{
			try { if (File.Exists(tempPath)) { File.Delete(tempPath); } } catch { /* best-effort cleanup */ }
			throw;
		}
	}

	public void BackupFile(string path) => File.Copy(path, path + ".bak", overwrite: true);

	public void CreateDirectory(string path) => Directory.CreateDirectory(path);

	public bool IsReadOnly(string path)
	{
		if (!File.Exists(path))
		{
			return false;
		}

		var attrs = File.GetAttributes(path);
		return (attrs & FileAttributes.ReadOnly) != 0;
	}

	public string GetUserHomePath() =>
		Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

	public string GetAppDataPath() =>
		ResolveAppDataPath(
			Environment.GetEnvironmentVariable,
			() => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
			Environment.GetFolderPath,
			OperatingSystem.IsMacOS(),
			OperatingSystem.IsLinux());

	internal static string ResolveAppDataPath(
		Func<string, string?> getEnvironmentVariable,
		Func<string> getUserHomePath,
		Func<Environment.SpecialFolder, string> getFolderPath,
		bool isMacOS,
		bool isLinux)
	{
		if (isMacOS)
		{
			return Path.Combine(getUserHomePath(), "Library", "Application Support");
		}

		if (isLinux && IsWsl(getEnvironmentVariable))
		{
			var windowsAppData = getEnvironmentVariable("APPDATA");
			if (!string.IsNullOrWhiteSpace(windowsAppData))
			{
				return ConvertWindowsPathToWslPath(windowsAppData);
			}
		}

		return getFolderPath(Environment.SpecialFolder.ApplicationData);
	}

	internal static bool IsWsl(Func<string, string?> getEnvironmentVariable) =>
		!string.IsNullOrWhiteSpace(getEnvironmentVariable("WSL_DISTRO_NAME"))
		|| !string.IsNullOrWhiteSpace(getEnvironmentVariable("WSL_INTEROP"));

	internal static string ConvertWindowsPathToWslPath(string windowsPath)
	{
		var trimmed = windowsPath.Trim();
		if (trimmed.Length >= 3 && trimmed[1] == ':' && (trimmed[2] == '\\' || trimmed[2] == '/'))
		{
			var driveLetter = char.ToLowerInvariant(trimmed[0]);
			var remainder = trimmed[2..].Replace('\\', '/');
			return $"/mnt/{driveLetter}{remainder}";
		}

		return trimmed.Replace('\\', '/');
	}
}
