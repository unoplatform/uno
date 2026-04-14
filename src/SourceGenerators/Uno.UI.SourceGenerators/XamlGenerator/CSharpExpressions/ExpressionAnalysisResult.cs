#nullable enable

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Uno.UI.SourceGenerators.XamlGenerator.CSharpExpressions;

/// <summary>
/// Bridge between parse/resolve and codegen. See data-model.md §6.
/// </summary>
internal sealed record ExpressionAnalysisResult(
	string TransformedCSharp,
	IReadOnlyList<BindingHandler> Handlers,
	IReadOnlyList<LocalCapture> Captures,
	ITypeSymbol? LeafPropertyType,
	bool IsOneShot,
	bool IsSettable);
