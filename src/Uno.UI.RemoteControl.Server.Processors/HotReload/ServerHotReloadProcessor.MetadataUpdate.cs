#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
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
using Uno.UI.RemoteControl.HotReload;
using Uno.UI.RemoteControl.HotReload.Messages;
using Uno.UI.RemoteControl.Messaging.HotReload;

namespace Uno.UI.RemoteControl.Host.HotReload
{
	partial class ServerHotReloadProcessor : IServerProcessor, IDisposable
	{
		private static readonly StringComparer _pathsComparer = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

		private FileSystemWatcher[]? _solutionWatchers;
		private CompositeDisposable? _solutionWatcherEventsDisposable;

		private Task<(Solution, WatchHotReloadService)>? _initializeTask;
		private Solution? _currentSolution;
		private WatchHotReloadService? _hotReloadService;
		private IReporter _reporter = new Reporter();

		private bool _useRoslynHotReload;

		private bool InitializeMetadataUpdater(ConfigureServer configureServer)
		{
			_ = bool.TryParse(_remoteControlServer.GetServerConfiguration("metadata-updates"), out _useRoslynHotReload);

			_useRoslynHotReload = _useRoslynHotReload || configureServer.EnableMetadataUpdates;

			if (_useRoslynHotReload)
			{
				CompilationWorkspaceProvider.InitializeRoslyn(Path.GetDirectoryName(configureServer.ProjectPath));

				InitializeInner(configureServer);

				return true;
			}
			else
			{
				return false;
			}
		}

		private void InitializeInner(ConfigureServer configureServer) => _initializeTask = Task.Run(
			async () =>
			{
				try
				{
					await Notify(HotReloadEvent.Initializing);

					var result = await CompilationWorkspaceProvider.CreateWorkspaceAsync(
						configureServer.ProjectPath,
						_reporter,
						configureServer.MetadataUpdateCapabilities,
						configureServer.MSBuildProperties,
						CancellationToken.None);

					ObserveSolutionPaths(result.Item1);

					await _remoteControlServer.SendFrame(new HotReloadWorkspaceLoadResult { WorkspaceInitialized = true });
					await Notify(HotReloadEvent.Ready);

					return result;
				}
				catch (Exception e)
				{
					Console.WriteLine($"Failed to initialize compilation workspace: {e}");

					await _remoteControlServer.SendFrame(new HotReloadWorkspaceLoadResult { WorkspaceInitialized = false });
					await Notify(HotReloadEvent.Disabled);

					throw;
				}
			},
			CancellationToken.None);

		private void ObserveSolutionPaths(Solution solution)
		{
			var observedPaths =
				solution.Projects
					.SelectMany(p => p
						.Documents
						.Select(d => d.FilePath)
						.Concat(p.AdditionalDocuments
							.Select(d => d.FilePath)))
					.Select(p => Path.GetDirectoryName(p))
					.Distinct()
					.ToArray();

#if DEBUG
			Console.WriteLine($"Observing paths {string.Join(", ", observedPaths)}");
#endif

			_solutionWatchers = observedPaths
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
				var disposable = ToObservable(watcher).Subscribe(
					filePaths => _ = ProcessMetadataChanges(filePaths.Distinct()),
					e => Console.WriteLine($"Error {e}"));

				_solutionWatcherEventsDisposable.Add(disposable);
			}
		}

		private async Task ProcessMetadataChanges(IEnumerable<string> filePaths)
		{
			if (_useRoslynHotReload) // Note: Always true here?!
			{
				var files = filePaths.ToImmutableHashSet(_pathsComparer);
				var hotReload = await StartOrContinueHotReload(files);

				try
				{
					// Note: We should process all files at once here!
					foreach (var file in files)
					{
						ProcessSolutionChanged(hotReload, file, CancellationToken.None).Wait();
					}
				}
				catch (Exception e)
				{
					_reporter.Warn($"Internal error while processing hot-reload ({e.Message}).");
				}
				finally
				{
					await hotReload.CompleteUsingIntermediates();
				}
			}
		}

		private async Task<bool> ProcessSolutionChanged(HotReloadServerOperation hotReload, string file, CancellationToken cancellationToken)
		{
			if (!await EnsureSolutionInitializedAsync() || _currentSolution is null || _hotReloadService is null)
			{
				return false;
			}

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

				// Generate an empty document to force the generators to run
				// in a separate project of the same solution. This is not needed
				// for the head project, but it's no causing issues either.
				var docName = Guid.NewGuid().ToString();
				updatedSolution = updatedSolution.AddAdditionalDocument(
					DocumentId.CreateNewId(updatedProjectId),
					docName,
					SourceText.From("")
				);
			}
			else
			{
				_reporter.Verbose($"Could not find document with path {file} in the workspace.");
				// HotReloadEventSource.Log.HotReloadEnd(HotReloadEventSource.StartType.CompilationHandler);
				return false;
			}


			var (updates, hotReloadDiagnostics) = await _hotReloadService.EmitSolutionUpdateAsync(updatedSolution, cancellationToken);
			var hasErrorDiagnostics = hotReloadDiagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

			_reporter.Output($"Found {updates.Length} metadata updates after {sw.Elapsed}");

			if (hasErrorDiagnostics && updates.IsDefaultOrEmpty)
			{
				// It's possible that there are compilation errors which prevented the solution update
				// from being updated. Let's look to see if there are compilation errors.
				var diagnostics = GetErrorDiagnostics(updatedSolution, cancellationToken);
				if (diagnostics.IsDefaultOrEmpty)
				{
					await UpdateMetadata(file, updates);
					hotReload.NotifyIntermediate(file, HotReloadServerResult.NoChanges);
				}
				else
				{
					_reporter.Output($"Got {diagnostics.Length} errors");
					hotReload.NotifyIntermediate(file, HotReloadServerResult.Failed);
				}

				// HotReloadEventSource.Log.HotReloadEnd(HotReloadEventSource.StartType.CompilationHandler);
				// Even if there were diagnostics, continue treating this as a success
				return true;
			}

			if (hasErrorDiagnostics)
			{
				// Rude edit.
				_reporter.Output("Unable to apply hot reload because of a rude edit. Rebuilding the app...");
				foreach (var diagnostic in hotReloadDiagnostics)
				{
					_reporter.Verbose(CSharpDiagnosticFormatter.Instance.Format(diagnostic, CultureInfo.InvariantCulture));
				}

				hotReload.NotifyIntermediate(file, HotReloadServerResult.RudeEdit);

				// HotReloadEventSource.Log.HotReloadEnd(HotReloadEventSource.StartType.CompilationHandler);
				return false;
			}

			_currentSolution = updatedSolution;

			sw.Stop();

			await UpdateMetadata(file, updates);
			hotReload.NotifyIntermediate(file, HotReloadServerResult.Success);

			// HotReloadEventSource.Log.HotReloadEnd(HotReloadEventSource.StartType.CompilationHandler);
			return true;

			async Task UpdateMetadata(string file, ImmutableArray<WatchHotReloadService.Update> updates)
			{
#if DEBUG
				_reporter.Output($"Sending {updates.Length} metadata updates for {file}");
#endif

				for (int i = 0; i < updates.Length; i++)
				{
					var updateTypesWriterStream = new MemoryStream();
					var updateTypesWriter = new BinaryWriter(updateTypesWriterStream);
					WriteIntArray(updateTypesWriter, updates[i].UpdatedTypes.ToArray());

					await _remoteControlServer.SendFrame(
						new AssemblyDeltaReload()
						{
							FilePath = file,
							ModuleId = updates[i].ModuleId.ToString(),
							PdbDelta = Convert.ToBase64String(updates[i].PdbDelta.ToArray()),
							ILDelta = Convert.ToBase64String(updates[i].ILDelta.ToArray()),
							MetadataDelta = Convert.ToBase64String(updates[i].MetadataDelta.ToArray()),
							UpdatedTypes = Convert.ToBase64String(updateTypesWriterStream.ToArray()),
						});
				}
			}

			static void WriteIntArray(BinaryWriter binaryWriter, int[] values)
			{
				if (values is null)
				{
					binaryWriter.Write(0);
					return;
				}

				binaryWriter.Write(values.Length);
				foreach (var value in values)
				{
					binaryWriter.Write(value);
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

		private ImmutableArray<string> GetErrorDiagnostics(Solution solution, CancellationToken cancellationToken)
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


		[MemberNotNullWhen(true, nameof(_currentSolution), nameof(_hotReloadService))]
		private async ValueTask<bool> EnsureSolutionInitializedAsync()
		{
			if (_currentSolution is not null && _hotReloadService is not null)
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
