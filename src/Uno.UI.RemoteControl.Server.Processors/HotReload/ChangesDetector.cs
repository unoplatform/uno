using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Uno.UI.RemoteControl.Helpers;
using Uno.UI.RemoteControl.Host.HotReload.MetadataUpdates;

namespace Uno.UI.RemoteControl.Host.HotReload;

internal record ChangeSet(
	ImmutableArray<Document> EditedDocuments,
	ImmutableArray<TextDocument> EditedAdditionalDocuments,
	ImmutableArray<AddedDocumentInfo> AddedDocuments,
	ImmutableArray<AddedDocumentInfo> AddedAdditionalDocuments,
	ImmutableArray<DocumentId> RemovedDocuments,
	ImmutableArray<DocumentId> RemovedAdditionalDocuments,
	ImmutableHashSet<string> IgnoredFiles
)
{
	public bool HasAddOrRemove => !AddedDocuments.IsEmpty || !AddedAdditionalDocuments.IsEmpty || !RemovedDocuments.IsEmpty || !RemovedAdditionalDocuments.IsEmpty;

	public static ChangeSet IgnoreAll(ImmutableHashSet<string> ignoredFiles)
		=> new([], [], [], [], [], [], ignoredFiles);
}

internal record struct AddedDocumentInfo(ProjectInfo Project, DocumentInfo Document);

internal class ChangesDetector(Func<CancellationToken, ValueTask<Workspace>> CreateWorkspace, IReporter reporter)
{
	public async ValueTask<ChangeSet> DiscoverChangesAsync(Solution solution, ImmutableHashSet<string> files, CancellationToken ct)
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
					reporter.Verbose($"Could not find document with path '{file}' in the workspace and file does not exist on disk.");
					notFound.Add(file);
				}
			}
		}

		var added = await DiscoverNewFilesAsync(ImmutableHashSet.CreateRange(potentiallyAdded), ct);

		return new(
			[.. editedDocuments],
			[.. editedAdditionalDocuments],
			added.documents,
			added.additionalDocuments,
			[.. removedDocuments],
			[.. removedAdditionalDocuments],
			[.. notFound, .. added.ignored]);
	}

	private async ValueTask<(ImmutableArray<AddedDocumentInfo> documents, ImmutableArray<AddedDocumentInfo> additionalDocuments, ImmutableHashSet<string> ignored)> DiscoverNewFilesAsync(
		ImmutableHashSet<string> newFiles,
		CancellationToken ct)
	{
		if (newFiles is not { IsEmpty: false })
		{
			return ([], [], newFiles);
		}

		Workspace? tempWorkspace = null;
		try
		{
			reporter.Output($"Detected {newFiles.Count} potentially new file(s). Creating temporary workspace to discover them...");

			// Create a temporary workspace to discover the new files
			tempWorkspace = await CreateWorkspace(ct);

			var discoveredDocuments = ImmutableArray.CreateBuilder<AddedDocumentInfo>();
			var discoveredAdditionalDocuments = ImmutableArray.CreateBuilder<AddedDocumentInfo>();
			var ignoredFiles = ImmutableHashSet.CreateBuilder<string>();

			foreach (var file in newFiles)
			{
				// Search for the file in the temp workspace's projects
				// Note: Here again we assume that document can appear in more than one project (same project loaded with different TFM)
				var found = false;
				foreach (var project in tempWorkspace.CurrentSolution.Projects)
				{
					if (project.Documents.FirstOrDefault(d => string.Equals(d.FilePath, file, PathComparer.Comparison)) is { } document)
					{
						found = true;
						discoveredDocuments.Add(new(project.GetInfo(), document.GetInfo()));
					}
					else if (project.AdditionalDocuments.FirstOrDefault(d => string.Equals(d.FilePath, file, PathComparer.Comparison)) is { } additionalDocument)
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
			reporter.Warn($"Error while discovering new files: {ex.Message}");
			return ([], [], newFiles);
		}
		finally
		{
			tempWorkspace?.Dispose();
		}
	}
}
