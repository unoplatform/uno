using Microsoft.CodeAnalysis;

namespace Uno.HotReload.Diffing;

internal record struct AddedDocumentInfo(ProjectInfo Project, DocumentInfo Document);
