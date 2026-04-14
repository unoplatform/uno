#nullable enable

using Microsoft.CodeAnalysis;

namespace Uno.UI.SourceGenerators.XamlGenerator.CSharpExpressions;

/// <summary>
/// Walks the parent <c>XamlObjectDefinition</c> chain from the current member upwards
/// to find the in-scope <c>x:DataType</c>. Respects <c>DataTemplate</c> boundaries.
/// Emits <c>UNO2010</c> once per XAML file when no <c>x:DataType</c> is found but the
/// file contains at least one C# expression.
/// See <c>contracts/resolution-algorithm.md</c> §DataType-discovery.
/// </summary>
internal static class XDataTypeResolver
{
	// TODO (T033): implement. Contract:
	//   - Walk from the member's owning XamlObjectDefinition upwards via parent pointers.
	//   - Stop at the nearest DataTemplate root (x:DataType on a DataTemplate is local to that template).
	//   - Return the resolved INamedTypeSymbol or null.
	//   - Caller emits UNO2010 (one per file) when resolution fails for any classified expression.
	public static INamedTypeSymbol? Resolve(
		XamlObjectDefinition memberOwner,
		Compilation compilation)
	{
		_ = memberOwner;
		_ = compilation;
		return null;
	}
}
