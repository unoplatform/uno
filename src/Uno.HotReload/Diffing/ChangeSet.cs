using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Uno.HotReload.Diffing;

/// <summary>
/// Describes the set of changes detected in a batch of modified files, including edits, adds, removes, and ignored files.
/// </summary>
public record ChangeSet(
	ImmutableArray<Document> EditedDocuments,
	ImmutableArray<TextDocument> EditedAdditionalDocuments,
	ImmutableArray<AddedDocumentInfo> AddedDocuments,
	ImmutableArray<AddedDocumentInfo> AddedAdditionalDocuments,
	ImmutableArray<DocumentId> RemovedDocuments,
	ImmutableArray<DocumentId> RemovedAdditionalDocuments,
	ImmutableArray<Project> EditedProjects,
	ImmutableArray<Project> AddedProjects,
	ImmutableArray<ProjectId> RemovedProjects,
	ImmutableHashSet<string> IgnoredFiles
)
{
	public bool HasAddOrRemove => !AddedDocuments.IsEmpty || !AddedAdditionalDocuments.IsEmpty || !RemovedDocuments.IsEmpty || !RemovedAdditionalDocuments.IsEmpty;

	public bool HasProjectEdits => !EditedProjects.IsEmpty || !AddedProjects.IsEmpty || !RemovedProjects.IsEmpty;

	public static ChangeSet IgnoreAll(ImmutableHashSet<string> ignoredFiles)
		=> new([], [], [], [], [], [], [], [], [], ignoredFiles);
}
