using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Uno.HotReload.Utils;

namespace Uno.HotReload.Diffing;

/// <summary>
/// Default <see cref="ISolutionUpdater"/> implementation. Mutates the solution
/// document set from the <see cref="ChangeSet"/> (edits, adds, removes plus an
/// analyzer-config refresh on add/remove). Reports project-level edits and
/// any unprocessed inputs via <see cref="SolutionUpdateResult.IgnoredChanges"/>;
/// the caller runs the standard hot-reload cycle on top of the resulting
/// solution.
/// </summary>
public sealed class SolutionUpdater : ISolutionUpdater
{
	/// <inheritdoc />
	public async ValueTask<SolutionUpdateResult> UpdateAsync(
		Solution solution,
		ChangeSet changeSet,
		CancellationToken ct)
	{
		// Update existing documents and additional documents, skipping texts that are already
		// up to date: a file-system event can re-observe a file whose content is already in the
		// solution (e.g. an event joining a batch right after its content was applied), and
		// re-applying byte-identical text would fork the snapshot — defeating the caller's
		// reference-equality NoChanges short-circuit. Only realized texts are compared: an
		// unrealized text means the document was never read into this snapshot, so the batch
		// cannot be a re-observation of it. Skipped entries are surfaced through
		// SolutionUpdateResult.UpToDateChanges.
		var upToDateDocuments = ImmutableArray.CreateBuilder<Document>();
		foreach (var document in changeSet.EditedDocuments)
		{
			var text = await GetSourceTextAsync(document.FilePath!, ct).ConfigureAwait(false);
			if (document.TryGetText(out var current) && current.ContentEquals(text))
			{
				upToDateDocuments.Add(document);
			}
			else
			{
				solution = solution.WithDocumentText(document.Id, text);
			}
		}

		var upToDateAdditionalDocuments = ImmutableArray.CreateBuilder<TextDocument>();
		foreach (var additionalDocument in changeSet.EditedAdditionalDocuments)
		{
			var text = await GetSourceTextAsync(additionalDocument.FilePath!, ct).ConfigureAwait(false);
			if (additionalDocument.TryGetText(out var current) && current.ContentEquals(text))
			{
				upToDateAdditionalDocuments.Add(additionalDocument);
			}
			else
			{
				solution = solution.WithAdditionalDocumentText(additionalDocument.Id, text);
			}
		}

		// Added documents has been detected using a temporary solution.
		// We need to make sure to find the right project instance in the current solution, and update the document ID accordingly.
		// Note: A project may appear multiple times in the solution (e.g. different TFM), so we need to add the document to **all** instances.
		var ignoredAdds = ImmutableArray.CreateBuilder<AddedDocumentInfo>();
		foreach (var added in changeSet.AddedDocuments)
		{
			var found = false;
			var projects = solution.Projects.Where(p => PathComparer.PathEquals(p.FilePath, added.Project.FilePath));
			foreach (var project in projects)
			{
				found = true;
				// Defensive: avoid creating a duplicate Document when an existing one denotes the same physical
				// file under a different separator/casing spelling (e.g. '\' vs '/'). Roslyn's Workspace does not
				// deduplicate by normalized path, which previously caused source generators to see the same file
				// twice and emit conflicting output.
				if (added.Document.FilePath is { } addedPath
					&& project.Documents.Any(d => PathComparer.PathEquals(d.FilePath, addedPath)))
				{
					continue;
				}
				solution = solution.AddDocument(added.Document.WithId(DocumentId.CreateNewId(project.Id)));
			}
			if (!found)
			{
				ignoredAdds.Add(added);
			}
		}

		var ignoredAdditionalAdds = ImmutableArray.CreateBuilder<AddedDocumentInfo>();
		foreach (var added in changeSet.AddedAdditionalDocuments)
		{
			var found = false;
			var projects = solution.Projects.Where(p => PathComparer.PathEquals(p.FilePath, added.Project.FilePath));
			foreach (var project in projects)
			{
				found = true;
				// Defensive: avoid creating a duplicate AdditionalDocument when an existing one denotes the same
				// physical file under a different separator/casing spelling. See comment above.
				if (added.Document.FilePath is { } addedPath
					&& project.AdditionalDocuments.Any(d => PathComparer.PathEquals(d.FilePath, addedPath)))
				{
					continue;
				}
				solution = solution.AddAdditionalDocument(added.Document.WithId(DocumentId.CreateNewId(project.Id)));
			}
			if (!found)
			{
				ignoredAdditionalAdds.Add(added);
			}
		}

		solution = solution
			.RemoveDocuments([.. changeSet.RemovedDocuments.Select(r => r.Id)])
			.RemoveAdditionalDocuments([.. changeSet.RemovedAdditionalDocuments.Select(r => r.Id)]);

		// If a document has been added, we make sure to refresh the configuration of the analyzers.
		// This is especially required for new XAML files to have the 'build_metadata.AdditionalFiles.SourceItemGroup = Page' updated
		// from the file ./obj/Debug/{tfm}/{projectName}.GeneratedMSBuildEditorConfig.editorconfig
		if (changeSet.HasAddOrRemove)
		{
			var analyzersConfigs = solution
				.Projects
				.SelectMany(p => p.AnalyzerConfigDocuments)
				.Where(config => config.FilePath is not null);
			foreach (var analyzerConfig in analyzersConfigs)
			{
				solution = solution.WithAnalyzerConfigDocumentText(analyzerConfig.Id, await GetSourceTextAsync(analyzerConfig.FilePath!, ct).ConfigureAwait(false));
			}
		}

		// `with` semantics: zero out only the fields this updater consumed. Anything
		// else (project-level edits, the upstream-detected IgnoredFiles, and any new
		// ChangeSet member added in the future) flows through to the caller as
		// ignored automatically. Up-to-date files were consumed (their content is in
		// the solution) — they are reported through UpToDateChanges, not as ignored.
		var ignored = changeSet with
		{
			EditedDocuments = [],
			EditedAdditionalDocuments = [],
			AddedDocuments = ignoredAdds.ToImmutable(),
			AddedAdditionalDocuments = ignoredAdditionalAdds.ToImmutable(),
			RemovedDocuments = [],
			RemovedAdditionalDocuments = [],
		};

		// Keep the common (nothing-skipped) path allocation-free: only build a ChangeSet when at
		// least one entry was actually up to date, otherwise reuse the shared empty instance.
		var upToDate = upToDateDocuments.Count is 0 && upToDateAdditionalDocuments.Count is 0
			? ChangeSet.Empty
			: ChangeSet.Empty with
			{
				EditedDocuments = upToDateDocuments.ToImmutable(),
				EditedAdditionalDocuments = upToDateAdditionalDocuments.ToImmutable(),
			};

		return new SolutionUpdateResult(solution, ignored) { UpToDateChanges = upToDate };
	}

	private static async ValueTask<SourceText> GetSourceTextAsync(string filePath, CancellationToken ct)
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
				await Task.Delay(20 * (attemptIndex + 1), ct).ConfigureAwait(false);
			}
		}

		Debug.Fail("This shouldn't happen.");
		return null!;
	}
}
