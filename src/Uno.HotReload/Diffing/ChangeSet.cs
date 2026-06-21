using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Uno.HotReload.Diffing;

/// <summary>
/// Describes the set of changes detected in a batch of modified files, including edits, adds, removes, and ignored files.
/// </summary>
/// <remarks>
/// Removed-* members carry the file path alongside the id so consumers can
/// report them (e.g. via <see cref="HotReload.Tracking.HotReloadOperation.NotifyIgnored(System.Collections.Generic.IEnumerable{string})"/>)
/// without needing the originating <see cref="Solution"/> snapshot to look the
/// path up.
/// </remarks>
public record ChangeSet(
	ImmutableArray<Document> EditedDocuments,
	ImmutableArray<TextDocument> EditedAdditionalDocuments,
	ImmutableArray<AddedDocumentInfo> AddedDocuments,
	ImmutableArray<AddedDocumentInfo> AddedAdditionalDocuments,
	ImmutableArray<(string FilePath, DocumentId Id)> RemovedDocuments,
	ImmutableArray<(string FilePath, DocumentId Id)> RemovedAdditionalDocuments,
	ImmutableArray<Project> EditedProjects,
	ImmutableArray<Project> AddedProjects,
	ImmutableArray<(string FilePath, ProjectId Id)> RemovedProjects,
	ImmutableHashSet<string> IgnoredFiles
)
{
	public bool HasAddOrRemove => !AddedDocuments.IsEmpty || !AddedAdditionalDocuments.IsEmpty || !RemovedDocuments.IsEmpty || !RemovedAdditionalDocuments.IsEmpty;

	public bool HasProjectEdits => !EditedProjects.IsEmpty || !AddedProjects.IsEmpty || !RemovedProjects.IsEmpty;

	public static ChangeSet IgnoreAll(ImmutableHashSet<string> ignoredFiles)
		=> new([], [], [], [], [], [], [], [], [], ignoredFiles);

	/// <summary>
	/// Enumerates the file path of every entry across <em>all</em> ChangeSet
	/// members. Documents / projects with a <see langword="null"/> or empty
	/// <c>FilePath</c> are skipped — they cannot be reported back to the user.
	/// </summary>
	/// <remarks>
	/// Single source of truth for "what files does this change set touch", used
	/// by the hot-reload manager to translate an ignored set into per-path
	/// notifications via <see cref="HotReload.Tracking.HotReloadOperation.NotifyIgnored(System.Collections.Generic.IEnumerable{string})"/>
	/// — without needing the originating <see cref="Solution"/> snapshot.
	/// Adding a new ChangeSet member without extending this enumeration would
	/// silently drop those entries from downstream reporting — keep it in sync.
	/// </remarks>
	public IEnumerable<string> GetAllPaths()
	{
		foreach (var path in IgnoredFiles)
		{
			if (!string.IsNullOrEmpty(path))
			{
				yield return path;
			}
		}

		foreach (var edited in EditedDocuments)
		{
			if (edited.FilePath is { Length: > 0 } path)
			{
				yield return path;
			}
		}

		foreach (var edited in EditedAdditionalDocuments)
		{
			if (edited.FilePath is { Length: > 0 } path)
			{
				yield return path;
			}
		}

		foreach (var added in AddedDocuments)
		{
			if (added.Document.FilePath is { Length: > 0 } path)
			{
				yield return path;
			}
		}

		foreach (var added in AddedAdditionalDocuments)
		{
			if (added.Document.FilePath is { Length: > 0 } path)
			{
				yield return path;
			}
		}

		foreach (var (path, _) in RemovedDocuments)
		{
			if (!string.IsNullOrEmpty(path))
			{
				yield return path;
			}
		}

		foreach (var (path, _) in RemovedAdditionalDocuments)
		{
			if (!string.IsNullOrEmpty(path))
			{
				yield return path;
			}
		}

		foreach (var project in EditedProjects)
		{
			if (project.FilePath is { Length: > 0 } path)
			{
				yield return path;
			}
		}

		foreach (var project in AddedProjects)
		{
			if (project.FilePath is { Length: > 0 } path)
			{
				yield return path;
			}
		}

		foreach (var (path, _) in RemovedProjects)
		{
			if (!string.IsNullOrEmpty(path))
			{
				yield return path;
			}
		}
	}
}
