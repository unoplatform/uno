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
			if (createdFiles.Count > 0)
			{
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
			dte.ExecuteCommand("Debug.ApplyCodeChanges");
		}
		catch (Exception e)
		{
			debug($"Batched hot-reload trigger failed: {e.Message}");
		}
	}

	/// <summary>
	/// Waits until the VS Roslyn workspace can compile the change-set containing
	/// <paramref name="createdFiles"/>: the files must be integrated by the project system
	/// (Document / AdditionalDocument) and the source generators of every touched project must
	/// have produced their outputs (forced through <c>GetCompilationAsync</c> — the materialized
	/// outputs are cached on the snapshot the subsequent EnC evaluation reuses).
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

		// 1) Wait for the created files to be integrated by the project system.
		while (stopwatch.Elapsed < timeout)
		{
			if (createdFiles.All(file => IsKnownToSolution(workspace.CurrentSolution, file)))
			{
				break;
			}

			await Task.Delay(100, ct);
		}

		// 2) Force the source generators to run on every touched project (multi-TFM projects
		//    yield several Roslyn projects — all of them are processed).
		var solution = workspace.CurrentSolution;
		foreach (var projectId in createdFiles.SelectMany(file => GetProjectsContaining(solution, file)).Distinct().ToList())
		{
			if (stopwatch.Elapsed >= timeout)
			{
				break;
			}

			if (solution.GetProject(projectId) is { } project)
			{
				using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
				cts.CancelAfter(timeout - stopwatch.Elapsed);
				try
				{
					await project.GetCompilationAsync(cts.Token);
				}
				catch (OperationCanceledException) when (!ct.IsCancellationRequested)
				{
					break;
				}
			}
		}

		if (stopwatch.Elapsed >= timeout)
		{
			debug($"Workspace readiness timed out after {timeout}; triggering hot reload anyway.");
		}
	}

	private static bool IsKnownToSolution(RoslynSolution solution, string filePath)
		=> solution.GetDocumentIdsWithFilePath(filePath).Any()
			|| solution.Projects.Any(project => project.AdditionalDocuments.Any(additional => string.Equals(additional.FilePath, filePath, StringComparison.OrdinalIgnoreCase)));

	private static IEnumerable<RoslynProjectId> GetProjectsContaining(RoslynSolution solution, string filePath)
		=> solution
			.GetDocumentIdsWithFilePath(filePath)
			.Select(documentId => documentId.ProjectId)
			.Concat(solution
				.Projects
				.Where(project => project.AdditionalDocuments.Any(additional => string.Equals(additional.FilePath, filePath, StringComparison.OrdinalIgnoreCase)))
				.Select(project => project.Id));
}
