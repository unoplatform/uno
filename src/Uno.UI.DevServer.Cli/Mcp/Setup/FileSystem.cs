using System.Text;

namespace Uno.UI.DevServer.Cli.Mcp.Setup;

/// <summary>
/// Production <see cref="IFileSystem"/> backed by <see cref="System.IO"/>.
/// Writes are atomic via temp file + <see cref="File.Move(string, string, bool)"/>.
/// </summary>
internal sealed class FileSystem : IFileSystem
{
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

		var tempPath = path + ".tmp";
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
		OperatingSystem.IsMacOS()
			? Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
				"Library", "Application Support")
			: Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
}
