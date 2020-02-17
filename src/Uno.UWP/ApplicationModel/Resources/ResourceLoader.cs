using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Uno;
using Uno.Extensions;
using Uno.Logging;
using Uno.UI;
using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Windows.ApplicationModel.Resources
{
	public sealed partial class ResourceLoader
	{
		private const int UPRIVersion = 2;
		private const string DefaultResourceLoaderName = "Resources";
		private static readonly Lazy<ILogger> _log = new Lazy<ILogger>(() => typeof(ResourceLoader).Log());

		private static readonly List<Assembly> _lookupAssemblies = new List<Assembly>();
		private static readonly Dictionary<string, ResourceLoader> _loaders = new Dictionary<string, ResourceLoader>(StringComparer.OrdinalIgnoreCase);
		private static CultureInfo _loadersCulture;
		private static string _loadersDefault;
		private static string[] _loadersHierarchy;

		private static string _defaultLanguage;

		private readonly Dictionary<string, Dictionary<string, string>> _resources = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

		public ResourceLoader(string name)
		{
			LoaderName = name;
		}

		internal string LoaderName { get; }

		public ResourceLoader()
		{
			if (_log.Value.IsEnabled(LogLevel.Debug))
			{
				_log.Value.LogDebug($"Initializing ResourceLoader (CurrentUICulture: {CultureInfo.CurrentUICulture})");
			}
		}

		public string GetString(string resource)
		{
			// "/[file]/[name]" format support
			if (resource.ElementAtOrDefault(0) == '/')
			{
				var separatorIndex = resource.IndexOf("/", 1);
				if (separatorIndex < 1)
				{
					return "";
				}
				var resourceFile = resource.Substring(1, separatorIndex-1);
				var resourceName = resource.Substring(separatorIndex + 1);
				return GetForCurrentView(resourceFile).GetString(resourceName);
			}

			// First make sure that resource cache matches the current culture
			var cultures = EnsureLoadersCultures();

			// Walk the culture hierarchy and the default
			foreach (var culture in cultures)
			{
				if (FindForCulture(culture, resource, out var value))
				{
					return value;
				}
			}

			// Finally try to fallback on the native localization system
#if !__WASM__ && !NET461
			if (GetStringInternal == null)
			{
				throw new InvalidOperationException($"ResourceLoader.GetStringInternal hasn't been set. Make sure ResourceHelper is initialized properly.");
			}

			return GetStringInternal.Invoke(resource);
#else
			return string.Empty;
#endif
		}

		private bool FindForCulture(string culture, string resource, out string resourceValue)
		{
			if (_log.Value.IsEnabled(LogLevel.Debug))
			{
				_log.Value.Debug($"[{LoaderName}] FindForCulture {culture}, {resource}");
			}

			if (_resources.TryGetValue(culture, out var values)
				&& values.TryGetValue(resource, out resourceValue))
			{
				return true;
			}
			else
			{
				resourceValue = null;
				return false;
			}
		}

		[NotImplemented]
		public string GetStringForUri(Uri uri) { throw new NotSupportedException(); }

		public static ResourceLoader GetForCurrentView() => GetNamedResourceLoader(DefaultResourceLoaderName);

		public static ResourceLoader GetForCurrentView(string name) => GetNamedResourceLoader(name);

		public static ResourceLoader GetForViewIndependentUse() => GetNamedResourceLoader(DefaultResourceLoaderName);

		public static ResourceLoader GetForViewIndependentUse(string name) => GetNamedResourceLoader(name);

		[NotImplemented]
		public static string GetStringForReference(Uri uri) { throw new NotSupportedException(); }

		// TODO: Remove this property when getting rid of ResourceHelper
		public static Func<string, string> GetStringInternal { get; set; }

		/// <summary>
		/// Provides the default culture if CurrentUICulture cannot provide it.
		/// </summary>
		public static string DefaultLanguage
		{
			get => _defaultLanguage;
			set
			{
				_defaultLanguage = value;

#if __WASM__
				if (CultureInfo.CurrentUICulture.IetfLanguageTag.Length == 0)
				{
					CultureInfo.CurrentCulture = new CultureInfo(DefaultLanguage);
					CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture;
				}
#endif

				EnsureLoadersCultures();
			}
		}

		/// <summary>
		/// Registers an assembly for resources lookup
		/// </summary>
		/// <param name="assembly">The assembly containing upri resources</param>
		public static void AddLookupAssembly(Assembly assembly)
		{
			_lookupAssemblies.Add(assembly);

			var current = CultureInfo.CurrentUICulture;
			var defaultLanguage = DefaultLanguage;

			if (current == _loadersCulture
				&& defaultLanguage == _loadersDefault)
			{
				// The cache matches the current culture, we only have to load resources from the given assembly
				ProcessAssembly(assembly, _loadersHierarchy);
			}
			else
			{
				// The current culture was altered, we have to rebuild the whole cache
				ReloadResources(current, defaultLanguage);
			}
		}

		private static void ClearResources()
		{
			// We clear each loader independently instead of clearing the '_loaders'
			// so if a loader instance has been captured, it will  be updated
			foreach (var loader in _loaders.Values)
			{
				loader._resources.Clear();
			}
		}

		private static IEnumerable<string> GetCulturesHierarchy(CultureInfo culture)
		{
			while (culture != CultureInfo.InvariantCulture)
			{
				yield return culture.IetfLanguageTag.ToLowerInvariant();

				culture = culture.Parent;
			}

			var defaultLanguage = DefaultLanguage.ToLowerInvariant();

			yield return defaultLanguage;

			var separatorIndex = defaultLanguage.Length;
			while ((separatorIndex = defaultLanguage.LastIndexOf('-', separatorIndex - 1)) > 0)
			{
				yield return defaultLanguage.Substring(0, separatorIndex);
			}
		}

		private static string[] EnsureLoadersCultures()
		{
			var current = CultureInfo.CurrentUICulture;
			var defaultLanguage = DefaultLanguage;

			if (current != _loadersCulture
				|| defaultLanguage != _loadersDefault)
			{
				ReloadResources(current, defaultLanguage);
			}

			return _loadersHierarchy;
		}

		private static void ReloadResources(CultureInfo current, string defaultLanguage)
		{
			ClearResources();

			var hierarchy = GetCulturesHierarchy(current).Distinct().ToArray();
			foreach (var assembly in _lookupAssemblies)
			{
				ProcessAssembly(assembly, hierarchy);
			}

			_loadersCulture = current;
			_loadersDefault = defaultLanguage;
			_loadersHierarchy = hierarchy;
		}

		private static void ProcessAssembly(Assembly assembly, string[] currentCultures)
		{
			foreach (var name in assembly.GetManifestResourceNames())
			{
				if (name.EndsWith(".upri"))
				{
					ProcessResourceFile(name, assembly.GetManifestResourceStream(name), currentCultures);
				}
			}
		}

		private static void ProcessResourceFile(string fileName, Stream input, string[] currentCultures)
		{
			using (var reader = new BinaryReader(input))
			{
				// "Magic" sequence to ensure we're reading a proper resource file
				if (!reader.ReadBytes(3).SequenceEqual(new byte[] { 0x75, 0x6E, 0x6F }))
				{
					throw new InvalidOperationException($"The file {fileName} is not a resource file");
				}

				var version = reader.ReadInt32();
				if (version != UPRIVersion)
				{
					throw new InvalidOperationException($"The resource file {fileName} has an invalid version (got {version}, expecting {UPRIVersion})");
				}

				var name = reader.ReadString();
				var culture = reader.ReadString().ToLowerInvariant();

				// Currently only load the resources for the current culture.
				if (currentCultures.Contains(culture))
				{
					var loader = GetNamedResourceLoader(name);
					if (!loader._resources.TryGetValue(culture, out var resources))
					{
						loader._resources[culture] = resources = new Dictionary<string, string>();
					}

					var resourceCount = reader.ReadInt32();
					for (var i = 0; i < resourceCount; i++)
					{
						var key = reader.ReadString();
						var value = reader.ReadString();

						if (_log.Value.IsEnabled(LogLevel.Debug))
						{
							_log.Value.Debug($"[{name}, {fileName}, {culture}] Adding resource {key}={value}");
						}

						resources[key] = value;
					}
				}
				else
				{
					if (_log.Value.IsEnabled(LogLevel.Debug))
					{
						_log.Value.LogDebug($"Skipping resource file {fileName} for {culture} (CurrentCulture {CultureInfo.CurrentUICulture.IetfLanguageTag})");
					}
				}
			}
		}

		private static ResourceLoader GetNamedResourceLoader(string name)
			=> _loaders.FindOrCreate(name, () => new ResourceLoader(name));
	}
}
