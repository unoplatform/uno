using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Uno.UI.RemoteControl.Messaging.IdeChannel;
using Uno.UI.RemoteControl.VS.Helpers;
using Uno.UI.RemoteControl.VS.IdeChannel;
using RoslynProjectId = Microsoft.CodeAnalysis.ProjectId;
using RoslynSolution = Microsoft.CodeAnalysis.Solution;
using Task = System.Threading.Tasks.Task;
using VisualStudioWorkspace = Microsoft.VisualStudio.LanguageServices.VisualStudioWorkspace;

#pragma warning disable VSTHRD010
#pragma warning disable VSTHRD109

namespace Uno.UI.RemoteControl.VS;

/// <summary>
/// IDE-side counterpart of the dev-server's <c>IdeFileUpdater</c> (spec 052): applies all the
/// content edits of an <see cref="UpdateFileRequestIdeMessage"/> in order — without saving open
/// documents — then waits for the Roslyn workspace to be able to compile the change-set before
/// performing the deferred saves and triggering EnC ("Debug.ApplyCodeChanges"), so Visual
/// Studio never evaluates an intermediate state of the batch.
/// </summary>
internal sealed class VisualStudioFileUpdater(
	DTE dte,
	DTE2 dte2,
	AsyncPackage asyncPackage,
	Func<IdeChannelClient?> ideChannelClient,
	Action<string> debug,
	CancellationToken ct)
{
	public async Task ProcessAsync(UpdateFileRequestIdeMessage request)
	{
		try
		{
			debug($"BatchUpdate #{request.CorrelationId}: received {request.Edits.Length} edit(s) for request {request.RequestId}.");

			var createdFiles = new List<string>();
			var deferredSaves = new List<Document>();

			foreach (var edit in request.Edits)
			{
				// Deletes are applied by the dev-server; every forwarded edit carries content.
				if (edit.NewText is not { Length: > 0 } newText)
				{
					continue;
				}

				var filePath = Path.GetFullPath(edit.FilePath);
				if (!File.Exists(filePath))
				{
					createdFiles.Add(filePath);
				}

				// Do not save open documents yet: a VS save can trigger "hot reload on save",
				// which must not evaluate the change-set before the workspace is ready (spec 052).
				if (await ApplyFileContentAsync(filePath, newText, saveOpenDocument: false) is { } openDocument
					&& (request.ForceSaveOnDisk ?? true))
				{
					deferredSaves.Add(openDocument);
				}
			}

			// Ack once the writes are applied: the readiness wait and the hot-reload trigger run
			// asynchronously, and their outcome flows through the hot-reload operation channel.
			if (ideChannelClient() is { } channel)
			{
				await channel.SendToDevServerAsync(new IdeResultMessage(request.CorrelationId, Result.Success()), ct);
			}

			// Finalization runs on a VS-tracked task (not a raw thread-pool Task.Run): the DTE
			// calls inside marshal to the UI thread. It also runs when the hot-reload trigger is
			// disabled — the deferred saves must be flushed regardless (ForceSaveOnDisk semantics
			// do not depend on the trigger).
			if (!request.IsForceHotReloadDisabled || deferredSaves.Count > 0)
			{
				_ = ThreadHelper.JoinableTaskFactory.RunAsync(() => WaitReadinessAndTriggerHotReloadAsync(request.IsForceHotReloadDisabled, createdFiles, deferredSaves, ct));
			}
		}
		catch (Exception e)
		{
			if (ideChannelClient() is { } channel)
			{
				// Send a message back to indicate that the request has failed.
				await channel.SendToDevServerAsync(new IdeResultMessage(request.CorrelationId, Result.Fail(e)), ct);
			}
			else
			{
				// No channel to ack on (early init / teardown): the dev-server will only give up
				// on its wait-for-IDE timeout — leave a trace so that wait is explainable.
				debug($"BatchUpdate #{request.CorrelationId}: failed with no channel to ack ({e.Message}).");
			}

			throw;
		}
	}

	/// <summary>
	/// Applies <paramref name="fileContent"/> to <paramref name="filePath"/> — in-memory when the
	/// document is open in the IDE, on disk otherwise — and returns the open document whose save
	/// was skipped so the caller can perform it later (batched updates save only after workspace
	/// readiness, see spec 052), or <see langword="null"/> when nothing is left to save.
	/// </summary>
	public async Task<Document?> ApplyFileContentAsync(string filePath, string fileContent, bool saveOpenDocument)
	{
		// Determine the appropriate encoding for the file
		var currentEncoding = EncodingHelpers.DetectFileEncoding(filePath);
		var targetEncoding = EncodingHelpers.GetCompatibleEncoding(currentEncoding, fileContent);

		// Check if document is already open in IDE
		var document = dte2
			.Documents
			.OfType<Document>()
			.FirstOrDefault(d => d.FullName.Equals(filePath, StringComparison.OrdinalIgnoreCase));

		var shouldReopenDocument = false;

		// If document is open but encoding needs to be changed, we close it
		if (document is not null && currentEncoding != targetEncoding)
		{
			// Document is open but does not have the current encoding, save it, then close it to change encoding
			debug($"Document {Path.GetFileName(filePath)} is open, saving and closing to change encoding from {currentEncoding.EncodingName} to {targetEncoding.EncodingName} with BOM");

			try
			{
				document.Save();
			}
			catch
			{
				// Ignore save errors
			}

			document.Close(vsSaveChanges.vsSaveChangesNo);
			shouldReopenDocument = true;
			await Task.Delay(250); // Small delay to ensure file system is ready
		}

		// If the file is open and encoding compatible, we update its content in-memory
		// TODO: We should NOT assume the `fileContent` to contains the full document content!
		if (!shouldReopenDocument && document?.Object("TextDocument") as TextDocument is { } textDocument && currentEncoding == targetEncoding)
		{
			debug($"Updating {Path.GetFileName(filePath)} (in memory).");

			// Flags: 0b0000_0011 = vsEPReplaceTextOptions.vsEPReplaceTextKeepMarkers | vsEPReplaceTextOptions.vsEPReplaceTextNormalizeNewLines
			// https://learn.microsoft.com/en-us/dotnet/api/envdte.vsepreplacetextoptions?view=visualstudiosdk-2022#fields
			const int flags = 0b0000_0011;
			textDocument
				.StartPoint
				.CreateEditPoint()
				.ReplaceText(textDocument.EndPoint, fileContent, flags);

			if (saveOpenDocument)
			{
				// Save the document.
				document.Save();
				return null;
			}

			// The save is deferred to the caller (a VS save can trigger "hot reload on save",
			// which must not evaluate the change-set before the workspace is ready).
			return document;
		}
		else
		{
			debug($"Updating {Path.GetFileName(filePath)} (on disk).");

			File.WriteAllText(filePath, fileContent, targetEncoding);

			if (document is not null)
			{
				// Re-open the document to reflect changes in IDE
				await Task.Delay(250); // Small delay to ensure file system is ready
				dte2.Documents.Open(filePath);
			}

			return null;
		}
	}

	private async Task WaitReadinessAndTriggerHotReloadAsync(bool isForceHotReloadDisabled, List<string> createdFiles, List<Document> deferredSaves, CancellationToken ct)
	{
		try
		{
			var stopwatch = Stopwatch.StartNew();
			if (!isForceHotReloadDisabled && createdFiles.Count > 0)
			{
				// DTE is STA COM: marshal to the UI thread for the project-system mutation, then
				// run the readiness polling through the thread pool so the UI thread is not held
				// for up to the 10 s readiness budget.
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(ct);
				NudgeProjectSystem(createdFiles);

				await Task.Run(() => WaitForWorkspaceReadinessAsync(createdFiles, TimeSpan.FromSeconds(10), ct), ct);
			}

			// document.Save() and ExecuteCommand are DTE calls too — back to the UI thread (this
			// also covers the trigger-disabled path, which never switched above).
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(ct);

			// Deferred saves: performed only once the workspace can compile the change-set, so a
			// save-triggered hot reload evaluates a coherent snapshot. Flushed even when the
			// hot-reload trigger is disabled — ForceSaveOnDisk semantics do not depend on it.
			foreach (var document in deferredSaves)
			{
				try
				{
					document.Save();
				}
				catch (Exception e)
				{
					debug($"Failed to save {document.FullName}: {e.Message}");
				}
			}

			if (isForceHotReloadDisabled)
			{
				return;
			}

			// Programmatically trigger the "Apply Code Changes" command in Visual Studio,
			// which will trigger the hot reload (same mechanics as ForceHotReloadIdeMessage).
			debug($"BatchUpdate: triggering Debug.ApplyCodeChanges (readiness + deferred saves took {stopwatch.ElapsedMilliseconds} ms).");
			dte.ExecuteCommand("Debug.ApplyCodeChanges");
		}
		catch (OperationCanceledException)
		{
			// Shutdown/teardown: expected, not a trigger failure — keep the log signal clean.
		}
		catch (Exception e)
		{
			debug($"Batched hot-reload trigger failed: {e.Message}");
		}
	}

	/// <summary>
	/// Waits until the VS Roslyn workspace can actually compile the change-set containing
	/// <paramref name="createdFiles"/>. Being known to the project system is NOT enough: a
	/// created .xaml is listed as AdditionalDocument before its item metadata is complete, and
	/// the XAML generator silently skips it until then — EnC would evaluate a compilation
	/// without the generated partial (CS1061 on InitializeComponent). The wait stays passive
	/// (cheap snapshot reads only): forcing compilations while polling starves the design-time
	/// build being awaited, and waiting for the workspace's own generated documents is
	/// pointless — VS runs source generators in "balanced" mode (re-run on save/build only, so
	/// they stay frozen during the wait), while the EnC delta-builder re-runs them itself on
	/// its own snapshot at apply time. The delivered item metadata is therefore the exact
	/// readiness signal.
	/// Bounded by <paramref name="timeout"/>; on expiry the caller proceeds anyway.
	/// </summary>
	private async Task WaitForWorkspaceReadinessAsync(List<string> createdFiles, TimeSpan timeout, CancellationToken ct)
	{
		if (await asyncPackage.GetServiceAsync(typeof(SComponentModel)) is not IComponentModel componentModel
			|| componentModel.GetService<VisualStudioWorkspace>() is not { } workspace)
		{
			debug("Roslyn workspace is not available; triggering hot reload without readiness wait.");
			return;
		}

		var stopwatch = Stopwatch.StartNew();
		RoslynSolution? lastChecked = null;
		var lastCheckAt = TimeSpan.MinValue;
		string? lastReason = null;
		while (stopwatch.Elapsed < timeout)
		{
			ct.ThrowIfCancellationRequested();

			// Re-check when the workspace produced a new snapshot (the wait is on project
			// system / design-time build activity — redoing work on every tick would compete
			// with the build we are waiting for), plus a forced re-check every second as a
			// safety net: a false negative evaluated on what turns out to be the final
			// snapshot must not stall until the timeout.
			var solution = workspace.CurrentSolution;
			if (!ReferenceEquals(solution, lastChecked) || stopwatch.Elapsed - lastCheckAt >= TimeSpan.FromSeconds(1))
			{
				lastChecked = solution;
				lastCheckAt = stopwatch.Elapsed;

				lastReason = GetNotReadyReason(solution, createdFiles);
				if (lastReason is null)
				{
					debug($"BatchUpdate: workspace compiles the full change-set after {stopwatch.ElapsedMilliseconds} ms.");
					return;
				}
			}

			await Task.Delay(100, ct);
		}

		debug($"Workspace readiness timed out after {timeout} (last reason: {lastReason ?? "none"}); triggering hot reload anyway.");
	}

	/// <summary>
	/// Returns <see langword="null"/> when the workspace can compile the change-set, otherwise
	/// the precise first reason it cannot. Stages per created file: part of a Roslyn project →
	/// (.xaml only) surfaced as analyzer AdditionalFile → its
	/// "build_metadata.AdditionalFiles.SourceItemGroup" analyzer-config option delivered — the
	/// exact predicate Uno's XAML generator uses to accept a file, and it only appears once
	/// the design-time build ran. Deliberately NOT checked: the workspace's generated
	/// documents — VS runs generators in "balanced" mode so they stay frozen until a
	/// save/build and would never show the new page during the wait, while the EnC
	/// delta-builder re-runs generators itself at apply time (verified: it emits the new
	/// page's generated partial while the workspace still exposes the stale set).
	/// All stages are cheap snapshot reads — no compilation, no generator run.
	/// </summary>
	private static string? GetNotReadyReason(RoslynSolution solution, List<string> createdFiles)
	{
		foreach (var file in createdFiles)
		{
			var name = Path.GetFileName(file);
			var projectIds = GetProjectsContaining(solution, file).Distinct().ToList();
			if (projectIds.Count == 0)
			{
				return $"{name} is not part of any Roslyn project yet";
			}

			if (!file.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
			{
				// A regular document contributes its syntax tree to the compilation by
				// construction — being part of a project is enough.
				continue;
			}

			foreach (var projectId in projectIds)
			{
				if (solution.GetProject(projectId) is not { } project)
				{
					return $"{name}: project {projectId} vanished from the snapshot";
				}

				var additionalFile = project.AnalyzerOptions.AdditionalFiles
					.FirstOrDefault(f => string.Equals(f.Path, file, StringComparison.OrdinalIgnoreCase));
				if (additionalFile is null)
				{
					return $"{name} is not surfaced as an analyzer AdditionalFile yet (project '{project.Name}', {project.AnalyzerOptions.AdditionalFiles.Length} additional file(s))";
				}

				if (!project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(additionalFile)
						.TryGetValue("build_metadata.AdditionalFiles.SourceItemGroup", out var sourceItemGroup)
					|| string.IsNullOrEmpty(sourceItemGroup))
				{
					return $"{name}: SourceItemGroup item metadata not delivered yet (project '{project.Name}')";
				}
			}
		}

		return null;
	}

	/// <summary>
	/// Routes created files through the project system (DTE <c>AddFromFile</c>) instead of
	/// relying on the file watcher: the watcher path is debounced and its design-time build
	/// runs at low priority while debugging (observed: 8-10 s before the AdditionalFiles item
	/// metadata shows up), whereas an explicit add is processed promptly — like a user adding
	/// the file in Solution Explorer. For SDK-style globbing projects the call does not modify
	/// the csproj: the item is already matched by the glob.
	/// </summary>
	private void NudgeProjectSystem(List<string> createdFiles)
	{
		foreach (var file in createdFiles)
		{
			try
			{
				var stopwatch = Stopwatch.StartNew();
				if (dte.Solution.FindProjectItem(file) is not null)
				{
					// Already picked up by the project system (globs are fast) — the readiness
					// gate below still waits for its item metadata.
					continue;
				}

				if (FindContainingProject(file) is { } project)
				{
					project.ProjectItems.AddFromFile(file);
					debug($"BatchUpdate: added {Path.GetFileName(file)} to project {project.Name} in {stopwatch.ElapsedMilliseconds} ms (project-system nudge).");
				}
				else
				{
					debug($"BatchUpdate: no containing project found for {Path.GetFileName(file)} — no nudge, the file watcher will pick it up.");
				}
			}
			catch (Exception e)
			{
				debug($"BatchUpdate: project-system nudge failed for {Path.GetFileName(file)}: {e.Message} — the file watcher will pick it up.");
			}
		}
	}

	private Project? FindContainingProject(string filePath)
	{
		Project? best = null;
		var bestLength = -1;

		void Visit(Project? project)
		{
			if (project is null)
			{
				return;
			}

			try
			{
				if (project.Kind == ProjectKinds.vsProjectKindSolutionFolder)
				{
					foreach (ProjectItem item in project.ProjectItems)
					{
						Visit(item.SubProject);
					}
				}
				else if (project.FullName is { Length: > 0 } projectPath
					&& Path.GetDirectoryName(projectPath) is { Length: > 0 } projectDir
					&& filePath.StartsWith(projectDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
					&& projectDir.Length > bestLength)
				{
					best = project;
					bestLength = projectDir.Length;
				}
			}
			catch
			{
				// Some project nodes (unloaded, miscellaneous) throw on property access — skip them.
			}
		}

		foreach (Project project in dte.Solution.Projects)
		{
			Visit(project);
		}

		return best;
	}

	private static IEnumerable<RoslynProjectId> GetProjectsContaining(RoslynSolution solution, string filePath)
		=> solution
			.GetDocumentIdsWithFilePath(filePath)
			.Select(documentId => documentId.ProjectId)
			.Concat(solution
				.Projects
				.Where(project => project.AdditionalDocuments.Any(additional => string.Equals(additional.FilePath, filePath, StringComparison.OrdinalIgnoreCase)))
				.Select(project => project.Id));
}
