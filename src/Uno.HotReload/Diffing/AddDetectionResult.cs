using System.Collections.Immutable;

namespace Uno.HotReload.Diffing;

/// <summary>
/// Result of an add detection operation, containing the discovered documents and any files that could not be mapped to a project.
/// </summary>
/// <param name="Documents">Source documents that were successfully mapped to a project.</param>
/// <param name="AdditionalDocuments">Additional (non-source) documents that were successfully mapped to a project.</param>
/// <param name="Ignored">Files that could not be found in any project and were ignored.</param>
public record struct AddDetectionResult(
	ImmutableArray<AddedDocumentInfo> Documents,
	ImmutableArray<AddedDocumentInfo> AdditionalDocuments,
	ImmutableHashSet<string> Ignored);
