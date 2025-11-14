#nullable enable

using Uno.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using static Uno.UI.Xaml.XamlFilePathHelper;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	internal sealed class XamlGlobalStaticResourcesMap(XamlFileDefinition[] xamlFiles)
	{
		private readonly Dictionary<string, XamlFileDefinition> _rdMap = BuildResourceDictionaryMap(xamlFiles);

		/// <summary>
		/// Gets the names of all GlobalStaticResources properties associated with a top-level ResourceDictionary.
		/// </summary>
		/// <param name="initialFiles">File names of the ResourceDictionaries whose properties should be returned first.</param>
		/// <param name="ignoredFiles">Files which shouldn't be included in the default system resources</param>
		/// <remarks>This is used when building Uno.UI itself to create a master dictionary of system resources.</remarks>
		internal IEnumerable<string> GetAllDictionaryProperties()
			=> _rdMap.Select(kvp => ConvertIdToResourceDictionaryProperty(kvp.Value.UniqueID));

		/// <summary>
		/// Gets the name of a GlobalStaticResources property associated with a ResourceDictionary.Source designation. Throws an exception if none is found.
		/// </summary>
		/// <param name="originDictionary">The file containing the XAML</param>
		/// <param name="source">The ResourceDictionary.Source value</param>
		internal string? FindTargetPropertyForMergedDictionarySource(XamlFileDefinition originDictionary, string source)
		{
			var targetSource = GetSourceLink(originDictionary);
			var absoluteSource = ResolveAbsoluteSource(targetSource, source);

			if (_rdMap.TryGetValue(absoluteSource, out var xamlFileDefinition))
			{
				// TODO: check if it actually corresponds to a ResourceDictionary
				return ConvertIdToResourceDictionaryProperty(xamlFileDefinition.UniqueID);
			}

			return null;
		}

		/// <summary>
		/// Get the appropriate source link for a given XAML file.
		/// </summary>
		internal string GetSourceLink(XamlFileDefinition xamlFileDefinition)
		{
			return xamlFileDefinition.SourceLink!;
		}

		private string ConvertIdToResourceDictionaryProperty(string id) => "{0}_ResourceDictionary".InvariantCultureFormat(id);

		/// <summary>
		/// Build a map of source links to corresponding XAML files, used for ResourceDictionary.Source resolution.
		/// </summary>
		private static Dictionary<string, XamlFileDefinition> BuildResourceDictionaryMap(XamlFileDefinition[] files)
		{
			var map = new Dictionary<string, XamlFileDefinition>(StringComparer.CurrentCultureIgnoreCase);
			for (var i = 0; i < files.Length; i++)
			{
				var file = files[i];
				map[file.SourceLink] = file;
			}
			return map;
		}
	}
}
