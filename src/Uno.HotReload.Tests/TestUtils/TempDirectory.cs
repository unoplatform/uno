namespace Uno.HotReload.Tests.TestUtils;

/// <summary>
/// A disposable temp directory for tests that exercise real file-system content
/// (the solution updater reads edited files from disk, the file-system observer
/// watches project directories).
/// </summary>
internal sealed class TempDirectory : IDisposable
{
	public string Path { get; } = Directory.CreateTempSubdirectory("uno-hr-tests").FullName;

	public async Task<string> WriteFileAsync(string relativePath, string content, CancellationToken ct)
	{
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
		catch (IOException)
		{
			// Best effort: leftover temp dirs are harmless.
		}
	}
}
