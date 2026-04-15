#nullable enable

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Uno.UI.SourceGenerators.XamlGenerator.CSharpExpressions;

/// <summary>
/// Ordered symbol sources the resolver walks for a single XAML file. See data-model.md §4.
/// </summary>
internal sealed class ResolutionScope
{
	public ResolutionScope(
		INamedTypeSymbol pageType,
		INamedTypeSymbol? dataType,
		IReadOnlyDictionary<string, INamedTypeSymbol> knownMarkupExtensions,
		IReadOnlyList<string> globalUsings,
		Compilation compilation)
	{
		PageType = pageType;
		DataType = dataType;
		KnownMarkupExtensions = knownMarkupExtensions;
		GlobalUsings = globalUsings;
		Compilation = compilation;
	}

	public INamedTypeSymbol PageType { get; }
	public INamedTypeSymbol? DataType { get; }
	public IReadOnlyDictionary<string, INamedTypeSymbol> KnownMarkupExtensions { get; }
	public IReadOnlyList<string> GlobalUsings { get; }
	public Compilation Compilation { get; }

	/// <summary>
	/// Decision entry point for a bare identifier. See <c>contracts/resolution-algorithm.md</c>.
	/// </summary>
	public ResolutionResult Resolve(string identifier)
		=> MemberResolver.Resolve(identifier, this);
}
