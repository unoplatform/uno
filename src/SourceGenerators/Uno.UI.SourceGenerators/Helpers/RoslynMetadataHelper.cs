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

		private readonly Task<Compilation> _compilationTask;
		private Project _project;
		private string[] _additionalTypes;
		private readonly Dictionary<string, string> _legacyTypes;
		private readonly Func<string, ITypeSymbol[]> _findTypesByName;
		private readonly Func<string, ITypeSymbol> _findTypeByFullName;

		public Compilation Compilation { get; }

		public RoslynMetadataHelper(string configuration, Compilation sourceCompilation, ProjectInstance projectInstance, Project roslynProject, string[] additionalTypes = null, Dictionary<string, string> legacyTypes = null)
		{
			Compilation = sourceCompilation;

			_findTypesByName = Funcs.Create<string, ITypeSymbol[]>(SourceFindTypesByName).AsLockedMemoized();
			_findTypeByFullName = Funcs.Create<string, ITypeSymbol>(SourceFindTypeByFullName).AsLockedMemoized();
			_additionalTypes = additionalTypes ?? new string[0];
			_legacyTypes = legacyTypes ?? new Dictionary<string, string>();
			_project = roslynProject;

			_compilationTask = Task.FromResult(sourceCompilation);

			GenerateAdditionalTypes();
		}

		private void GenerateAdditionalTypes()
		{
			if (_additionalTypes.Any())
			{
				var sb = new StringBuilder();
				sb.AppendLine("class __test__ {");

				int index = 0;
				foreach (var type in _additionalTypes)
				{
					sb.AppendLine($"{type} __{index};");
				}

				sb.AppendLine("}");

				_project = _project.AddDocument(AdditionalTypesFileName, sb.ToString()).Project;
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
					.Where(r => r.Kind != SymbolKind.ErrorType && r.TypeArguments.None())
					// Apply legacy
					.Where(r => legacyType == null || r.OriginalDefinition.Name != name || r.OriginalDefinition.GetFullName() == legacyType)
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
			// Apply legacy
			fullName = _legacyTypes.UnoGetValueOrDefault(fullName?.Split('.').Last()) ?? fullName;
			
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
						return ((INamedTypeSymbol) FindTypeByFullName("System.Nullable`1")).Construct(type);
					}
				}

				var tree = Compilation.SyntaxTrees.FirstOrDefault(s => s.FilePath == AdditionalTypesFileName);

				if (tree != null)
				{
					var fieldSymbol = tree
						.GetRoot()
						.DescendantNodesAndSelf()
						.OfType<FieldDeclarationSyntax>()
						.Where(f => f.Declaration.Type.ToString() == fullName)
						.FirstOrDefault();

					if (fieldSymbol != null)
					{
						var info = Compilation.GetSemanticModel(tree).GetSymbolInfo(fieldSymbol.Declaration.Type);

						if (info.Symbol != null && info.Symbol.Kind != SymbolKind.ErrorType)
						{
							return info.Symbol as ITypeSymbol;
						}

						var declaredSymbol = Compilation.GetSemanticModel(tree).GetDeclaredSymbol(fieldSymbol.Declaration.Type);

						if (declaredSymbol != null && declaredSymbol.Kind != SymbolKind.ErrorType)
						{
							return declaredSymbol as ITypeSymbol;
						}
					}
				}
			}

			if (symbol?.Kind == SymbolKind.ErrorType)
			{
				return null;
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

		public INamedTypeSymbol GetSpecial(SpecialType specialType) => Compilation.GetSpecialType(specialType);

		public INamedTypeSymbol GetGenericType(string name = "T") =>  Compilation.CreateErrorTypeSymbol(null, name, 0);

		public IArrayTypeSymbol GetArray(ITypeSymbol type) => Compilation.CreateArrayTypeSymbol(type);
	}
}
