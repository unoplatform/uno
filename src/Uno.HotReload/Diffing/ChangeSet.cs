using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Uno.HotReload.Diffing;

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
