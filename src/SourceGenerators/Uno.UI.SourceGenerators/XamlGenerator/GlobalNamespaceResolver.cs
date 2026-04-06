#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	/// <summary>
	/// Discovers XmlnsDefinition and XmlnsPrefix attributes from the compilation's
	/// referenced assemblies and the current assembly, targeting the global namespace URI.
	/// </summary>
	internal static class GlobalNamespaceResolver
	{
		/// <summary>
		/// Scans all referenced assemblies and the current assembly for XmlnsDefinition
		/// attributes targeting the specified global URI, returning the CLR namespaces.
		/// </summary>
		public static string[] GetGlobalClrNamespaces(Compilation compilation, string globalUri)
		{
			var namespaces = new List<string>();
			var xmlnsDefSymbol = GetXmlnsDefinitionSymbol(compilation);

			// Scan referenced assemblies
			foreach (var reference in compilation.References)
			{
				if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly)
				{
					CollectXmlnsDefinitions(assembly, globalUri, namespaces, xmlnsDefSymbol);
				}
			}

			// Scan current assembly
			CollectXmlnsDefinitions(compilation.Assembly, globalUri, namespaces, xmlnsDefSymbol);

			return namespaces.Distinct(StringComparer.Ordinal).ToArray();
		}

		/// <summary>
		/// Scans all referenced assemblies and the current assembly for XmlnsPrefix
		/// attributes, returning prefix-to-URI mappings for implicit availability.
		/// When multiple assemblies register the same prefix, the current assembly wins;
		/// ties within the same tier are broken by URI for deterministic ordering.
		/// </summary>
		public static (string Prefix, string Uri)[] GetImplicitPrefixes(Compilation compilation)
		{
			var prefixes = new List<(string Prefix, string Uri)>();
			var xmlnsPrefixSymbol = GetXmlnsPrefixSymbol(compilation);

			// Scan referenced assemblies
			foreach (var reference in compilation.References)
			{
				if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly)
				{
					CollectXmlnsPrefixes(assembly, prefixes, xmlnsPrefixSymbol);
				}
			}

			// Remember how many prefixes came from referenced assemblies so that
			// we can give deterministic precedence to the current assembly below.
			var referencePrefixCount = prefixes.Count;

			// Scan current assembly
			CollectXmlnsPrefixes(compilation.Assembly, prefixes, xmlnsPrefixSymbol);

			return prefixes
				.Select((p, index) => (p.Prefix, p.Uri, Index: index))
				.GroupBy(p => p.Prefix, StringComparer.Ordinal)
				.Select(g => g
					// Prefer prefixes declared in the current assembly (indices >= referencePrefixCount),
					// then use URI as a stable tiebreaker within the same precedence tier.
					.OrderBy(p => p.Index >= referencePrefixCount ? 0 : 1)
					.ThenBy(p => p.Uri, StringComparer.Ordinal)
					.First())
				.Select(p => (p.Prefix, p.Uri))
				.ToArray();
		}

		/// <summary>
		/// Scans all referenced assemblies and the current assembly for all XmlnsDefinition
		/// attributes, returning a dictionary mapping XML namespace URI to CLR namespaces.
		/// This enables type resolution for any URI registered via XmlnsDefinition.
		/// </summary>
		public static Dictionary<string, List<string>> GetAllXmlnsDefinitions(Compilation compilation)
		{
			var result = new Dictionary<string, List<string>>(StringComparer.Ordinal);
			var xmlnsDefSymbol = GetXmlnsDefinitionSymbol(compilation);

			foreach (var reference in compilation.References)
			{
				if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly)
				{
					CollectAllXmlnsDefinitions(assembly, result, xmlnsDefSymbol);
				}
			}

			CollectAllXmlnsDefinitions(compilation.Assembly, result, xmlnsDefSymbol);
			return result;
		}

		private static INamedTypeSymbol? GetXmlnsDefinitionSymbol(Compilation compilation)
			=> compilation.GetTypeByMetadataName("System.Windows.Markup.XmlnsDefinitionAttribute");

		private static INamedTypeSymbol? GetXmlnsPrefixSymbol(Compilation compilation)
			=> compilation.GetTypeByMetadataName("System.Windows.Markup.XmlnsPrefixAttribute");

		private static bool IsAttributeMatch(AttributeData attr, INamedTypeSymbol? expectedSymbol, string expectedName)
		{
			if (expectedSymbol is not null)
			{
				return SymbolEqualityComparer.Default.Equals(attr.AttributeClass, expectedSymbol);
			}

			// Fall back to name-based matching if the symbol couldn't be resolved
			return attr.AttributeClass?.Name == expectedName;
		}

		private static void CollectAllXmlnsDefinitions(IAssemblySymbol assembly, Dictionary<string, List<string>> result, INamedTypeSymbol? xmlnsDefSymbol)
		{
			foreach (var attr in assembly.GetAttributes())
			{
				if (IsAttributeMatch(attr, xmlnsDefSymbol, "XmlnsDefinitionAttribute")
					&& attr.ConstructorArguments.Length >= 2
					&& attr.ConstructorArguments[0].Value is string uri
					&& attr.ConstructorArguments[1].Value is string clrNamespace)
				{
					if (!result.TryGetValue(uri, out var list))
					{
						list = new List<string>();
						result[uri] = list;
					}

					if (!list.Contains(clrNamespace))
					{
						list.Add(clrNamespace);
					}
				}
			}
		}

		private static void CollectXmlnsDefinitions(IAssemblySymbol assembly, string globalUri, List<string> namespaces, INamedTypeSymbol? xmlnsDefSymbol)
		{
			foreach (var attr in assembly.GetAttributes())
			{
				if (IsAttributeMatch(attr, xmlnsDefSymbol, "XmlnsDefinitionAttribute")
					&& attr.ConstructorArguments.Length >= 2
					&& attr.ConstructorArguments[0].Value is string uri
					&& string.Equals(uri, globalUri, StringComparison.Ordinal)
					&& attr.ConstructorArguments[1].Value is string clrNamespace)
				{
					namespaces.Add(clrNamespace);
				}
			}
		}

		private static void CollectXmlnsPrefixes(IAssemblySymbol assembly, List<(string Prefix, string Uri)> prefixes, INamedTypeSymbol? xmlnsPrefixSymbol)
		{
			foreach (var attr in assembly.GetAttributes())
			{
				if (IsAttributeMatch(attr, xmlnsPrefixSymbol, "XmlnsPrefixAttribute")
					&& attr.ConstructorArguments.Length >= 2
					&& attr.ConstructorArguments[0].Value is string uri
					&& attr.ConstructorArguments[1].Value is string prefix
					&& !string.IsNullOrEmpty(prefix))
				{
					prefixes.Add((prefix, uri));
				}
			}
		}
	}
}
