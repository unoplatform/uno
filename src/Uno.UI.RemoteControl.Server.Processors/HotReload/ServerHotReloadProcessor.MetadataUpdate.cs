#nullable enable

#if NET6_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Uno.Disposables;
using Uno.Extensions;
using Uno.UI.RemoteControl.Host.HotReload.MetadataUpdates;
using Uno.UI.RemoteControl.HotReload.Messages;

namespace Uno.UI.RemoteControl.Host.HotReload
{
	partial class ServerHotReloadProcessor : IServerProcessor, IDisposable
	{
		private FileSystemWatcher[] _solutionWatchers;
		private CompositeDisposable _solutionWatcherEventsDisposable;

		private Task<(Solution, WatchHotReloadService)>? _initializeTask;
		private Solution _currentSolution;
		private WatchHotReloadService _hotReloadService;
		private IReporter _reporter = new Reporter();

		private bool _useRoslynHotReload;

		private void InitializeMetadataUpdater(ConfigureServer configureServer)
		{
			_ = bool.TryParse(_remoteControlServer.GetServerConfiguration("metadata-updates"), out _useRoslynHotReload);

			if (_useRoslynHotReload)
			{
				CompilationWorkspaceProvider.InitializeRoslyn();

				InitializeInner(configureServer);
			}
		}

		private void InitializeInner(ConfigureServer configureServer) => _initializeTask = Task.Run(
			async () =>
			{
				var result = await CompilationWorkspaceProvider.CreateWorkspaceAsync(configureServer.ProjectPath, _reporter, configureServer.MetadataUpdateCapabilities, CancellationToken.None);

				ObserveSolutionPaths(result.Item1);

				return result;
			},
			CancellationToken.None);

		private void ObserveSolutionPaths(Solution solution)
		{
			_solutionWatchers = solution.Projects
				.SelectMany(p => p.Documents.Select(d => Path.GetDirectoryName(d.FilePath)))
				.Distinct()
				.Select(p => new FileSystemWatcher
				{
					Path = p!,
					Filter = "*.*",
					NotifyFilter = NotifyFilters.LastWrite |
						NotifyFilters.Attributes |
						NotifyFilters.Size |
						NotifyFilters.CreationTime |
						NotifyFilters.FileName,
					EnableRaisingEvents = true,
					IncludeSubdirectories = false
				})
				.ToArray();

			_solutionWatcherEventsDisposable = new CompositeDisposable();

			foreach (var watcher in _solutionWatchers)
			{
				// Create an observable instead of using the FromEventPattern which
				// does not register to events properly.
				// Renames are required for the WriteTemporary->DeleteOriginal->RenameToOriginal that
				// Visual Studio uses to save files.

				var changes = Observable.Create<string>(o =>
				{

					void changed(object s, FileSystemEventArgs args) => o.OnNext(args.FullPath);
					void renamed(object s, RenamedEventArgs args) => o.OnNext(args.FullPath);

					watcher.Changed += changed;
					watcher.Created += changed;
					watcher.Renamed += renamed;

					return Disposable.Create(() =>
					{
						watcher.Changed -= changed;
						watcher.Created -= changed;
						watcher.Renamed -= renamed;
					});
				});

				var disposable = changes
					.Buffer(TimeSpan.FromMilliseconds(250))
					.Subscribe(filePaths =>
					{
#if NET6_0_OR_GREATER
						ProcessMetadataChanges(filePaths.Distinct());
#endif
					}, e => Console.WriteLine($"Error {e}"));

				_solutionWatcherEventsDisposable.Add(disposable);

			}
		}

		private void ProcessMetadataChanges(IEnumerable<string> filePaths)
		{
			if (_useRoslynHotReload)
			{
				foreach (var file in filePaths)
				{
					ProcessSolutionChanged(CancellationToken.None, file).Wait();
				}
			}
		}

		private async Task<bool> ProcessSolutionChanged(CancellationToken cancellationToken, string file)
		{
			await EnsureSolutionInitializedAsync();

			var sw = Stopwatch.StartNew();

			Solution? updatedSolution = null;
			ProjectId updatedProjectId;

			if (_currentSolution.Projects.SelectMany(p => p.Documents).FirstOrDefault(d => string.Equals(d.FilePath, file, StringComparison.OrdinalIgnoreCase)) is Document documentToUpdate)
			{
				var sourceText = await GetSourceTextAsync(file);
				updatedSolution = documentToUpdate.WithText(sourceText).Project.Solution;
				updatedProjectId = documentToUpdate.Project.Id;
			}
			else if (_currentSolution.Projects.SelectMany(p => p.AdditionalDocuments).FirstOrDefault(d => string.Equals(d.FilePath, file, StringComparison.OrdinalIgnoreCase)) is AdditionalDocument additionalDocument)
			{
				var sourceText = await GetSourceTextAsync(file);
				updatedSolution = _currentSolution.WithAdditionalDocumentText(additionalDocument.Id, sourceText, PreservationMode.PreserveValue);
				updatedProjectId = additionalDocument.Project.Id;
			}
			else
			{
				_reporter.Verbose($"Could not find document with path {file} in the workspace.");
				// HotReloadEventSource.Log.HotReloadEnd(HotReloadEventSource.StartType.CompilationHandler);
				return false;
			}

			var (updates, hotReloadDiagnostics) = await _hotReloadService.EmitSolutionUpdateAsync(updatedSolution, cancellationToken);

			Console.WriteLine($"Got results after {sw.Elapsed}");

			if (hotReloadDiagnostics.IsDefaultOrEmpty && updates.IsDefaultOrEmpty)
			{
				// It's possible that there are compilation errors which prevented the solution update
				// from being updated. Let's look to see if there are compilation errors.
				var diagnostics = GetDiagnostics(updatedSolution, cancellationToken);
				if (diagnostics.IsDefaultOrEmpty)
				{
					await UpdateMetadata(file, updates);
				}
				else
				{
					Console.WriteLine($"Got {diagnostics.Length} diagnostics");
				}

				// HotReloadEventSource.Log.HotReloadEnd(HotReloadEventSource.StartType.CompilationHandler);
				// Even if there were diagnostics, continue treating this as a success
				return true;
			}

			if (!hotReloadDiagnostics.IsDefaultOrEmpty)
			{
				// Rude edit.
				_reporter.Output("Unable to apply hot reload because of a rude edit. Rebuilding the app...");
				foreach (var diagnostic in hotReloadDiagnostics)
				{
					_reporter.Verbose(CSharpDiagnosticFormatter.Instance.Format(diagnostic, CultureInfo.InvariantCulture));
				}

				// HotReloadEventSource.Log.HotReloadEnd(HotReloadEventSource.StartType.CompilationHandler);
				return false;
			}

			_currentSolution = updatedSolution;

			sw.Stop();

			await UpdateMetadata(file, updates);

			// HotReloadEventSource.Log.HotReloadEnd(HotReloadEventSource.StartType.CompilationHandler);
			return true;

			async Task UpdateMetadata(string file, ImmutableArray<WatchHotReloadService.Update> updates)
			{
				for (int i = 0; i < updates.Length; i++)
				{
					await _remoteControlServer.SendFrame(
						new AssemblyDeltaReload()
						{
							FilePath = file,
							ModuleId = updates[i].ModuleId.ToString(),
							PdbDelta = Convert.ToBase64String(updates[i].PdbDelta.ToArray()),
							ILDelta = Convert.ToBase64String(updates[i].ILDelta.ToArray()),
							MetadataDelta = Convert.ToBase64String(updates[i].MetadataDelta.ToArray()),
						});
				}
			}
		}

		private async ValueTask<SourceText> GetSourceTextAsync(string filePath)
		{
			for (var attemptIndex = 0; attemptIndex < 6; attemptIndex++)
			{
				try
				{
					using var stream = File.OpenRead(filePath);
					return SourceText.From(stream, Encoding.UTF8);
				}
				catch (IOException) when (attemptIndex < 5)
				{
					await Task.Delay(20 * (attemptIndex + 1));
				}
			}

			Debug.Fail("This shouldn't happen.");
			return null;
		}


		private ImmutableArray<string> GetDiagnostics(Solution solution, CancellationToken cancellationToken)
		{
			var @lock = new object();
			var builder = ImmutableArray<string>.Empty;
			Parallel.ForEach(solution.Projects, project =>
			{
				if (!project.TryGetCompilation(out var compilation))
				{
					return;
				}

				var compilationDiagnostics = compilation.GetDiagnostics(cancellationToken);
				if (compilationDiagnostics.IsDefaultOrEmpty)
				{
					return;
				}

				var projectDiagnostics = ImmutableArray<string>.Empty;
				foreach (var item in compilationDiagnostics)
				{
					if (item.Severity == DiagnosticSeverity.Error)
					{
						var diagnostic = CSharpDiagnosticFormatter.Instance.Format(item, CultureInfo.InvariantCulture);
						_reporter.Output(diagnostic);
						projectDiagnostics = projectDiagnostics.Add(diagnostic);
					}
				}

				lock (@lock)
				{
					builder = builder.AddRange(projectDiagnostics);
				}
			});

			return builder;
		}



		private async ValueTask<bool> EnsureSolutionInitializedAsync()
		{
			if (_currentSolution != null)
			{
				return true;
			}

			if (_initializeTask is null)
			{
				return false;
			}

			try
			{
				(_currentSolution, _hotReloadService) = await _initializeTask;
				return true;
			}
			catch (Exception ex)
			{
				_reporter.Warn(ex.Message);
				return false;
			}
		}
	}
}
#endif
