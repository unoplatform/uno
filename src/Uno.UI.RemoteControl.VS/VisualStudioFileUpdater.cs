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
using Microsoft.VisualStudio.Threading;
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
	// Finalizations (readiness wait → deferred saves / encoding upgrades → EnC trigger) start after
	// the ack is sent, so without ordering a later batch's trigger could overtake an earlier batch
	// still polling workspace readiness — re-exposing the intermediate snapshot this path exists to
	// avoid (spec 052). They are chained on a single serial task so they run in request order.
	private readonly object _finalizationGate = new();
	private JoinableTask _finalizationChain = ThreadHelper.JoinableTaskFactory.RunAsync(() => Task.CompletedTask);

	public async Task ProcessAsync(UpdateFileRequestIdeMessage request)
	{
		try
		{
			debug($"BatchUpdate #{request.CorrelationId}: received {request.Edits.Length} edit(s) for request {request.RequestId}.");

			var createdFiles = new List<string>();
			var deferredFinalizations = new List<Func<Task>>();
			var forceSaveOnDisk = request.ForceSaveOnDisk ?? true;

			foreach (var edit in request.Edits)
			{
				// `null` NewText is the delete sentinel (applied on disk by the dev-server, never
				// forwarded here); an empty string is valid content — a request to truncate a file —
				// and must flow through, otherwise the batch acks Success while silently dropping the
				// write.
				if (edit.NewText is not { } newText)
				{
					continue;
				}

				var filePath = Path.GetFullPath(edit.FilePath);
				if (!File.Exists(filePath))
				{
					createdFiles.Add(filePath);
				}

				// Do not persist open documents yet: a VS save can trigger "hot reload on save",
				// which must not evaluate the change-set before the workspace is ready (spec 052).
				// Any persisting work (a save, or the close/re-encode/reopen an encoding change needs)
				// is returned as a deferred step and run during finalization.
				if (await ApplyFileContentAsync(filePath, newText, forceSaveOnDisk) is { } deferredFinalization)
				{
					deferredFinalizations.Add(deferredFinalization);
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
			// do not depend on the trigger). Batches are serialized so triggers fire in request order.
			if (!request.IsForceHotReloadDisabled || deferredFinalizations.Count > 0)
			{
				QueueFinalization(request.IsForceHotReloadDisabled, createdFiles, deferredFinalizations);
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
	/// Chains <see cref="WaitReadinessAndTriggerHotReloadAsync"/> behind any still-running earlier
	/// batch. The dev-server is acked before finalization starts (so its BufferGate releases and the
	/// next request can be applied); serializing the finalizations guarantees a later batch's EnC
	/// trigger cannot overtake an earlier batch that is still waiting for workspace readiness.
	/// </summary>
	private void QueueFinalization(bool isForceHotReloadDisabled, List<string> createdFiles, List<Func<Task>> deferredFinalizations)
	{
		lock (_finalizationGate)
		{
			var previous = _finalizationChain;
			_finalizationChain = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
			{
				try
				{
					// Await the previous batch's JoinableTask (VSTHRD003-safe: JTF tracks it).
					// WaitReadinessAndTriggerHotReloadAsync swallows its own exceptions, so an earlier
					// batch normally completes successfully; guard regardless so one batch can never
					// break the chain for the following ones. (An unfinished earlier batch yields here,
					// releasing the gate immediately — the lock is only ever held briefly.)
					await previous;
				}
				catch
				{
				}

				await WaitReadinessAndTriggerHotReloadAsync(isForceHotReloadDisabled, createdFiles, deferredFinalizations, ct);
			});
		}
	}

	/// <summary>
	/// Applies <paramref name="fileContent"/> to <paramref name="filePath"/> — in-memory when the
	/// document is open in the IDE, on disk otherwise — and returns the work that would persist an
	/// open document (a plain save, or the close/re-encode/reopen needed when the content requires a
	/// different encoding) as a step for the caller to run *when* it wants: immediately (legacy
	/// single-file path) or deferred to finalization so a batched update only touches disk once the
	/// workspace can compile the change-set (spec 052). Returns <see langword="null"/> when there is
	/// nothing left to persist. When <paramref name="forceSaveOnDisk"/> is <see langword="false"/> the
	/// change stays in-memory and is never persisted (so there is no step to return).
	/// </summary>
	public async Task<Func<Task>?> ApplyFileContentAsync(string filePath, string fileContent, bool forceSaveOnDisk = true)
	{
		// Determine the appropriate encoding for the file
		var currentEncoding = EncodingHelpers.DetectFileEncoding(filePath);
		var targetEncoding = EncodingHelpers.GetCompatibleEncoding(currentEncoding, fileContent);

		// Check if document is already open in IDE
		var document = dte2
			.Documents
			.OfType<Document>()
			.FirstOrDefault(d => d.FullName.Equals(filePath, StringComparison.OrdinalIgnoreCase));

		// Open document whose new content needs a different encoding: it has to be rewritten on disk
		// with the target encoding (close → write → reopen). That persists the file — and a VS save
		// can trigger "hot reload on save" — so the whole operation is returned as a step the caller
		// runs when appropriate rather than run inline here.
		if (document is not null && currentEncoding != targetEncoding)
		{
			// Reflect the new content in-memory right away so neither VS nor the dev-server (which is
			// acked before finalization) ever observes stale content during the readiness wait. Only
			// the persist that changes the on-disk encoding — close/re-encode/reopen — is deferred,
			// as that is the part a "hot reload on save" could evaluate too early.
			UpdateInMemory(document, fileContent);

			if (!forceSaveOnDisk)
			{
				// Not forcing a disk save: leave the change in-memory and unsaved (VS negotiates the
				// encoding whenever it is eventually saved).
				return null;
			}

			return async () =>
			{
				debug($"Document {Path.GetFileName(filePath)} is open, closing to change encoding from {currentEncoding.EncodingName} to {targetEncoding.EncodingName} with BOM");

				document.Close(vsSaveChanges.vsSaveChangesNo);
				await Task.Delay(250); // Small delay to ensure file system is ready
				File.WriteAllText(filePath, fileContent, targetEncoding);
				await Task.Delay(250); // Small delay to ensure file system is ready
				dte2.Documents.Open(filePath);
			};
		}

		// If the file is open (with a compatible encoding), we update its content in-memory.
		if (document is not null && document.Object("TextDocument") is TextDocument)
		{
			debug($"Updating {Path.GetFileName(filePath)} (in memory).");

			UpdateInMemory(document, fileContent);

			// The save is returned as a step (a VS save can trigger "hot reload on save", which must
			// not evaluate the change-set before the workspace is ready). None to return when the
			// caller does not force a disk save — the change stays in-memory.
			if (!forceSaveOnDisk)
			{
				return null;
			}

			return () =>
			{
				document.Save();
				return Task.CompletedTask;
			};
		}

		// Not open as a text document in the IDE: write straight to disk (and reopen if it was open).
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

	/// <summary>
	/// Replaces the full in-memory content of an open document. The save (and therefore the on-disk
	/// persist) is left to the caller so a batched update never persists before workspace readiness.
	/// </summary>
	private static void UpdateInMemory(Document document, string fileContent)
	{
		// TODO: We should NOT assume the `fileContent` to contain the full document content!
		if (document.Object("TextDocument") is not TextDocument textDocument)
		{
			return;
		}

		// Keep existing markers and normalize newlines while replacing the whole document.
		// https://learn.microsoft.com/en-us/dotnet/api/envdte.vsepreplacetextoptions?view=visualstudiosdk-2022#fields
		const vsEPReplaceTextOptions flags = vsEPReplaceTextOptions.vsEPReplaceTextKeepMarkers | vsEPReplaceTextOptions.vsEPReplaceTextNormalizeNewlines;
		textDocument
			.StartPoint
			.CreateEditPoint()
			.ReplaceText(textDocument.EndPoint, fileContent, (int)flags);
	}

	private async Task WaitReadinessAndTriggerHotReloadAsync(bool isForceHotReloadDisabled, List<string> createdFiles, List<Func<Task>> deferredFinalizations, CancellationToken ct)
	{
		try
		{
			var stopwatch = Stopwatch.StartNew();

			// Created files must be integrated into the workspace before ANYTHING can evaluate the
			// change-set — our own Debug.ApplyCodeChanges below, but also a "hot reload on save" that a
			// deferred save / encoding rewrite can itself kick off. So wait for readiness whenever there
			// are created files and we will either trigger or run a deferred persist — including when the
			// explicit trigger is disabled but there are still deferred persists to flush.
			if (createdFiles.Count > 0 && (!isForceHotReloadDisabled || deferredFinalizations.Count > 0))
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

			// Deferred finalizations (saves and encoding upgrades): performed only once the workspace
			// can compile the change-set, so a save-triggered hot reload evaluates a coherent snapshot.
			// Flushed even when the hot-reload trigger is disabled — ForceSaveOnDisk semantics do not
			// depend on it.
			foreach (var finalize in deferredFinalizations)
			{
				try
				{
					await finalize();
				}
				catch (Exception e)
				{
					debug($"Deferred finalization failed: {e.Message}");
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
					debug($"BatchUpdate: workspace reports the full change-set as ready after {stopwatch.ElapsedMilliseconds} ms.");
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
