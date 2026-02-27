using Uno.UI.DevServer.Cli.Mcp.Setup;

namespace Uno.UI.DevServer.Cli.Tests.Mcp.Setup;

/// <summary>
/// In-memory <see cref="IFileSystem"/> fake for unit tests.
/// All paths are normalized to forward slashes and compared case-insensitively.
/// </summary>
internal sealed class InMemoryFileSystem : IFileSystem
{
	private readonly Dictionary<string, string> _files = new(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<string> _directories = new(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<string> _readOnlyFiles = new(StringComparer.OrdinalIgnoreCase);

	public string HomePath { get; set; } = "/home/testuser";
	public string AppDataPath { get; set; } = "/home/testuser/.config";

	public bool FileExists(string path) => _files.ContainsKey(Normalize(path));

	public bool DirectoryExists(string path)
	{
		var norm = Normalize(path);
		if (_directories.Contains(norm))
		{
			return true;
		}

		// A directory implicitly exists if any file is under it
		var prefix = norm.EndsWith('/') ? norm : norm + "/";
		return _files.Keys.Any(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
	}

	public string ReadAllText(string path)
	{
		var norm = Normalize(path);
		if (!_files.TryGetValue(norm, out var content))
		{
			throw new FileNotFoundException($"File not found: {path}", path);
		}

		return content;
	}

	public void WriteAllText(string path, string content)
	{
		var norm = Normalize(path);
		if (_readOnlyFiles.Contains(norm))
		{
			throw new UnauthorizedAccessException($"File is read-only: {path}");
		}

		_files[norm] = content;

		// Ensure parent directory exists
		var dir = GetDirectoryName(norm);
		if (dir is not null)
		{
			_directories.Add(dir);
		}
	}

	public void CreateDirectory(string path) => _directories.Add(Normalize(path));

	public bool IsReadOnly(string path) => _readOnlyFiles.Contains(Normalize(path));

	public string GetUserHomePath() => HomePath;

	public string GetAppDataPath() => AppDataPath;

	// ── Test helpers ──

	public void AddFile(string path, string content) => _files[Normalize(path)] = content;

	public void AddDirectory(string path) => _directories.Add(Normalize(path));

	public void SetReadOnly(string path)
	{
		var norm = Normalize(path);
		_readOnlyFiles.Add(norm);
	}

	public string? GetFileContent(string path)
	{
		_files.TryGetValue(Normalize(path), out var content);
		return content;
	}

	public IReadOnlyDictionary<string, string> AllFiles => _files;

	private static string Normalize(string path) => path.Replace('\\', '/').TrimEnd('/');

	private static string? GetDirectoryName(string normalizedPath)
	{
		var idx = normalizedPath.LastIndexOf('/');
		return idx > 0 ? normalizedPath[..idx] : null;
	}
}
