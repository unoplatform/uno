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
		private static Lazy<ILogger> _log = new Lazy<ILogger>(() => typeof(ResourceLoader).Log());

		private static Dictionary<string, ResourceLoader> _loaders = new Dictionary<string, ResourceLoader>(StringComparer.OrdinalIgnoreCase);
		private static string _defaultLanguage;

		private Dictionary<string, Dictionary<string, string>> _resources = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

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
			string culture = GetUICulture();

			if (FindForCulture(culture, resource, out var value))
			{
				return value;
			}
			else if (FindForCulture(GetParentUICulture(), resource, out var parentValue))
			{
				return parentValue;
			}

#if !__WASM__ && !NET46
			if (GetStringInternal == null)
			{
				throw new InvalidOperationException($"ResourceLoader.GetStringInternal hasn't been set. Make sure ResourceHelper is initialized properly.");
			}

			return GetStringInternal.Invoke(resource);
#else
			return string.Empty;
#endif
		}

		private static string GetUICulture()
		{
			return CultureInfo.CurrentUICulture.IetfLanguageTag;
		}

		private static string GetParentUICulture()
		{
			return CultureInfo.CurrentUICulture.Parent.IetfLanguageTag;
		}

		private bool FindForCulture(string culture, string resource, out string resourceValue)
		{
			if (_log.Value.IsEnabled(LogLevel.Debug))
			{
				_log.Value.Debug($"[{LoaderName}] FindForCulture {culture}, {resource}");
			}

			if (_resources.TryGetValue(culture, out var values))
			{
				if (values.TryGetValue(resource, out var value))
				{
					resourceValue = value;
					return true;
				}
			}

			resourceValue = null;

			return false;
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
		/// Registers an assembly for resources lookup
		/// </summary>
		/// <param name="assembly">The assembly containing upri resources</param>
		public static void AddLookupAssembly(Assembly assembly)
		{
			foreach (var name in assembly.GetManifestResourceNames())
			{
				if (name.EndsWith(".upri"))
				{
					ProcessResourceFile(name, assembly.GetManifestResourceStream(name));
				}
			}
		}

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
			}
		}

		internal static void ClearResources()
		{
			_loaders.Clear();
		}

		internal static void ProcessResourceFile(string fileName, Stream input)
		{
			var currentCulture = CultureInfo.CurrentUICulture.IetfLanguageTag;
			var parentCulture = GetParentUICulture();

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
				var culture = reader.ReadString();

				if (
					// Currently only load the resources for the current culture.
					culture.Equals(currentCulture, StringComparison.OrdinalIgnoreCase)
					|| culture.Equals(parentCulture, StringComparison.OrdinalIgnoreCase)
				)
				{
					var loader = GetNamedResourceLoader(name);

					var resourceCount = reader.ReadInt32();

					if (!loader._resources.TryGetValue(culture, out var resources))
					{
						loader._resources[culture] = resources = new Dictionary<string, string>();
					}

					for (int i = 0; i < resourceCount; i++)
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
						_log.Value.LogDebug($"Skipping resource file {fileName} for {culture} (CurrentCulture {currentCulture}/{parentCulture})");
					}
				}
			}
		}

		private static ResourceLoader GetNamedResourceLoader(string name)
			=> _loaders.FindOrCreate(name, () => new ResourceLoader(name));
	}
}
