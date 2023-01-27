using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Uno.Extensions;

#if NETFRAMEWORK
using GeneratorExecutionContext = Uno.SourceGeneration.GeneratorExecutionContext;
#endif

namespace Uno.Roslyn
{
	internal class RoslynMetadataHelper
	{
		private const string AdditionalTypesFileName = "additionalTypes.cs";

		private readonly INamedTypeSymbol _nullableSymbol;
		private readonly Func<string, ITypeSymbol> _findTypeByFullName;
		private readonly Func<INamedTypeSymbol, INamedTypeSymbol[]> _getAllTypesAttributedWith;
		private readonly Dictionary<string, INamedTypeSymbol> _additionalTypesMap;

		public Compilation Compilation { get; }

		public string AssemblyName => Compilation.AssemblyName;

		public RoslynMetadataHelper(GeneratorExecutionContext context)
		{
			Compilation = context.Compilation;
			_additionalTypesMap = GenerateAdditionalTypesMap();

			_findTypeByFullName = Funcs.Create<string, ITypeSymbol>(SourceFindTypeByFullName).AsLockedMemoized();
			_getAllTypesAttributedWith = Funcs.Create<INamedTypeSymbol, INamedTypeSymbol[]>(SourceGetAllTypesAttributedWith).AsLockedMemoized();
			_nullableSymbol = Compilation.GetSpecialType(SpecialType.System_Nullable_T);
		}

		private Dictionary<string, INamedTypeSymbol> GenerateAdditionalTypesMap()
		{
			var tree = Compilation.SyntaxTrees.FirstOrDefault(s => s.FilePath == AdditionalTypesFileName);

			if (tree != null)
			{
				var additionalTypesTree = Compilation.GetSemanticModel(tree);

				INamedTypeSymbol GetFieldSymbol(FieldDeclarationSyntax fieldSyntax)
				{
					var info = additionalTypesTree.GetSymbolInfo(fieldSyntax.Declaration.Type);

					if (info.Symbol != null && info.Symbol.Kind != SymbolKind.ErrorType)
					{
						return info.Symbol as INamedTypeSymbol;
					}

					var declaredSymbol = additionalTypesTree.GetDeclaredSymbol(fieldSyntax.Declaration.Type);

					if (declaredSymbol != null && declaredSymbol.Kind != SymbolKind.ErrorType)
					{
						return declaredSymbol as INamedTypeSymbol;
					}

					return null;
				}

				return tree
					.GetRoot()
					.DescendantNodesAndSelf()
					.OfType<FieldDeclarationSyntax>()
					.ToDictionary(s => s.Declaration.Type.ToString(), GetFieldSymbol);
			}
			else
			{
				return new Dictionary<string, INamedTypeSymbol>();
			}
		}

		public ITypeSymbol FindTypeByFullName(string fullName)
		{
			return _findTypeByFullName(fullName);
		}

		private ITypeSymbol SourceFindTypeByFullName(string fullName)
		{
			var symbol = Compilation.GetTypeByMetadataName(fullName);

			if (symbol?.Kind == SymbolKind.ErrorType)
			{
				symbol = null;
			}

			if (symbol == null)
			{
				// This type resolution is required because there is no way (yet) to get a type 
				// symbol from a string for types that are not "simple", like generic types or arrays.

				// We then use a temporary documents that contains all the known
				// additional types from the constructor of this class, then search for symbols through it.

				if (fullName.EndsWith("[]", StringComparison.OrdinalIgnoreCase))
				{
					var type = FindTypeByFullName(fullName.Substring(0, fullName.Length - 2));
					if (type != null)
					{
						type = Compilation.CreateArrayTypeSymbol(type);
						return type;
					}
				}
				else if (fullName.StartsWith("System.Nullable`1[", StringComparison.Ordinal))
				{
					const int prefixLength = 18; // System.Nullable'1[
					const int suffixLength = 1; // ]
					var type = FindTypeByFullName(fullName.Substring(prefixLength, fullName.Length - (prefixLength + suffixLength)));
					if (type != null)
					{
						return _nullableSymbol.Construct(type);
					}
				}

				if (_additionalTypesMap.TryGetValue(fullName, out var additionalType))
				{
					return additionalType;
				}
			}

			return symbol;
		}

		public ITypeSymbol GetTypeByFullName(string fullName)
		{
			var symbol = FindTypeByFullName(fullName);

			if (symbol == null)
			{
				throw new InvalidOperationException($"Unable to find type {fullName}");
			}

			return symbol;
		}

		public INamedTypeSymbol[] GetAllTypesAttributedWith(INamedTypeSymbol attributeClass)
			=> _getAllTypesAttributedWith(attributeClass);

		private INamedTypeSymbol[] SourceGetAllTypesAttributedWith(INamedTypeSymbol attributeClass)
			=> Compilation.GlobalNamespace.GetNamespaceTypes().Where(t => t.FindAttribute(attributeClass) is { }).ToArray();
	}
}
