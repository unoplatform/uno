#nullable enable

using System;
using System.IO;
using System.Linq;
using Gtk;

namespace Uno.UI.Runtime.Skia.GTK.Extensions.Helpers
{
	public static class CssProviderExtensions
	{
		/// <summary>
		/// Provides a workaround for https://github.com/GtkSharp/GtkSharp/issues/212.
		/// </summary>
		/// <param name="cssProvider">CSS provider.</param>
		/// <param name="embeddedResourceName">Embedded resource name.</param>
		/// <returns>Task.</returns>
		internal static void LoadFromEmbeddedResource(this CssProvider cssProvider, string embeddedResourceName)
		{
			var gtkHostAssembly = typeof(GtkHost).Assembly;
			var names = gtkHostAssembly.GetManifestResourceNames();
			var resource = names.FirstOrDefault(name => name.EndsWith(embeddedResourceName));
			if (resource == null)
			{
				throw new InvalidOperationException($"There is no resource ending with {embeddedResourceName}");
			}

			using var stream = typeof(GtkHost).Assembly.GetManifestResourceStream(resource);
			if (stream == null)
			{
				throw new InvalidOperationException($"Embedded resource {resource} could not be opened.");
			}

			using var streamReader = new StreamReader(stream);
			var css = streamReader.ReadToEnd();
			cssProvider.LoadFromData(css);
		}
	}
}
