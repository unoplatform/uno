#nullable enable

using System;
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
	/// <summary>
	/// Walks from <paramref name="memberOwner"/> upward until it finds an <c>x:DataType</c>
	/// attribute or exits the enclosing <c>DataTemplate</c>. Returns the resolved type
	/// (via <paramref name="typeResolver"/>) or <c>null</c>.
	/// </summary>
	/// <param name="memberOwner">The <see cref="XamlObjectDefinition"/> that owns the member carrying the expression.</param>
	/// <param name="typeResolver">Callback that resolves a raw XAML type string (e.g. <c>local:Customer</c>) to an <see cref="INamedTypeSymbol"/>. Typically wraps <c>RewriteNamespaces</c> + <c>GetType</c>.</param>
	public static INamedTypeSymbol? Resolve(
		XamlObjectDefinition memberOwner,
		Func<string, INamedTypeSymbol?> typeResolver)
	{
		if (memberOwner is null)
		{
			return null;
		}

		for (var current = memberOwner; current is not null; current = current.Owner)
		{
			foreach (var member in current.Members)
			{
				if (member.Member.Name == "DataType"
					&& member.Member.PreferredXamlNamespace == XamlConstants.XamlXmlNamespace
					&& member.Value is string value
					&& !string.IsNullOrWhiteSpace(value))
				{
					return typeResolver(value);
				}
			}

			// DataTemplate shadows ancestors: if the current element is a DataTemplate
			// and did not declare x:DataType above, stop here (the outer scope's
			// x:DataType does not apply inside a DataTemplate).
			if (current.Type?.Name == "DataTemplate")
			{
				return null;
			}
		}

		return null;
	}
}
