#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Tasks.Deployment.Bootstrapper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Uno.Disposables;
using Uno.Threading;
using Uno.UI.RemoteControl.Helpers;
using Uno.UI.RemoteControl.Host.HotReload.MetadataUpdates;
using Uno.UI.RemoteControl.HotReload.Messages;
using Uno.UI.RemoteControl.Messaging.HotReload;
using Uno.UI.Tasks.HotReloadInfo;

namespace Uno.UI.RemoteControl.Host.HotReload
{
	partial class ServerHotReloadProcessor : IServerProcessor, IDisposable
	{
		private static readonly StringComparer _pathsComparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
		private static readonly StringComparison _pathsComparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

		private IDisposable? _solutionSubscriptions;
		private readonly FastAsyncLock _solutionUpdateGate = new();
		private readonly BufferGate _solutionWatchersGate = new();

		private Task<(Solution, WatchHotReloadService)>? _initializeTask;
		private Solution? _currentSolution;
		private WatchHotReloadService? _hotReloadService;
		private IReporter _reporter = new Reporter();

		private bool _useRoslynHotReload;
		private bool _useHotReloadThruDebugger;
		private ConfigureServer? _configureServer;

		private bool InitializeMetadataUpdater(ConfigureServer configureServer)
		{
			_ = bool.TryParse(_remoteControlServer.GetServerConfiguration("metadata-updates"), out _useRoslynHotReload);

			_useRoslynHotReload = _useRoslynHotReload || configureServer.EnableMetadataUpdates;
			_useHotReloadThruDebugger = configureServer.EnableHotReloadThruDebugger;

			if (_useRoslynHotReload)
			{
				// Assembly registrations must be done before the workspace is initialized
				// Not doing so will cause the roslyn msbuild workspace to fail to load because
				// of a missing path on assemblies loaded from a memory stream.
				CompilationWorkspaceProvider.RegisterAssemblyLoader();

				// Store configuration for later use when adding files
				_configureServer = configureServer;

				InitializeInner(configureServer);

				return true;
			}
			else
			{
				return false;
			}
		}

		private void InitializeInner(ConfigureServer configureServer)
		{
			try
			{
				if (Assembly.Load("Microsoft.CodeAnalysis.Workspaces") is { } wsAsm)
				{
					// If this assembly was loaded from a stream, it will not have a location.
					// This will indicate that the assembly loader from CompilationWorkspaceProvider
					// has been registered too late.
					if (string.IsNullOrEmpty(wsAsm.Location))
					{
						throw new InvalidOperationException("Microsoft.CodeAnalysis.Workspaces was loaded from a stream and must be loaded from a known path");
					}
				}

				CompilationWorkspaceProvider.InitializeRoslyn(Path.GetDirectoryName(configureServer.ProjectPath));

				_initializeTask = InitializeAsync(CancellationToken.None);
			}
			catch (Exception e)
			{
				Console.WriteLine($"Failed to initialize compilation workspace: {e}");

				_ = _remoteControlServer.SendFrame(new HotReloadWorkspaceLoadResult { WorkspaceInitialized = false });
				_ = Notify(HotReloadEvent.Disabled);

				throw;
			}
			async Task<(Solution, WatchHotReloadService)> InitializeAsync(CancellationToken ct)
			{
				try
				{
					await Notify(HotReloadEvent.Initializing);

					var (outputPath, intermediateOutputPath, solution, watch) = await CreateCompilation(configureServer, ct);

					ObserveSolutionPaths(solution, intermediateOutputPath, outputPath);

					await _remoteControlServer.SendFrame(new HotReloadWorkspaceLoadResult { WorkspaceInitialized = true });
					await Notify(HotReloadEvent.Ready);

					return (solution, watch);
				}
				catch (Exception e)
				{
					Console.WriteLine($"Failed to initialize compilation workspace: {e}");

					await _remoteControlServer.SendFrame(new HotReloadWorkspaceLoadResult { WorkspaceInitialized = false });
					await Notify(HotReloadEvent.Disabled);

					throw;
				}
			}
		}

		private async Task<(string? outputPath, string? intermediateOutputPath, Solution solution, WatchHotReloadService watch)> CreateCompilation(ConfigureServer configureServer, CancellationToken ct)
		{
			// Clone the properties from the ConfigureServer
			var properties = configureServer.MSBuildProperties.ToDictionary();

			// Flag the current build as created for hot reload, which allows for running targets or settings
			// props/items in the context of the hot reload workspace.
			properties["UnoIsHotReloadHost"] = "True";

			// If the runtime identifier NOT been used in the output path, this usually indicates that it was not passed as a parameter for the build
			// in that case we **must** not use it to init the hot-reload workspace (parameters are required to be exactly the same to get valid patches)
			// Note: This is required to get HR to work on Rider 2024.3 with Android
			// Note 2: We remove both properties to make sure to use the default behavior
			var appendIdToPath = properties.Remove("AppendRuntimeIdentifierToOutputPath", out var appendStr)
				&& bool.TryParse(appendStr, out var append)
				&& append;
			var hasOutputPath = properties.Remove("OutputPath", out var outputPath);
			properties.Remove("IntermediateOutputPath", out var intermediateOutputPath);

			if (properties.Remove("RuntimeIdentifier", out var runtimeIdentifier))
			{
				if (appendIdToPath && hasOutputPath && Path.TrimEndingDirectorySeparator(outputPath ?? "").EndsWith(runtimeIdentifier, StringComparison.OrdinalIgnoreCase))
				{
					// Set the RuntimeIdentifier as a temporary property so that we do not force the
					// property as a read-only global property that would be transitively applied to
					// projects that are not supporting the head's RuntimeIdentifier. (e.g. an android app
					// which references a netstd2.0 library project)
					properties["UnoHotReloadRuntimeIdentifier"] = runtimeIdentifier;
				}
			}

			// Pass the TargetFramework as a temporary property so that we do not force the tfm for all projects, but only the head project
			// (that references the Dev Server assembly which includes the target file to promote back the UnoHotReloadTargetFramework as TargetFramework).
			// This is required to make sure that an application referencing a class-lib project targeting a different TFM (e.g. net10 while head is net10-desktop)
			// can still be hot-reloaded.
			if (properties.Remove("TargetFramework", out var targetFramework))
			{
				properties["UnoHotReloadTargetFramework"] = targetFramework;
			}

			var (solution, watch) = await CompilationWorkspaceProvider.CreateWorkspaceAsync(
				configureServer.ProjectPath,
				_reporter,
				configureServer.MetadataUpdateCapabilities,
				properties,
				ct);
			return (outputPath, intermediateOutputPath, solution, watch);
		}

		private void ObserveSolutionPaths(Solution solution, params string?[] excludedDirPattern)
		{
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
			var watchersSubscription = To2StepsObservable(watchers, HasInterest, _solutionWatchersGate).Subscribe(
				filePaths => _ = ProcessFileChanges(filePaths, processing.Token),
				e => Console.WriteLine($"Error {e}"));

			_solutionSubscriptions = new CompositeDisposable([watchersSubscription, Disposable.Create(processing.Cancel), processing, .. watchers]);

			bool HasInterest(string file)
			{
				if (Path.GetExtension(file).ToLowerInvariant() is ".csproj" or ".editorconfig")
				{
					return false;
				}

				if (excludedDir.Any(dir => file.StartsWith(dir, _pathsComparison)))
				{
					// File is in an excluded directory (bin or obj)
					// However, we still allow changes from the HotReloadInfo
					if (!file.EndsWith(HotReloadInfoHelper.HotReloadInfoFilePath, _pathsComparison))
					{
						return false;
					}
				}

				return true;
			}
		}

		private async Task ProcessFileChanges(Task<ImmutableHashSet<string>> filesAsync, CancellationToken ct)
		{
			// Notify the start of the hot-reload processing as soon as possible, even before the buffering of file change is completed
			var hotReload = await StartOrContinueHotReload();
			var files = await filesAsync;
			if (!hotReload.TryMerge(files))
			{
				hotReload = await StartHotReload(files);
			}

			// Process the batch of files (sequentially!)
			try
			{
				using var _ = await _solutionUpdateGate.LockAsync(ct);
				await ProcessSolutionChanged(hotReload, files, ct);
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

			// Detects the changes and try to update the solution
			var originalSolution = _currentSolution;
			var changeSet = await DiscoverChangesAsync(originalSolution, files, ct);
			var solution = await Apply(originalSolution, changeSet, hotReload, ct);

			if (solution == _currentSolution)
			{
				_reporter.Output($"No changes found in {string.Join(",", files.Select(Path.GetFileName))}");

				await hotReload.Complete(HotReloadServerResult.NoChanges);
				return;
			}

			// Not mater if the build will succeed or not, we update the _currentSolution.
			// Files needs to be updated again to fix the compilation errors.
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
					await hotReload.Complete(HotReloadServerResult.Failed, diagnostics: hotReloadDiagnostics);
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

				await hotReload.Complete(HotReloadServerResult.RudeEdit, diagnostics: hotReloadDiagnostics);
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
					if (_useHotReloadThruDebugger)
					{
						if (!await _remoteControlServer.TrySendMessageToIDEAsync(
							new Uno.UI.RemoteControl.Messaging.IdeChannel.HotReloadThruDebuggerIdeMessage(
								updates[i].ModuleId.ToString(),
								Convert.ToBase64String(updates[i].MetadataDelta.ToArray()),
								Convert.ToBase64String(updates[i].ILDelta.ToArray()),
								Convert.ToBase64String(updates[i].PdbDelta.ToArray())
							),
							ct))
						{
							throw new InvalidOperationException("No active connection with the IDE to send update thru debugger.");
						}
					}
					else
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

		private static async ValueTask<SourceText> GetSourceTextAsync(string filePath, CancellationToken ct)
		{
			for (var attemptIndex = 0; attemptIndex < 6; attemptIndex++)
			{
				try
				{
					await using var stream = File.OpenRead(filePath);
					return SourceText.From(stream, Encoding.UTF8);
				}
				catch (IOException) when (attemptIndex < 5)
				{
					await Task.Delay(20 * (attemptIndex + 1), ct);
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

		private async ValueTask<ChangeSet> DiscoverChangesAsync(Solution solution, ImmutableHashSet<string> files, CancellationToken ct)
		{
			var editedDocuments = new List<Document>();
			var editedAdditionalDocuments = new List<TextDocument>();
			var removedDocuments = new List<DocumentId>();
			var removedAdditionalDocuments = new List<DocumentId>();
			var potentiallyAdded = new List<string>();
			var notFound = new List<string>();

			foreach (var file in files)
			{
				var found = false;
				var exists = File.Exists(file);
				var documents = solution
					.Projects
					.SelectMany(p => p.Documents)
					.Where(d => string.Equals(d.FilePath, file, StringComparison.OrdinalIgnoreCase));
				foreach (var document in documents)
				{
					found = true;
					if (exists)
					{
						editedDocuments.Add(document);
					}
					else
					{
						removedDocuments.Add(document.Id);
					}
				}

				var additionalDocuments = solution
					.Projects
					.SelectMany(p => p.AdditionalDocuments)
					.Where(d => string.Equals(d.FilePath, file, StringComparison.OrdinalIgnoreCase));
				foreach (var additionalDocument in additionalDocuments)
				{
					found = true;
					if (exists)
					{
						editedAdditionalDocuments.Add(additionalDocument);
					}
					else
					{
						removedAdditionalDocuments.Add(additionalDocument.Id);
					}
				}

				// Not found in current solution
				if (!found)
				{
					if (exists)
					{
						potentiallyAdded.Add(file);
					}
					else
					{
						_reporter.Verbose($"Could not find document with path '{file}' in the workspace and file does not exist on disk.");
						notFound.Add(file);
					}
				}
			}

			var added = await DiscoverNewFilesAsync(ImmutableHashSet.CreateRange(potentiallyAdded), ct);

			return new(
				[..editedDocuments],
				[..editedAdditionalDocuments],
				added.documents,
				added.additionalDocuments,
				[..removedDocuments],
				[..removedAdditionalDocuments],
				[..notFound, ..added.ignored]);
		}

		private async ValueTask<(ImmutableArray<AddedDocumentInfo> documents, ImmutableArray<AddedDocumentInfo> additionalDocuments, ImmutableHashSet<string> ignored)> DiscoverNewFilesAsync(
			ImmutableHashSet<string> newFiles,
			CancellationToken ct)
		{
			if (_configureServer is null || _currentSolution is null)
			{
				_reporter.Warn("Cannot handle new files: configuration not available.");
				return ([], [], newFiles);
			}

			if (newFiles is not { IsEmpty: false })
			{
				return ([], [], newFiles);
			}

			try
			{
				_reporter.Output($"Detected {newFiles.Count} potentially new file(s). Creating temporary workspace to discover them...");

				// Create a temporary workspace to discover the new files
				var (_, _, tempSolution, _) = await CreateCompilation(_configureServer, ct);

				var discoveredDocuments = ImmutableArray.CreateBuilder<AddedDocumentInfo>();
				var discoveredAdditionalDocuments = ImmutableArray.CreateBuilder<AddedDocumentInfo>();
				var ignoredFiles = ImmutableHashSet.CreateBuilder<string>();

				foreach (var file in newFiles)
				{
					// Search for the file in the temp workspace's projects
					// Note: Here again we assume that document can appear in more than one project (same project loaded with different TFM)
					var found = false;
					foreach (var project in tempSolution.Projects)
					{
						if (project.Documents.FirstOrDefault(d => string.Equals(d.FilePath, file, _pathsComparison)) is { } document)
						{
							found = true;
							discoveredDocuments.Add(new(project.GetInfo(), document.GetInfo()));
						}
						else if (project.AdditionalDocuments.FirstOrDefault(d => string.Equals(d.FilePath, file, _pathsComparison)) is { } additionalDocument)
						{
							found = true;
							discoveredAdditionalDocuments.Add(new(project.GetInfo(), additionalDocument.GetInfo()));
						}
					}

					if (!found)
					{
						ignoredFiles.Add(file);
					}
				}

				return (discoveredDocuments.ToImmutable(), discoveredAdditionalDocuments.ToImmutable(), ignoredFiles.ToImmutable());
			}
			catch (Exception ex)
			{
				_reporter.Warn($"Error while discovering new files: {ex.Message}");
				return ([], [], newFiles);
			}
		}

		private static async ValueTask<Solution> Apply(Solution solution, ChangeSet changeSet, HotReloadServerOperation hotReload, CancellationToken ct)
		{
			// Update existing documents
			foreach (var document in changeSet.EditedDocuments)
			{
				solution = solution.WithDocumentText(document.Id, await GetSourceTextAsync(document.FilePath!, ct));
				//_reporter.Output($"Updated document {Path.Combine([.. document.Folders, document.Name])}");
			}

			// Update existing additional documents
			foreach (var additionalDocument in changeSet.EditedAdditionalDocuments)
			{
				solution = solution.WithAdditionalDocumentText(additionalDocument.Id, await GetSourceTextAsync(additionalDocument.FilePath!, ct));
			}

			foreach (var projectWithEditedAdditionalDocument in changeSet.EditedAdditionalDocuments.Select(ad => ad.Project.Id).Distinct())
			{
				// Generate an empty document to force the generators to run in a separate project of the same solution.
				// This is not needed for the head project, but it's no causing issues either.
				solution = solution.AddAdditionalDocument(
					DocumentId.CreateNewId(projectWithEditedAdditionalDocument),
					Guid.NewGuid().ToString(),
					SourceText.From("")
				);
			}

			// Added documents has been detected using a temporary solution.
			// We need to make sure to find the right project instance in the current solution, and update the document ID accordingly.
			// Note: A project may appear multiple times in the solution (e.g. different TFM), so we need to add the document to **all** instances.
			foreach (var added in changeSet.AddedDocuments)
			{
				var found = false;
				var projects = solution.Projects.Where(p => p.FilePath == added.Project.FilePath);
				foreach (var project in projects)
				{
					found = true;
					solution = solution.AddDocument(added.Document.WithId(DocumentId.CreateNewId(project.Id)));
				}
				if (!found)
				{
					hotReload.NotifyIgnored(added.Document.FilePath!);
				}
			}

			foreach (var added in changeSet.AddedAdditionalDocuments)
			{
				var found = false;
				var projects = solution.Projects.Where(p => p.FilePath == added.Project.FilePath);
				foreach (var project in projects)
				{
					found = true;
					solution = solution.AddAdditionalDocument(added.Document.WithId(DocumentId.CreateNewId(project.Id)));
				}
				if (!found)
				{
					hotReload.NotifyIgnored(added.Document.FilePath!);
				}
			}

			solution = solution
				.RemoveDocuments(changeSet.RemovedDocuments)
				.RemoveAdditionalDocuments(changeSet.RemovedAdditionalDocuments);

			hotReload.NotifyIgnored(changeSet.IgnoredFiles);

			return solution;
		}

		private record ChangeSet(
			ImmutableArray<Document> EditedDocuments,
			ImmutableArray<TextDocument> EditedAdditionalDocuments,
			ImmutableArray<AddedDocumentInfo> AddedDocuments,
			ImmutableArray<AddedDocumentInfo> AddedAdditionalDocuments,
			ImmutableArray<DocumentId> RemovedDocuments,
			ImmutableArray<DocumentId> RemovedAdditionalDocuments,
			ImmutableHashSet<string> IgnoredFiles
		)
		{
			public static ChangeSet IgnoreAll(ImmutableHashSet<string> ignoredFiles)
				=> new([], [], [], [], [], [], ignoredFiles);
		}

		private record struct AddedDocumentInfo(ProjectInfo Project, DocumentInfo Document);
	}
}
