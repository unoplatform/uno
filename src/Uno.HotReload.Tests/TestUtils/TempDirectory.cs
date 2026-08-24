namespace Uno.HotReload.Tests.TestUtils;

/// <summary>
/// A disposable temp directory for tests that exercise real file-system content
/// (the solution updater reads edited files from disk).
/// </summary>
internal sealed class TempDirectory : IDisposable
{
	public string Path { get; } = Directory.CreateTempSubdirectory("uno-hr-tests").FullName;

	public async Task<string> WriteFileAsync(string relativePath, string content, CancellationToken ct)
	{
		// Guard against a rooted path silently discarding the temp root in Path.Combine
		// (Path.Combine(root, "/abs") == "/abs"): callers must pass a path relative to Path.
		if (System.IO.Path.IsPathRooted(relativePath))
		{
			throw new ArgumentException("Path must be relative to the temp directory.", nameof(relativePath));
		}

		var path = System.IO.Path.Combine(Path, relativePath);
		Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
		await File.WriteAllTextAsync(path, content, ct);
		return path;
	}

	public void Dispose()
	{
		try
		{
			Directory.Delete(Path, recursive: true);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			// Best effort: leftover temp dirs are harmless (files can be transiently locked on Windows).
		}
	}
}
