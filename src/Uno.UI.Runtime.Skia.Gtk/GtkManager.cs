using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.Gtk.Hosting;

namespace Uno.UI.Runtime.Skia.Gtk;

internal static class GtkManager
{
	internal static XamlRootMap<IGtkXamlRootHost> XamlRootMap { get; } = new();
}
