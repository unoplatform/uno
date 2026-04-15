#nullable enable

using Microsoft.CodeAnalysis;

namespace Uno.UI.SourceGenerators.XamlGenerator.CSharpExpressions;

/// <summary>
/// Resolves bare identifiers and member-access roots against the <see cref="ResolutionScope"/>.
/// Implements the decision table in <c>contracts/resolution-algorithm.md</c> §Resolution.
/// </summary>
/// <remarks>
/// Decision table summary:
/// <list type="bullet">
/// <item>Identifier found on <c>PageType</c> only → <c>This</c></item>
/// <item>Identifier found on <c>DataType</c> only → <c>DataType</c></item>
/// <item>Identifier found on both → <c>Both</c> + <c>UNO2002</c></item>
/// <item>Identifier found nowhere + matches a registered markup extension → <c>MarkupExtension</c> + <c>UNO2001</c></item>
/// <item>Identifier found nowhere + matches a static type in <c>GlobalUsings</c> → <c>StaticType</c></item>
/// <item>Identifier collides with a member AND a static type → <c>UNO2004</c></item>
/// <item>Identifier resolves nowhere → <c>Neither</c> + <c>UNO2003</c></item>
/// </list>
/// </remarks>
internal static class MemberResolver
{
	// TODO (T066): extend with ForcedThis, ForcedDataType branches (US3 — UNO2001 nuances).
	// TODO (T076): extend with static-type lookup via ResolutionScope.GlobalUsings (US4 — UNO2004).
	public static ResolutionResult Resolve(string identifier, ResolutionScope scope)
	{
		var pageSymbol = FindInstanceMember(scope.PageType, identifier);
		var dataSymbol = scope.DataType is { } dataType
			? FindInstanceMember(dataType, identifier)
			: null;

		if (pageSymbol is not null && dataSymbol is not null)
		{
			return new ResolutionResult(MemberLocation.Both, pageSymbol, Diagnostics.AmbiguousMemberExpression);
		}

		if (pageSymbol is not null)
		{
			return new ResolutionResult(MemberLocation.This, pageSymbol, Diagnostic: null);
		}

		if (dataSymbol is not null)
		{
			return new ResolutionResult(MemberLocation.DataType, dataSymbol, Diagnostic: null);
		}

		if (scope.KnownMarkupExtensions.TryGetValue(identifier, out var markupExtension))
		{
			return new ResolutionResult(MemberLocation.MarkupExtension, markupExtension, Diagnostic: null);
		}

		return new ResolutionResult(MemberLocation.Neither, Symbol: null, Diagnostics.MemberNotFound);
	}

	private static ISymbol? FindInstanceMember(INamedTypeSymbol type, string identifier)
	{
		for (var current = (INamedTypeSymbol?)type; current is not null; current = current.BaseType)
		{
			foreach (var member in current.GetMembers(identifier))
			{
				if (member.IsStatic)
				{
					continue;
				}

				switch (member.Kind)
				{
					case SymbolKind.Property:
					case SymbolKind.Field:
					case SymbolKind.Method:
					case SymbolKind.Event:
						return member;
				}
			}
		}

		return null;
	}
}
