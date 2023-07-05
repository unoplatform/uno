#nullable disable

using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.GTK.Hosting;

namespace Uno.UI.Runtime.Skia.GTK;

internal static class GtkManager
{
	internal static XamlRootMap<IGtkXamlRootHost> XamlRootMap { get; } = new();
}
