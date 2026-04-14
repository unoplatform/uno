#nullable enable

using Microsoft.CodeAnalysis;

namespace Uno.UI.SourceGenerators.XamlGenerator.CSharpExpressions;

/// <summary>
/// Outcome of <see cref="ResolutionScope.Resolve"/>. See data-model.md §5.
/// </summary>
internal sealed record ResolutionResult(
	MemberLocation Location,
	ISymbol? Symbol,
	DiagnosticDescriptor? Diagnostic);

internal enum MemberLocation
{
	This,
	DataType,
	Both,
	Neither,
	ForcedThis,
	ForcedDataType,
	StaticType,
	MarkupExtension,
}
