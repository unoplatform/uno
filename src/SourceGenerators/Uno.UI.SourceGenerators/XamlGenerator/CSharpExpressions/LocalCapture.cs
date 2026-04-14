#nullable enable

using Microsoft.CodeAnalysis;

namespace Uno.UI.SourceGenerators.XamlGenerator.CSharpExpressions;

/// <summary>
/// A page-level identifier captured once at load time. See data-model.md §8.
/// </summary>
internal sealed record LocalCapture(
	string OriginalIdentifier,
	string CaptureVariableName,
	string CaptureInitializer,
	ITypeSymbol Type);
