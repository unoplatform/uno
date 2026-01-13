using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Disposables;
using Uno.UI.RemoteControl.Host.HotReload.MetadataUpdates;
using Uno.UI.RemoteControl.Server.Processors.Helpers;
using Uno.UI.Tasks.HotReloadInfo;

namespace Uno.UI.RemoteControl.Host.HotReload;

internal class FileSystemObserver : IDisposable
{
	private readonly HotReloadManager _manager;
	private readonly IReporter _reporter1;
	private readonly BufferGate _solutionWatchersGate1;
	private readonly IDisposable _subscription;

	public FileSystemObserver(HotReloadManager manager, IReporter reporter, BufferGate solutionWatchersGate)
	{
		_manager = manager;
		_reporter1 = reporter;
		_solutionWatchersGate1 = solutionWatchersGate;

		_subscription = ObserveSolutionPaths();
	}

	private IDisposable ObserveSolutionPaths()
	{
		var solution = _manager.CurrentSolution;
		var excludedDirPattern = _manager.OutputPaths;

		// TODO: Resolve the bin and obj folders from the project (instead of assuming same config for all projects)
		// e.g.: projectDir.First().AnalyzerOptions.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue("build_property.intermediateoutputpath", out string value)
		// Not implemented yet: for a netstd2.0 project, we don't have properties such intermediateoutputpath available!
		ImmutableArray<string> excludedDir =
		[
			.. from pattern in excludedDirPattern
			where pattern is not null
			from project in solution.Projects
			let projectDir = Path.GetDirectoryName(project.FilePath)!
			select Path.Combine(projectDir, pattern).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
		];

		var watchers = solution
			.Projects
			.Select(project => Path.GetDirectoryName(project.FilePath))
			.Distinct()
			.Select(dir =>
			{
				_reporter1.Verbose($"Observing '{dir}' project directories for metadata changes.");

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
		var watchersSubscription = To2StepsObservable(watchers, HasInterest, _solutionWatchersGate1)
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
