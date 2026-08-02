#nullable enable

using System;
using System.IO;
using System.Text;
using System.Threading;
using Windows.Storage;

namespace Windows.UI.Shell.Tasks;

internal interface IAppTaskInfoStore
{
	string? Read();

	void Write(string value);

	void Quarantine();

	IDisposable AcquireLock();
}

internal sealed class FileAppTaskInfoStore : IAppTaskInfoStore
{
	private const int MaxQuarantineFiles = 3;
	private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
	private readonly string _filePath;
	private readonly string _lockFilePath;

	internal FileAppTaskInfoStore()
		: this(Path.Combine(ApplicationData.Current.LocalFolder.Path, "UnoPlatform", "ShellTasks", "tasks.json"))
	{
	}

	internal FileAppTaskInfoStore(string filePath)
	{
		_filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
		_lockFilePath = filePath + ".lock";
	}

	public string? Read() => File.Exists(_filePath) ? File.ReadAllText(_filePath, Utf8WithoutBom) : null;

	public void Write(string value)
	{
		ArgumentNullException.ThrowIfNull(value);

		var directory = Path.GetDirectoryName(_filePath)
			?? throw new InvalidOperationException($"Unable to determine the app task storage directory for '{_filePath}'.");
		Directory.CreateDirectory(directory);
		if (OperatingSystem.IsBrowser())
		{
			File.WriteAllText(_filePath, value, Utf8WithoutBom);
			return;
		}

		var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(_filePath)}.{Guid.NewGuid():N}.tmp");
		try
		{
			using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
			using (var writer = new StreamWriter(stream, Utf8WithoutBom))
			{
				writer.Write(value);
				writer.Flush();
				stream.Flush(flushToDisk: !OperatingSystem.IsBrowser());
			}

			File.Move(temporaryPath, _filePath, overwrite: true);
		}
		finally
		{
			if (File.Exists(temporaryPath))
			{
				File.Delete(temporaryPath);
			}
		}
	}

	public void Quarantine()
	{
		if (!File.Exists(_filePath))
		{
			return;
		}
		if (OperatingSystem.IsBrowser())
		{
			File.Delete(_filePath);
			return;
		}

		var directory = Path.GetDirectoryName(_filePath)
			?? throw new InvalidOperationException($"Unable to determine the app task storage directory for '{_filePath}'.");
		var quarantinePath = Path.Combine(
			directory,
			$"{Path.GetFileNameWithoutExtension(_filePath)}.corrupt.{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.{Guid.NewGuid():N}{Path.GetExtension(_filePath)}");
		File.Move(_filePath, quarantinePath);
		TrimQuarantineFiles(directory);
	}

	private void TrimQuarantineFiles(string directory)
	{
		var fileName = Path.GetFileNameWithoutExtension(_filePath);
		var extension = Path.GetExtension(_filePath);
		var quarantineFiles = Directory.GetFiles(directory, $"{fileName}.corrupt.*{extension}");
		if (quarantineFiles.Length <= MaxQuarantineFiles)
		{
			return;
		}

		Array.Sort(
			quarantineFiles,
			(left, right) => File.GetLastWriteTimeUtc(right).CompareTo(File.GetLastWriteTimeUtc(left)));
		foreach (var quarantineFile in quarantineFiles.AsSpan(MaxQuarantineFiles))
		{
			File.Delete(quarantineFile);
		}
	}

	public IDisposable AcquireLock()
	{
		var directory = Path.GetDirectoryName(_lockFilePath)
			?? throw new InvalidOperationException($"Unable to determine the app task storage directory for '{_lockFilePath}'.");
		Directory.CreateDirectory(directory);

		var timeoutAt = Environment.TickCount64 + 5000;
		while (true)
		{
			try
			{
				return new FileStream(_lockFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
			}
			catch (IOException) when (Environment.TickCount64 < timeoutAt)
			{
				Thread.Sleep(25);
			}
		}
	}
}

internal sealed class NoopAppTaskInfoStoreLock : IDisposable
{
	internal static NoopAppTaskInfoStoreLock Instance { get; } = new();

	public void Dispose()
	{
	}
}
