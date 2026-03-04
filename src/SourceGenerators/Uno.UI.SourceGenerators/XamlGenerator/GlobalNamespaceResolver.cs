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

			// Scan referenced assemblies
			foreach (var reference in compilation.References)
			{
				if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly)
				{
					CollectXmlnsDefinitions(assembly, globalUri, namespaces);
				}
			}

			// Scan current assembly
			CollectXmlnsDefinitions(compilation.Assembly, globalUri, namespaces);

			return namespaces.Distinct(StringComparer.Ordinal).ToArray();
		}

		/// <summary>
		/// Scans all referenced assemblies and the current assembly for XmlnsPrefix
		/// attributes, returning prefix-to-URI mappings for implicit availability.
		/// </summary>
		public static (string Prefix, string Uri)[] GetImplicitPrefixes(Compilation compilation)
		{
			var prefixes = new List<(string Prefix, string Uri)>();

			// Scan referenced assemblies
			foreach (var reference in compilation.References)
			{
				if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly)
				{
					CollectXmlnsPrefixes(assembly, prefixes);
				}
			}

			// Scan current assembly
			CollectXmlnsPrefixes(compilation.Assembly, prefixes);

			return prefixes
				.GroupBy(p => p.Prefix, StringComparer.Ordinal)
				.Select(g => g.First())
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

			foreach (var reference in compilation.References)
			{
				if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly)
				{
					CollectAllXmlnsDefinitions(assembly, result);
				}
			}

			CollectAllXmlnsDefinitions(compilation.Assembly, result);
			return result;
		}

		private static void CollectAllXmlnsDefinitions(IAssemblySymbol assembly, Dictionary<string, List<string>> result)
		{
			foreach (var attr in assembly.GetAttributes())
			{
				if (attr.AttributeClass?.Name == "XmlnsDefinitionAttribute"
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

		private static void CollectXmlnsDefinitions(IAssemblySymbol assembly, string globalUri, List<string> namespaces)
		{
			foreach (var attr in assembly.GetAttributes())
			{
				if (attr.AttributeClass?.Name == "XmlnsDefinitionAttribute"
					&& attr.ConstructorArguments.Length >= 2
					&& attr.ConstructorArguments[0].Value is string uri
					&& string.Equals(uri, globalUri, StringComparison.Ordinal)
					&& attr.ConstructorArguments[1].Value is string clrNamespace)
				{
					namespaces.Add(clrNamespace);
				}
			}
		}

		private static void CollectXmlnsPrefixes(IAssemblySymbol assembly, List<(string Prefix, string Uri)> prefixes)
		{
			foreach (var attr in assembly.GetAttributes())
			{
				if (attr.AttributeClass?.Name == "XmlnsPrefixAttribute"
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
