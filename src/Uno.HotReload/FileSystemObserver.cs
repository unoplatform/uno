using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Uno.Disposables;
using Uno.HotReload.Tracking;
using Uno.HotReload.Utils;
using Uno.UI.Tasks.HotReloadInfo;

namespace Uno.HotReload;

/// <summary>
/// Observes file system changes in project directories and notifies the hot reload manager of relevant updates.
/// </summary>
/// <remarks>FileSystemObserver monitors changes to source and metadata files within the solution's project
/// directories, excluding certain files and directories such as build output folders and Visual Studio cache
/// directories. It is intended for use with hot reload scenarios, where timely detection of file changes is required.
/// This class is not thread-safe and should be disposed when no longer needed to release file system watcher
/// resources.</remarks>
public sealed partial class FileSystemObserver : IDisposable
{
	[GeneratedRegex("net\\d+(?:\\.\\d+)*(?:-[A-Za-z][A-Za-z0-9]*)?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
	private static partial Regex TfmRegex();

	[GeneratedRegex("(debug|release)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
	private static partial Regex ConfigRegex();
	
	private readonly HotReloadManager _manager;
	private readonly IReporter _reporter;
	private readonly BufferGate _solutionWatchersGate;
	private readonly IDisposable _subscription;

	public FileSystemObserver(HotReloadManager manager, IReporter reporter, BufferGate solutionWatchersGate)
	{
		_manager = manager;
		_reporter = reporter;
		_solutionWatchersGate = solutionWatchersGate;

		_subscription = Enable();
	}

	private IDisposable Enable()
	{
		var solution = _manager.CurrentSolution;

		ImmutableArray<string> excludedDir =
		[
			.. from project in solution.Projects
			let projectDir = Path.GetDirectoryName(project.FilePath)
			from outputPath in new[] { project.OutputFilePath, project.CompilationOutputInfo.AssemblyPath }
			let excludedRoot = GetExcludedRoot(projectDir, outputPath)
			where excludedRoot is not null
			select excludedRoot
		];

		var watchers = solution
			.Projects
			.Select(project => Path.GetDirectoryName(project.FilePath))
			.Distinct()
			.Select(dir =>
			{
				_reporter.Verbose($"Observing '{dir}' project directories for metadata changes.");

				return new FileSystemWatcher
				{
					Path = dir!,
					Filter = "*.*",
					NotifyFilter = NotifyFilters.LastWrite |
						NotifyFilters.Attributes |
						NotifyFilters.Size |
						NotifyFilters.CreationTime |
						NotifyFilters.FileName,
					EnableRaisingEvents = true,
					IncludeSubdirectories = true // Required for added files in subfolders
				};
			})
			.ToArray();
		var processing = new CancellationTokenSource(); // Updates are cumulative, we cannot abort updates, so we have a SINGLE token for all operations.
		var watchersSubscription = To2StepsObservable(watchers, HasInterest, _solutionWatchersGate)
			.Subscribe(
				filePaths => _ = _manager.ProcessFileChanges(filePaths, processing.Token),
				e => Console.WriteLine($"Error {e}"));

		return new CompositeDisposable([watchersSubscription, Disposable.Create(processing.Cancel), processing, .. watchers]);

		bool HasInterest(string path)
		{
			if (Path.GetExtension(path).ToLowerInvariant() is ".csproj" or ".editorconfig")
			{
				return false;
			}

			if (!File.Exists(path) && Directory.Exists(path))
			{
				return false;
			}

			if (path.Contains("\\.vs\\")) // No need to check for AltDirectorySeparatorChar as VS is windows only and always uses '\'
			{
				// Ignore changes in the .vs cache folder
				return false;
			}

			if (excludedDir.Any(dir => path.StartsWith(dir, PathComparer.Comparison)))
			{
				// File is in an excluded directory (bin or obj)
				// However, we still allow changes from the HotReloadInfo
				if (!path.EndsWith(HotReloadInfoHelper.HotReloadInfoFilePath, PathComparer.Comparison))
				{
					return false;
				}
			}

			return true;
		}

		static string? GetExcludedRoot(string? projectDir, string? outputFilePath)
		{
			if (string.IsNullOrWhiteSpace(projectDir) || string.IsNullOrWhiteSpace(outputFilePath))
			{
				return null;
			}

			var directory = Path.GetDirectoryName(outputFilePath);
			if (string.IsNullOrWhiteSpace(directory))
			{
				return null;
			}

			projectDir = projectDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			directory = directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

			// Strategy 1: If the output is under the project directory, exclude the first sub-level
			// (e.g. projectDir\bin\... -> exclude projectDir\bin, same for any custom first-level folder).
			if (directory.StartsWith(projectDir, PathComparer.Comparison)
				&& directory.Length > projectDir.Length
				&& (directory[projectDir.Length] == Path.DirectorySeparatorChar || directory[projectDir.Length] == Path.AltDirectorySeparatorChar))
			{
				var relative = directory.Substring(projectDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
				var separatorIndex = relative.IndexOfAny(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
				var firstSubLevel = separatorIndex >= 0 ? relative[..separatorIndex] : relative;
				if (!string.IsNullOrWhiteSpace(firstSubLevel))
				{
					return Path.Combine(projectDir, firstSubLevel).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
				}
			}

			// Strategy 2: If output is outside the project directory, trim trailing TFM/config segments
			// (e.g. ...\Debug\net10.0-desktop -> ...).
			var outputDir = directory;
			while (true)
			{
				var updated = outputDir;
				var leaf = Path.GetFileName(updated);
				if (!string.IsNullOrWhiteSpace(leaf))
				{
					if (TfmRegex().IsMatch(leaf) || ConfigRegex().IsMatch(leaf))
					{
						var parent = Path.GetDirectoryName(updated);
						if (!string.IsNullOrWhiteSpace(parent))
						{
							updated = parent.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
						}
					}
				}

				if (string.Equals(updated, outputDir, StringComparison.Ordinal))
				{
					break;
				}

				outputDir = updated;
			}

			return outputDir;
		}

	}

	private static IObservable<Task<ImmutableHashSet<string>>> To2StepsObservable(FileSystemWatcher[] watchers, Predicate<string> filter, BufferGate bufferGate)
		=> Observable.Create<Task<ImmutableHashSet<string>>>(o =>
		{
			// Create an observable instead of using the FromEventPattern which
			// does not register to events properly.
			// Renames are required for the WriteTemporary->DeleteOriginal->RenameToOriginal that
			// Visual Studio uses to save files.

			var gate = new object();
			var buffer = default((ImmutableHashSet<string>.Builder items, TaskCompletionSource<ImmutableHashSet<string>> task)?);
			var bufferTimer = new Timer(_ => bufferGate.RunOrPlan(CloseBuffer));

			void changed(object s, FileSystemEventArgs args) => OnNext(args.FullPath);
			void renamed(object s, RenamedEventArgs args) => OnNext(args.FullPath);

			foreach (var watcher in watchers)
			{
				watcher.Changed += changed;
				watcher.Created += changed;
				watcher.Deleted += changed;
				watcher.Renamed += renamed;
			}

			void OnNext(string file)
			{
				if (!filter(file))
				{
					return;
				}

				lock (gate)
				{
					if (buffer is null)
					{
						buffer = (ImmutableHashSet.CreateBuilder<string>(PathComparer.Comparer), new());
						o.OnNext(buffer.Value.task.Task);
					}

					buffer.Value.items.Add(file);
					bufferTimer.Change(250, Timeout.Infinite); // Wait for 250 ms without any file change
				}
			}

			void CloseBuffer()
			{
				(ImmutableHashSet<string>.Builder items, TaskCompletionSource<ImmutableHashSet<string>> task) completingBuffer;
				if (buffer is null)
				{
					Debug.Fail("Should not happen.");
					return;
				}

				lock (gate)
				{
					completingBuffer = buffer.Value;
					buffer = default;
				}

				completingBuffer.task.SetResult(completingBuffer.items.ToImmutable());
			}

			return Disposable.Create(() =>
			{
				foreach (var watcher in watchers)
				{
					watcher.Changed -= changed;
					watcher.Created -= changed;
					watcher.Renamed -= renamed;
				}

				bufferTimer.Dispose();
			});
		});

	/// <inheritdoc />
	public void Dispose()
		=> _subscription.Dispose();
}
