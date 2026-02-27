namespace Uno.UI.DevServer.Cli.Mcp.Setup;

/// <summary>
/// Abstracts file-system I/O for testability. All MCP setup business logic
/// accesses the file system exclusively through this interface.
/// </summary>
internal interface IFileSystem
{
	bool FileExists(string path);
	bool DirectoryExists(string path);
	string ReadAllText(string path);

	/// <summary>
	/// Writes content to <paramref name="path"/> atomically (via temp file + move).
	/// Creates parent directories if they do not exist.
	/// </summary>
	void WriteAllText(string path, string content);

	void CreateDirectory(string path);
	bool IsReadOnly(string path);
	string GetUserHomePath();
	string GetAppDataPath();
}
