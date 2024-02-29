using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;
using Uno;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;

namespace Windows.ApplicationModel.Resources
{
	public sealed partial class ResourceLoader
	{
		private const int UPRIVersion = 3;
		private const string DefaultResourceLoaderName = "Resources";
		private static readonly Logger _log = typeof(ResourceLoader).Log();

		private static readonly List<Assembly> _lookupAssemblies = new List<Assembly>();
		private static readonly Dictionary<string, ResourceLoader> _loaders = new Dictionary<string, ResourceLoader>(StringComparer.OrdinalIgnoreCase);
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable. - TODO: Annotate properly.
		private static CultureInfo _loadersCulture;
		private static string _loadersDefault;
		private static string[] _loadersHierarchy;

		private static string _defaultLanguage;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

		private readonly Dictionary<string, Dictionary<string, string>> _resources = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

		public ResourceLoader() : this(DefaultResourceLoaderName, true)
		{
		}

		public ResourceLoader(string name) : this(name, true)
		{
		}

		/// <summary>
		/// Creates a loader with a given name.
		/// If the loader does not exist yet, it can add it if requested.
		/// </summary>
		/// <param name="name">Name of the loader.</param>
		/// <param name="addLoader">
		/// A value indicating whether the loader
		/// should be added to the list of loaders.
		/// </param>
		private ResourceLoader(string name, bool addLoader)
		{
			if (_log.IsEnabled(LogLevel.Debug))
			{
				_log.LogDebug($"Initializing ResourceLoader {name} (CurrentUICulture: {CultureInfo.CurrentUICulture})");
			}

			LoaderName = name;

			if (_loaders.TryGetValue(name, out var existingLoader))
			{
				// If there is already a loader with the same name,
				// they should share the same resources.
				_resources = existingLoader._resources;
			}
			else if (addLoader)
			{
				_loaders[name] = this;
			}
		}

		internal string LoaderName { get; }

		public string GetString(string resource)
		{
			// "/[file]/[name]" format support
			if (resource.ElementAtOrDefault(0) == '/')
			{
				var separatorIndex = resource.IndexOf('/', 1);
				if (separatorIndex < 1)
				{
					return "";
				}
				var resourceFile = resource.Substring(1, separatorIndex - 1);
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

			return string.Empty;
		}

		private bool FindForCulture(string culture, string resource, [NotNullWhen(true)] out string? resourceValue)
		{
			if (_log.IsEnabled(LogLevel.Debug))
			{
				_log.Debug($"[{LoaderName}] FindForCulture {culture}, {resource}");
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

		public static ResourceLoader GetForCurrentView() => GetOrCreateNamedResourceLoader(DefaultResourceLoaderName);

		public static ResourceLoader GetForCurrentView(string name) => GetOrCreateNamedResourceLoader(name);

		public static ResourceLoader GetForViewIndependentUse() => GetOrCreateNamedResourceLoader(DefaultResourceLoaderName);

		public static ResourceLoader GetForViewIndependentUse(string name) => GetOrCreateNamedResourceLoader(name);

		[NotImplemented]
		public static string GetStringForReference(Uri uri) { throw new NotSupportedException(); }

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

				// If we have a culture that doesn't specify country/region, we want to match its specific cultures if it's not found.
				// For example, if we have es and it's not found, we want to match es-MX
				if (culture.IsNeutralCulture)
				{
					var specificCultures = CultureInfo.GetCultures(CultureTypes.SpecificCultures).Where(c => c.Parent.Equals(culture)).OrderByDescending(x => x.Name);
					foreach (var specificCulture in specificCultures)
					{
						yield return specificCulture.IetfLanguageTag.ToLowerInvariant();
					}
				}

				if (culture == culture.Parent)
				{
					// On .NET Framework 4.8, culture may equal culture.Parent even if it's
					// not equal to CultureInfo.InvariantCulture when reaching the end of the
					// culture chain.
					break;
				}

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
				if (name.EndsWith(".upri", StringComparison.Ordinal))
				{
					ProcessResourceFile(name, assembly.GetManifestResourceStream(name)!, currentCultures);
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

				bool adjustKeyTransformationForV2 = false;
				var version = reader.ReadInt32();
				if (version == 2)
				{
					version = UPRIVersion;
					adjustKeyTransformationForV2 = true;
				}

				if (version != UPRIVersion)
				{
					throw new InvalidOperationException($"The resource file {fileName} has an invalid version (got {version}, expecting {UPRIVersion})");
				}

				var name = reader.ReadString();
				var culture = reader.ReadString().ToLowerInvariant();

				// Currently only load the resources for the current culture.
				if (currentCultures.Contains(culture))
				{
					var loader = GetOrCreateNamedResourceLoader(name);
					if (!loader._resources.TryGetValue(culture, out var resources))
					{
						loader._resources[culture] = resources = new Dictionary<string, string>();
					}

					var resourceCount = reader.ReadInt32();
					StringBuilder sb = new();
					for (var i = 0; i < resourceCount; i++)
					{
						var key = reader.ReadString();
						var value = reader.ReadString();

						if (adjustKeyTransformationForV2)
						{
							// Restore the original format
							key = key.Replace("/", ".");

							var firstDotIndex = key.IndexOf('.');
							if (firstDotIndex != -1)
							{
								sb.Clear();
								sb.Append(key);

								sb[firstDotIndex] = '/';

								key = sb.ToString();
							}
						}

						if (_log.IsEnabled(LogLevel.Debug))
						{
							_log.Debug($"[{name}, {fileName}, {culture}] Adding resource {key}={value}");
						}

						resources[key] = value;
					}
				}
				else
				{
					if (_log.IsEnabled(LogLevel.Debug))
					{
						_log.LogDebug($"Skipping resource file {fileName} for {culture} (CurrentCulture {CultureInfo.CurrentUICulture.IetfLanguageTag})");
					}
				}
			}
		}

		private static ResourceLoader GetOrCreateNamedResourceLoader(string name) =>
			_loaders.FindOrCreate(name, () => new ResourceLoader(name, addLoader: false));
	}
}
