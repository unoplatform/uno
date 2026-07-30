using Microsoft.CodeAnalysis;

namespace Uno.HotReload.Diffing;

/// <summary>
/// Associates an added document with the project it belongs to.
/// </summary>
/// <param name="Project">The project containing the added document.</param>
/// <param name="Document">The document that was added.</param>
public record struct AddedDocumentInfo(ProjectInfo Project, DocumentInfo Document);
