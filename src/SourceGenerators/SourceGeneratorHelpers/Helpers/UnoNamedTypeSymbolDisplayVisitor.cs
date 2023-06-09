#nullable enable

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Uno.Roslyn;

/// <summary>
/// This visitor is intended to be used to visit INamedTypeSymbol.
/// Don't use it for other symbols without revising the implementation and updating accordingly.
/// </summary>
/// <remarks>
/// We implemented our own visitor to overcome https://github.com/dotnet/roslyn/issues/67067
/// </remarks>
public sealed class UnoNamedTypeSymbolDisplayVisitor : SymbolVisitor
{
	private readonly StringBuilder _builder;
	private readonly bool _includeGlobalNamespace;

	public UnoNamedTypeSymbolDisplayVisitor(StringBuilder builder, bool includeGlobalNamespace)
	{
		_builder = builder;
		_includeGlobalNamespace = includeGlobalNamespace;
	}

	private void AppendName(string text)
	{
		if (SyntaxFacts.GetKeywordKind(text) != SyntaxKind.None)
		{
			_builder.Append('@');
		}

		_builder.Append(text);
	}

	public override void VisitArrayType(IArrayTypeSymbol symbol)
	{
		Visit(symbol.ElementType);
		_builder.Append("[]");
	}

	public override void VisitDynamicType(IDynamicTypeSymbol symbol)
	{
		_builder.Append("dynamic");
	}

	public override void VisitNamedType(INamedTypeSymbol symbol)
	{
		if (GetSpecialTypeName(symbol) is string specialName)
		{
			_builder.Append(specialName);
			return;
		}

		if (symbol.IsGenericType && symbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
		{
			Visit(symbol.TypeArguments[0]);
			_builder.Append('?');
			return;
		}

		Visit(symbol.ContainingSymbol);
		if (symbol.ContainingSymbol is INamespaceSymbol { IsGlobalNamespace: true })
		{
			if (_includeGlobalNamespace)
			{
				_builder.Append("global::");
			}
		}
		else
		{
			_builder.Append('.');
		}

		AppendName(symbol.Name);

		if (symbol.IsGenericType)
		{
			_builder.Append('<');
			for (int i = 0; i < symbol.TypeArguments.Length; i++)
			{
				Visit(symbol.TypeArguments[i]);
				if (i < symbol.TypeArguments.Length - 1)
				{
					_builder.Append(", ");
				}
			}

			_builder.Append('>');
		}
	}

	private static string? GetSpecialTypeName(INamedTypeSymbol symbol)
	{
		return symbol.SpecialType switch
		{
			SpecialType.System_SByte => "sbyte",
			SpecialType.System_Int16 => "short",
			SpecialType.System_Int32 => "int",
			SpecialType.System_Int64 => "long",
			SpecialType.System_Byte => "byte",
			SpecialType.System_UInt16 => "ushort",
			SpecialType.System_UInt32 => "uint",
			SpecialType.System_UInt64 => "ulong",
			SpecialType.System_Single => "float",
			SpecialType.System_Double => "double",
			SpecialType.System_Decimal => "decimal",
			SpecialType.System_Char => "char",
			SpecialType.System_Boolean => "bool",
			SpecialType.System_String => "string",
			SpecialType.System_Object => "object",
			_ => null,
		};
	}

	public override void VisitNamespace(INamespaceSymbol symbol)
	{
		Visit(symbol.ContainingSymbol);
		if (symbol.IsGlobalNamespace)
		{
			return;
		}

		if (symbol.ContainingNamespace?.IsGlobalNamespace == true)
		{
			if (_includeGlobalNamespace)
			{
				_builder.Append("global::");
			}
		}
		else
		{
			_builder.Append('.');
		}

		AppendName(symbol.Name);
	}

	public override void VisitTypeParameter(ITypeParameterSymbol symbol)
	{
		AppendName(symbol.Name);
	}
}
