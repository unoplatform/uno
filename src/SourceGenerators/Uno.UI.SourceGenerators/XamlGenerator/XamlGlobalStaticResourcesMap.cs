using Uno.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	internal class XamlGlobalStaticResourcesMap
	{
		private const string AppXIdentifier = "ms-appx:///";
		private readonly Dictionary<string, List<StaticResourceDefinition>> _map = new Dictionary<string, List<StaticResourceDefinition>>();
		private readonly Dictionary<string, XamlFileDefinition> _rdMap = new Dictionary<string, XamlFileDefinition>(StringComparer.CurrentCultureIgnoreCase);

		public XamlGlobalStaticResourcesMap()
		{
		}

		internal StaticResourceDefinition FindResource(string resourceKey)
		{
			var list = GetListForKey(resourceKey);

			return list.OrderBy(k => k.Precedence).FirstOrDefault();
		}

		internal void Add(string staticResourceKey, string ns, ResourcePrecedence precedence)
		{
			var list = GetListForKey(staticResourceKey);

			list.Add(new StaticResourceDefinition(staticResourceKey, ns, precedence));
		}

		/// <summary>
		/// Gets the names of all GlobalStaticResources properties associated with a top-level ResourceDictionary.
		/// </summary>
		/// <param name="initialFiles">File names of the ResourceDictionaries whose properties should be returned first.</param>
		/// <remarks>This is used when building Uno.UI itself to create a master dictionary of system resources.</remarks>
		internal IEnumerable<string> GetAllDictionaryProperties(string[] initialFiles)
		{
			var initialProperties = initialFiles.Select(f =>
					_rdMap.First(kvp =>
						kvp.Key.EndsWith(f, StringComparison.InvariantCultureIgnoreCase)
					)
				)
				.ToArray();

			return initialProperties.Concat(
					_rdMap.Except(initialProperties)
				)
				.Select(kvp => ConvertIdToResourceDictionaryProperty(kvp.Value.UniqueID));
		}

		/// <summary>
		/// Gets the name of a GlobalStaticResources property associated with a ResourceDictionary.Source designation. Throws an exception if none is found.
		/// </summary>
		/// <param name="originDictionary">The file containing the XAML</param>
		/// <param name="source">The ResourceDictionary.Source value</param>
		internal string FindTargetPropertyForMergedDictionarySource(XamlFileDefinition originDictionary, string source)
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
			return _rdMap.FirstOrDefault(kvp => kvp.Value == xamlFileDefinition).Key; //TODO: this is O(n), is it an actual perf issue?
		}

		/// <summary>
		/// Convert relative source path to absolute path.
		/// </summary>
		private string ResolveAbsoluteSource(string origin, string relativeTargetPath)
		{
			if (relativeTargetPath.StartsWith(AppXIdentifier))
			{
				// The path is already absolute. (Currently we assume it's in the local assembly.)
				var trimmedPath = relativeTargetPath.TrimStart(AppXIdentifier);
				var i = trimmedPath.IndexOf('/');
				return trimmedPath.Substring(i + 1);
			}

			var originDirectory = Path.GetDirectoryName(origin);
			if (originDirectory.IsNullOrWhiteSpace())
			{
				return relativeTargetPath;
			}

			var absoluteTargetPath = GetAbsolutePath(originDirectory, relativeTargetPath);

			return absoluteTargetPath.Replace('\\', '/');
		}

		private string GetAbsolutePath(string originDirectory, string relativeTargetPath)
		{
			var addedRootLength = 0;
			if (Path.GetPathRoot(originDirectory).Length == 0)
			{
				var localRoot = Path.GetPathRoot(Directory.GetCurrentDirectory());
				addedRootLength = localRoot.Length;
				// Prepend a dummy root so that GetFullPath doesn't try to add the working directory. We remove it immediately afterward.
				originDirectory = localRoot + originDirectory;
			}
			var absoluteTargetPath = Path.GetFullPath(
					Path.Combine(originDirectory, relativeTargetPath)
				);

			absoluteTargetPath = absoluteTargetPath.Substring(addedRootLength);

			return absoluteTargetPath;
		}

		private string ConvertIdToResourceDictionaryProperty(string id) => "{0}_ResourceDictionary".InvariantCultureFormat(id);

		/// <summary>
		/// Build a map of source links to corresponding XAML files, used for ResourceDictionary.Source resolution.
		/// </summary>
		internal void BuildResourceDictionaryMap(XamlFileDefinition[] files, string[] links)
		{
			for (int i = 0; i < files.Length; i++)
			{
				_rdMap[links[i].Replace('\\', '/')] = files[i];
			}
		}

		private List<StaticResourceDefinition> GetListForKey(string staticResourceKey)
		{
			return _map.FindOrCreate(staticResourceKey, () => new List<StaticResourceDefinition>());
		}

		public enum ResourcePrecedence : int
		{
			Local = 0,
			Library,
			System,
		}

		public class StaticResourceDefinition
		{
			public StaticResourceDefinition(string staticResourceKey, string ns, ResourcePrecedence precedence)
			{
				Name = staticResourceKey;
				Namespace = ns;
				Precedence = precedence;
			}

			public string Name { get; }

			public string Namespace { get; }

			public ResourcePrecedence Precedence { get; }
		}
	}
}
