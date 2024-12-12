#nullable enable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Uno.UI.RemoteControl.Host.HotReload.MetadataUpdates;
using Uno.UI.RemoteControl.HotReload.Messages;
using Uno.UI.RemoteControl.Messaging.HotReload;

namespace Uno.UI.RemoteControl.Host.HotReload
{
	partial class ServerHotReloadProcessor : IServerProcessor, IDisposable
	{
		private static readonly StringComparer _pathsComparer = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

		private FileSystemWatcher[]? _solutionWatchers;
		private IDisposable? _solutionWatcherEventsDisposable;

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
						configureServer.MSBuildProperties.Where(kvp => !kvp.Key.StartsWith("MSBuild", StringComparison.OrdinalIgnoreCase)).ToDictionary(),
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
					.Select(Path.GetDirectoryName)
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

			_solutionWatcherEventsDisposable = To2StepsObservable(_solutionWatchers, HasInterest).Subscribe(
				filePaths => _ = ProcessMetadataChanges(filePaths),
				e => Console.WriteLine($"Error {e}"));

			static bool HasInterest(string file)
				=> Path.GetExtension(file).ToLowerInvariant() is not ".csproj" and not ".editorconfig";
		}

		private async Task ProcessMetadataChanges(Task<ImmutableHashSet<string>> filesAsync)
		{
			// Notify the start of the hot-reload processing as soon as possible, even before the buffering of file change is completed
			var hotReload = await StartOrContinueHotReload();
			var files = await filesAsync;
			if (!hotReload.TryMerge(files))
			{
				hotReload = await StartHotReload(files);
			}

			try
			{
				await ProcessSolutionChanged(hotReload, files, CancellationToken.None);
			}
			catch (Exception e)
			{
				_reporter.Warn($"Internal error while processing hot-reload ({e.Message}).");

				await hotReload.Complete(HotReloadServerResult.InternalError, e);
			}
		}

		private async ValueTask ProcessSolutionChanged(HotReloadServerOperation hotReload, ImmutableHashSet<string> files, CancellationToken ct)
		{
			if (!await EnsureSolutionInitializedAsync() || _currentSolution is null || _hotReloadService is null)
			{
				await hotReload.Complete(HotReloadServerResult.Failed); // Failed to init the workspace
				return;
			}

			var sw = Stopwatch.StartNew();

			// Edit the files in the workspace.
			var solution = _currentSolution;
			foreach (var file in files)
			{
				if (solution.Projects.SelectMany(p => p.Documents).FirstOrDefault(d => string.Equals(d.FilePath, file, StringComparison.OrdinalIgnoreCase)) is Document documentToUpdate)
				{
					var sourceText = await GetSourceTextAsync(file);
					solution = documentToUpdate.WithText(sourceText).Project.Solution;
				}
				else if (solution.Projects.SelectMany(p => p.AdditionalDocuments).FirstOrDefault(d => string.Equals(d.FilePath, file, StringComparison.OrdinalIgnoreCase)) is AdditionalDocument additionalDocument)
				{
					var sourceText = await GetSourceTextAsync(file);
					solution = solution.WithAdditionalDocumentText(additionalDocument.Id, sourceText, PreservationMode.PreserveValue);

					// Generate an empty document to force the generators to run
					// in a separate project of the same solution. This is not needed
					// for the head project, but it's no causing issues either.
					var docName = Guid.NewGuid().ToString();
					solution = solution.AddAdditionalDocument(
						DocumentId.CreateNewId(additionalDocument.Project.Id),
						docName,
						SourceText.From("")
					);
				}
				else
				{
					_reporter.Verbose($"Could not find document with path {file} in the workspace.");
				}
			}

			// Not mater if the build will succeed or not, we update the _currentSolution.
			// Files needs to be updated again to fix the compilation errors.
			if (solution == _currentSolution)
			{
				_reporter.Output($"No changes found in {string.Join(",", files.Select(Path.GetFileName))}");

				await hotReload.Complete(HotReloadServerResult.NoChanges);
				return;
			}

			_currentSolution = solution;

			// Compile the solution and get deltas
			var (updates, hotReloadDiagnostics) = await _hotReloadService.EmitSolutionUpdateAsync(solution, ct);
			// hotReloadDiagnostics currently includes semantic Warnings and Errors for types being updated. We want to limit rude edits to the class
			// of unrecoverable errors that a user cannot fix and requires an app rebuild.
			var rudeEdits = hotReloadDiagnostics.RemoveAll(d => d.Severity == DiagnosticSeverity.Warning || !d.Descriptor.Id.StartsWith("ENC", StringComparison.Ordinal));

			_reporter.Output($"Found {updates.Length} metadata updates after {sw.Elapsed}");
			sw.Stop();


			if (rudeEdits.IsEmpty && updates.IsEmpty)
			{
				var compilationErrors = GetCompilationErrors(solution, ct);
				if (compilationErrors.IsEmpty)
				{
					_reporter.Output("No hot reload changes to apply.");
					await hotReload.Complete(HotReloadServerResult.NoChanges);
				}
				else
				{
					await hotReload.Complete(HotReloadServerResult.Failed);
				}

				return;
			}

			if (!rudeEdits.IsEmpty)
			{
				// Rude edit.
				_reporter.Output("Unable to apply hot reload because of a rude edit.");
				foreach (var diagnostic in hotReloadDiagnostics)
				{
					_reporter.Verbose(CSharpDiagnosticFormatter.Instance.Format(diagnostic, CultureInfo.InvariantCulture));
				}

				await hotReload.Complete(HotReloadServerResult.RudeEdit);
				return;
			}

			await SendUpdates(updates);

			await hotReload.Complete(HotReloadServerResult.Success);

			async Task SendUpdates(ImmutableArray<WatchHotReloadService.Update> updates)
			{
#if DEBUG
				_reporter.Output($"Sending {updates.Length} metadata updates for {string.Join(",", files.Select(Path.GetFileName))}");
#endif

				for (var i = 0; i < updates.Length; i++)
				{
					var updateTypesWriterStream = new MemoryStream();
					var updateTypesWriter = new BinaryWriter(updateTypesWriterStream);
					WriteIntArray(updateTypesWriter, updates[i].UpdatedTypes.ToArray());

					await _remoteControlServer.SendFrame(
						new AssemblyDeltaReload
						{
							FilePaths = files,
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

		private ImmutableArray<string> GetCompilationErrors(Solution solution, CancellationToken cancellationToken)
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
				if (compilationDiagnostics.IsEmpty)
				{
					return;
				}

				var projectDiagnostics = ImmutableArray<string>.Empty;
				foreach (var item in compilationDiagnostics)
				{
					if (item.Severity == DiagnosticSeverity.Error)
					{
						var diagnostic = CSharpDiagnosticFormatter.Instance.Format(item, CultureInfo.InvariantCulture);
						_reporter.Output("\x1B[40m\x1B[31m" + diagnostic);
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
