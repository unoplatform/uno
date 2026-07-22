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
using RoslynCompilation = Microsoft.CodeAnalysis.Compilation;
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

			debug($"BatchUpdate #{request.CorrelationId}: edits applied ({createdFiles.Count} created file(s), {deferredSaves.Count} deferred save(s)); acknowledging{(request.IsForceHotReloadDisabled ? "" : " and scheduling the readiness-gated hot reload")}.");

			// Ack once the writes are applied: the readiness wait and the hot-reload trigger run
			// asynchronously, and their outcome flows through the hot-reload operation channel.
			if (ideChannelClient() is { } channel)
			{
				await channel.SendToDevServerAsync(new IdeResultMessage(request.CorrelationId, Result.Success()), ct);
			}

			if (!request.IsForceHotReloadDisabled)
			{
				_ = Task.Run(() => WaitReadinessAndTriggerHotReloadAsync(createdFiles, deferredSaves, ct), ct);
			}
		}
		catch (Exception e) when (ideChannelClient() is { } channel)
		{
			// Send a message back to indicate that the request has failed.
			await channel.SendToDevServerAsync(new IdeResultMessage(request.CorrelationId, Result.Fail(e)), ct);

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

	private async Task WaitReadinessAndTriggerHotReloadAsync(List<string> createdFiles, List<Document> deferredSaves, CancellationToken ct)
	{
		try
		{
			var stopwatch = Stopwatch.StartNew();
			if (createdFiles.Count > 0)
			{
				debug($"BatchUpdate: waiting for workspace readiness ({createdFiles.Count} created file(s))...");
				await WaitForWorkspaceReadinessAsync(createdFiles, TimeSpan.FromSeconds(10), ct);
			}

			// Deferred saves: performed only once the workspace can compile the change-set, so a
			// save-triggered hot reload evaluates a coherent snapshot.
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

			// Programmatically trigger the "Apply Code Changes" command in Visual Studio,
			// which will trigger the hot reload (same mechanics as ForceHotReloadIdeMessage).
			debug($"BatchUpdate: triggering Debug.ApplyCodeChanges (readiness + deferred saves took {stopwatch.ElapsedMilliseconds} ms).");
			dte.ExecuteCommand("Debug.ApplyCodeChanges");
		}
		catch (Exception e)
		{
			debug($"Batched hot-reload trigger failed: {e.Message}");
		}
	}

	/// <summary>
	/// Waits until the VS Roslyn workspace can actually compile the change-set containing
	/// <paramref name="createdFiles"/>. Being known to the project system is NOT enough: a
	/// created .xaml can be listed as AdditionalDocument before its item metadata is complete,
	/// in which case the XAML generator silently skips it and EnC evaluates a compilation
	/// without the generated partial (CS1061 on InitializeComponent). Readiness therefore
	/// requires the EFFECT of every created file in a fresh compilation of every project
	/// containing it (multi-TFM ⇒ all heads): a source file must contribute its syntax tree,
	/// a .xaml must have produced its per-file generated output.
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
		while (stopwatch.Elapsed < timeout)
		{
			ct.ThrowIfCancellationRequested();

			if (await AreCreatedFilesEffectiveAsync(workspace.CurrentSolution, createdFiles, timeout - stopwatch.Elapsed, ct))
			{
				debug($"BatchUpdate: workspace compiles the full change-set after {stopwatch.ElapsedMilliseconds} ms.");
				return;
			}

			await Task.Delay(100, ct);
		}

		debug($"Workspace readiness timed out after {timeout}; triggering hot reload anyway.");
	}

	private static async Task<bool> AreCreatedFilesEffectiveAsync(RoslynSolution solution, List<string> createdFiles, TimeSpan budget, CancellationToken ct)
	{
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
		cts.CancelAfter(budget);

		try
		{
			var compilations = new Dictionary<RoslynProjectId, RoslynCompilation?>();
			foreach (var file in createdFiles)
			{
				var projectIds = GetProjectsContaining(solution, file).Distinct().ToList();
				if (projectIds.Count == 0)
				{
					// Not integrated by the project system yet.
					return false;
				}

				foreach (var projectId in projectIds)
				{
					if (!compilations.TryGetValue(projectId, out var compilation))
					{
						compilation = solution.GetProject(projectId) is { } project
							? await project.GetCompilationAsync(cts.Token)
							: null;
						compilations[projectId] = compilation;
					}

					if (compilation is null || !IsFileEffective(compilation, file))
					{
						return false;
					}
				}
			}

			return true;
		}
		catch (OperationCanceledException) when (!ct.IsCancellationRequested)
		{
			// Budget expired mid-compilation — the outer loop reports the timeout.
			return false;
		}
	}

	private static bool IsFileEffective(RoslynCompilation compilation, string filePath)
	{
		if (filePath.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
		{
			// The per-file generated output (hint name "<Page>_<hash>.cs", or "<Page>.g.*") is
			// the signal that the XAML generator actually processed the file. Note: the
			// code-behind is named "<Page>.xaml.cs" and must NOT satisfy this check.
			var expectedPrefix = Path.GetFileNameWithoutExtension(filePath);
			return compilation.SyntaxTrees.Any(tree =>
			{
				var name = Path.GetFileName(tree.FilePath);
				return name.StartsWith(expectedPrefix + "_", StringComparison.OrdinalIgnoreCase)
					|| name.StartsWith(expectedPrefix + ".g.", StringComparison.OrdinalIgnoreCase);
			});
		}

		if (filePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
		{
			return compilation.SyntaxTrees.Any(tree => string.Equals(tree.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
		}

		// Other file kinds (assets…) have no compilation effect to observe — being part of a
		// project (checked above) is the best signal available.
		return true;
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
