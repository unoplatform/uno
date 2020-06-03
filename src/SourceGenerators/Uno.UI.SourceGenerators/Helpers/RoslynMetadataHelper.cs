using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Uno.Extensions;
using Microsoft.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Build.Execution;

namespace Uno.Roslyn
{
	internal class RoslynMetadataHelper
	{
		private const string AdditionalTypesFileName = "additionalTypes.cs";

		private Project _project;
		private readonly INamedTypeSymbol _nullableSymbol;
		private string[] _additionalTypes;
		private readonly Dictionary<string, INamedTypeSymbol> _legacyTypes;
		private readonly Func<string, ITypeSymbol[]> _findTypesByName;
		private readonly Func<string, ITypeSymbol> _findTypeByFullName;
		private readonly Func<INamedTypeSymbol, IEnumerable<INamedTypeSymbol>> _getAllDerivingTypes;
		private readonly Func<string, IEnumerable<INamedTypeSymbol>> _getAllTypesAttributedWith;
		private readonly Dictionary<string, INamedTypeSymbol> _additionalTypesMap;

		public Compilation Compilation { get; }

		public string AssemblyName => _project.AssemblyName;

		public RoslynMetadataHelper(string configuration, Compilation sourceCompilation, ProjectInstance projectInstance, Project roslynProject, string[] additionalTypes = null, Dictionary<string, string> legacyTypes = null)
		{
			Compilation = GenerateAdditionalTypes(sourceCompilation);
			_additionalTypesMap = GenerateAdditionalTypesMap();

			_findTypesByName = Funcs.Create<string, ITypeSymbol[]>(SourceFindTypesByName).AsLockedMemoized();
			_findTypeByFullName = Funcs.Create<string, ITypeSymbol>(SourceFindTypeByFullName).AsLockedMemoized();
			_additionalTypes = additionalTypes ?? new string[0];
			_legacyTypes = BuildLegacyTypes(legacyTypes);
			_getAllDerivingTypes = Funcs.Create<INamedTypeSymbol, IEnumerable<INamedTypeSymbol>>(GetAllDerivingTypes).AsLockedMemoized();
			_getAllTypesAttributedWith = Funcs.Create<string, IEnumerable<INamedTypeSymbol>>(SourceGetAllTypesAttributedWith).AsLockedMemoized();
			_project = roslynProject;
			_nullableSymbol = Compilation.GetTypeByMetadataName("System.Nullable`1");
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

		private Dictionary<string, INamedTypeSymbol> BuildLegacyTypes(Dictionary<string, string> legacyTypes)
			=> legacyTypes
			?.Select(t => (Key: t.Key, Metadata: Compilation.GetTypeByMetadataName(t.Value)))
			?.ToDictionary(t => t.Key, t => t.Metadata)
			?? new Dictionary<string, INamedTypeSymbol>();

		private Compilation GenerateAdditionalTypes(Compilation sourceCompilation)
		{
			if (_additionalTypes?.Any() ?? false)
			{
				var sb = new StringBuilder();
				sb.AppendLine("class __test__ {");

				int index = 0;
				foreach (var type in _additionalTypes)
				{
					sb.AppendLine($"{type} __{index++};");
				}

				sb.AppendLine("}");

				_project = _project.AddDocument(AdditionalTypesFileName, sb.ToString()).Project;

				return _project.GetCompilationAsync().Result;
			}
			else
			{
				return sourceCompilation;
			}
		}

		public ITypeSymbol[] FindTypesByName(string name)
		{
			if (name.HasValue())
			{
				return _findTypesByName(name);
			}

			return Array.Empty<ITypeSymbol>();
		}

		private ITypeSymbol[] SourceFindTypesByName(string name)
		{
			var legacyType = _legacyTypes.UnoGetValueOrDefault(name);

			if (name.HasValue())
			{
				// This validation ensure that the project has been loaded.
				Compilation.Validation().NotNull("Compilation");

				var results = SymbolFinder
					.FindDeclarationsAsync(_project, name, ignoreCase: false, filter: SymbolFilter.Type)
					.Result;

				return results
					.OfType<INamedTypeSymbol>()
					.Where(r => r.Kind != SymbolKind.ErrorType && r.TypeArguments.Length == 0)
					// Apply legacy
					.Where(r => legacyType == null || r.OriginalDefinition.Name != name || r.OriginalDefinition == legacyType)
					.ToArray();
			}

			return Array.Empty<ITypeSymbol>();
		}

		public ITypeSymbol FindTypeByName(string name)
		{
			return FindTypesByName(name).FirstOrDefault();
		}

		public ITypeSymbol FindTypeByFullName(string fullName)
		{
			return _findTypeByFullName(fullName);
		}

		private ITypeSymbol SourceFindTypeByFullName(string fullName)
		{
			if (_legacyTypes.TryGetValue(GetTypeNameFromFullTypeName(fullName), out var legacyType))
			{
				return legacyType;
			}

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
				else if (fullName.StartsWith("System.Nullable`1["))
				{
					const int prefixLength = 18; // System.Nullable'1[
					const int suffixLength = 1; // ]
					var type = FindTypeByFullName(fullName.Substring(prefixLength, fullName.Length - (prefixLength + suffixLength)));
					if (type != null)
					{
						return _nullableSymbol.Construct(type);
					}
				}

				if(_additionalTypesMap.TryGetValue(fullName, out var additionalType))
				{
					return additionalType;
				}
			}

			if (symbol?.Kind == SymbolKind.ErrorType)
			{
				return null;
			}

			return symbol;
		}

		private static string GetTypeNameFromFullTypeName(string fullName)
		{
			int index = fullName.LastIndexOf('.');

			if (index != -1)
			{
				return fullName.Substring(index + 1);
			}

			return fullName;
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

		public INamedTypeSymbol GetSpecial(SpecialType specialType) => Compilation.GetSpecialType(specialType);

		public INamedTypeSymbol GetGenericType(string name = "T") =>  Compilation.CreateErrorTypeSymbol(null, name, 0);

		public IArrayTypeSymbol GetArray(ITypeSymbol type) => Compilation.CreateArrayTypeSymbol(type);

		public IEnumerable<INamedTypeSymbol> GetAllTypesDerivingFrom(INamedTypeSymbol baseType)
		{
			return _getAllDerivingTypes(baseType);
		}

		private IEnumerable<INamedTypeSymbol> GetAllDerivingTypes(INamedTypeSymbol baseType)
		{
			return GetTypesDerivingFrom(Compilation.GlobalNamespace);

			IEnumerable<INamedTypeSymbol> GetTypesDerivingFrom(INamespaceSymbol ns)
			{
				foreach (var member in ns.GetMembers())
				{
					if (member is INamespaceSymbol nsInner)
					{
						foreach (var t in GetTypesDerivingFrom(nsInner))
						{
							yield return t;
						}
					}
					else if (member is INamedTypeSymbol type)
					{
						if (((INamedTypeSymbol)member).Is(baseType))
						{
							yield return type;
						}
					}
				}
			}
		}

		public IEnumerable<INamedTypeSymbol> GetAllTypesAttributedWith(string attributeClassFullName)
		{
			return _getAllTypesAttributedWith(attributeClassFullName);
		}

		private IEnumerable<INamedTypeSymbol> SourceGetAllTypesAttributedWith(string attributeClassFullName)
		{
			return GetTypesAttributedWith(Compilation.GlobalNamespace);

			IEnumerable<INamedTypeSymbol> GetTypesAttributedWith(INamespaceSymbol ns)
			{
				foreach (var member in ns.GetMembers())
				{
					if (member is INamespaceSymbol nsInner)
					{
						foreach (var t in GetTypesAttributedWith(nsInner))
						{
							yield return t;
						}
					}
					else if (member is INamedTypeSymbol type)
					{
						if (type.FindAttribute(attributeClassFullName) is AttributeData _)
						{
							yield return type;
						}
					}
				}
			}
		}
	}
}
