using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.UI;
using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Windows.ApplicationModel.Resources
{
	public sealed partial class ResourceLoader
	{
		private static Dictionary<string, Dictionary<string, string>> _resources = new Dictionary<string, Dictionary<string, string>>();

		private static readonly ResourceLoader _loader = new ResourceLoader();

		public ResourceLoader(string name) { }

		public ResourceLoader() {

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"Initializing ResourceLoader (CurrentUICulture: {CultureInfo.CurrentUICulture})");
			}
		}

		public string GetString(string resource)
		{
			var culture = CultureInfo.CurrentUICulture.IetfLanguageTag;

#if __WASM__
			if (!culture.HasValue())
			{
				// This may happend in WASM (mono does not set it properly yet)
				culture = DefaultLanguage;
			}
#endif

			if (FindForCulture(culture, resource, out var value))
			{
				return value;
			}
			else if (FindForCulture(CultureInfo.CurrentUICulture.Parent.IetfLanguageTag, resource, out var parentValue))
			{
				return parentValue;
			}

			if (GetStringInternal == null)
			{
				throw new InvalidOperationException($"ResourceLoader.GetStringInternal hasn't been set. Make sure ResourceHelper is initialized properly.");
			}

			return GetStringInternal.Invoke(resource);
		}

		private bool FindForCulture(string culture, string resource, out string resourceValue)
		{
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

		public string GetStringForUri(Uri uri) { throw new NotSupportedException(); }

		public static ResourceLoader GetForCurrentView() => _loader;

		public static ResourceLoader GetForCurrentView(string name) => _loader;

		public static ResourceLoader GetForViewIndependentUse() => _loader;

		public static ResourceLoader GetForViewIndependentUse(string name) => _loader;

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

		public static string DefaultLanguage { get; set; }

		internal static void ClearResources()
		{
			_resources.Clear();
		}

		internal static void ProcessResourceFile(string name, Stream input)
		{
			using (var reader = new BinaryReader(input))
			{
				if (!reader.ReadBytes(3).SequenceEqual(new byte[] { 0x75, 0x6E, 0x6F }))
				{
					throw new InvalidCastException($"The file {name} is not a resource file");
				}

				if (reader.ReadInt32() != 1)
				{
					throw new InvalidCastException($"The resource file {name} has an invalid version");
				}

				var culture = reader.ReadString();
				var resourceCount = reader.ReadInt32();

				if (!_resources.TryGetValue(culture, out var resources))
				{
					_resources[culture] = resources = new Dictionary<string, string>();
				}

				for (int i = 0; i < resourceCount; i++)
				{
					var key = reader.ReadString();
					var value = reader.ReadString();

					resources[key] = value;
				}
			}
		}
	}
}
