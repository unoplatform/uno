using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Uno.HotReload.Tracking;

namespace Uno.HotReload.Diffing;

internal static class ChangeSetExtensions
{
	public static async ValueTask<Solution> ApplyAsync(this Solution solution, ChangeSet changeSet, HotReloadOperation hotReload, CancellationToken ct)
	{
		// Update existing documents
		foreach (var document in changeSet.EditedDocuments)
		{
			solution = solution.WithDocumentText(document.Id, await GetSourceTextAsync(document.FilePath!, ct).ConfigureAwait(false));
		}

		// Update existing additional documents
		foreach (var additionalDocument in changeSet.EditedAdditionalDocuments)
		{
			solution = solution.WithAdditionalDocumentText(additionalDocument.Id, await GetSourceTextAsync(additionalDocument.FilePath!, ct).ConfigureAwait(false));
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

		hotReload.NotifyIgnored(changeSet.IgnoredFiles);

		return solution;
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
		return null;
	}
}
