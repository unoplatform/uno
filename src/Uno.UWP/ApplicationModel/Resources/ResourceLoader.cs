using System;
using System.Runtime.InteropServices;
using Uno.UI;
using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Windows.ApplicationModel.Resources
{
	public sealed partial class ResourceLoader
	{
		private static readonly ResourceLoader _loader = new ResourceLoader();

		public ResourceLoader(string name) { }

		public ResourceLoader() { }

		public string GetString(string resource)
		{
			if (GetStringInternal == null)
			{
				throw new InvalidOperationException($"ResourceLoader.GetStringInternal hasn't been set. Make sure ResourceHelper is initialized properly.");
			}

			return GetStringInternal.Invoke(resource);
		}

		public string GetStringForUri(Uri uri) { throw new NotSupportedException(); }

		public static ResourceLoader GetForCurrentView() => _loader;

		public static ResourceLoader GetForCurrentView(string name) => _loader;

		public static ResourceLoader GetForViewIndependentUse() => _loader;

		public static ResourceLoader GetForViewIndependentUse(string name) => _loader;

		public static string GetStringForReference(Uri uri) { throw new NotSupportedException(); }

		// TODO: Remove this property when getting rid of ResourceHelper
		public static Func<string, string> GetStringInternal { get; set; }
	}
}
