using System;
using System.IO;
using System.Threading.Tasks;
using Uno.HotReload.Tracking;

namespace Uno.HotReload.IO;

internal static class FileSystemHelper
{
	/// <summary>
	/// Sets up a <see cref="FileSystemWatcher"/> and returns a <see cref="ValueTask"/> that completes
	/// when the specified file is modified (or after a timeout).
	/// </summary>
	/// <remarks>
	/// This must be called **before** the actual file modification so the watcher is in place.
	/// Await the returned task **after** performing the file write/delete.
	/// </remarks>
	public static async ValueTask WaitForFileUpdated(string filePath, IReporter? reporter = null)
	{
		var file = new FileInfo(filePath);
		var dir = file.Directory;
		while (dir is { Exists: false })
		{
			dir = dir.Parent;
		}

		if (dir is null)
		{
			return;
		}

		var tcs = new TaskCompletionSource();
		using var watcher = new FileSystemWatcher(dir.FullName);
		watcher.Changed += (snd, e) =>
		{
			if (e.FullPath.Equals(file.FullName, StringComparison.OrdinalIgnoreCase))
			{
				tcs.TrySetResult();
			}
		};
		watcher.EnableRaisingEvents = true;

		if (await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(5))) != tcs.Task)
		{
			reporter?.Verbose($"File update event not received for '{filePath}', continuing anyway.");
		}
	}
}
