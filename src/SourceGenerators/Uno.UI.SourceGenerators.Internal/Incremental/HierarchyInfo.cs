#nullable enable

using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Uno.UI.SourceGenerators.Internal.Incremental;

/// <summary>
/// One type in a <see cref="HierarchyInfo"/>, as needed to reopen it with a partial declaration.
/// </summary>
/// <param name="Keyword">The declaration keyword(s), e.g. <c>class</c> or <c>record struct</c>.</param>
/// <param name="Name">The escaped name, including its type parameter list.</param>
internal sealed record TypeDeclarationInfo(string Keyword, string Name);

/// <summary>
/// A named type and everything containing it, so generated code can reopen it as a partial declaration.
/// </summary>
/// <param name="MetadataName">The fully qualified metadata name, e.g. <c>N.Outer+Inner`1</c>.</param>
/// <param name="FullyQualifiedName">The fully qualified C# name, e.g. <c>global::N.Outer.Inner&lt;T&gt;</c>.</param>
/// <param name="Namespace">The containing namespace, or an empty string for the global namespace.</param>
/// <param name="Types">The type and its containing types, outermost first.</param>
internal sealed record HierarchyInfo(string MetadataName, string FullyQualifiedName, string Namespace, EquatableArray<TypeDeclarationInfo> Types)
{
	private static readonly SymbolDisplayFormat s_namespaceFormat = SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted);

	public static HierarchyInfo From(INamedTypeSymbol type)
	{
		var types = ImmutableArray.CreateBuilder<TypeDeclarationInfo>();
		for (var current = type; current is not null; current = current.ContainingType)
		{
			types.Insert(0, new TypeDeclarationInfo(GetKeyword(current), GetName(current)));
		}

		// Built from metadata names, which are never escaped: '@' isn't valid in a hint name.
		var metadataName = new StringBuilder(type.MetadataName);
		for (var current = type.ContainingType; current is not null; current = current.ContainingType)
		{
			metadataName.Insert(0, '+').Insert(0, current.MetadataName);
		}

		for (var current = type.ContainingNamespace; current is { IsGlobalNamespace: false }; current = current.ContainingNamespace)
		{
			metadataName.Insert(0, '.').Insert(0, current.MetadataName);
		}

		var @namespace = type.ContainingNamespace is { IsGlobalNamespace: false } ns ? ns.ToDisplayString(s_namespaceFormat) : "";

		return new HierarchyInfo(
			metadataName.ToString(),
			type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
			@namespace,
			new EquatableArray<TypeDeclarationInfo>(types.ToImmutable()));
	}

	private static string GetKeyword(INamedTypeSymbol type)
		=> type switch
		{
			{ IsRecord: true, TypeKind: TypeKind.Struct } => "record struct",
			{ IsRecord: true } => "record",
			{ TypeKind: TypeKind.Struct } => "struct",
			{ TypeKind: TypeKind.Interface } => "interface",
			_ => "class",
		};

	private static string GetName(INamedTypeSymbol type)
	{
		var name = EscapeIdentifier(type.Name);
		if (type.TypeParameters.Length == 0)
		{
			return name;
		}

		var builder = new StringBuilder(name).Append('<');
		for (var i = 0; i < type.TypeParameters.Length; i++)
		{
			if (i > 0)
			{
				builder.Append(", ");
			}

			builder.Append(EscapeIdentifier(type.TypeParameters[i].Name));
		}

		return builder.Append('>').ToString();
	}

	public static string EscapeIdentifier(string identifier)
		=> SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None ? "@" + identifier : identifier;
}
