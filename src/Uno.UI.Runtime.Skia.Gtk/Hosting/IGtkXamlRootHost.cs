#nullable enable

using System.Threading.Tasks;
using Gtk;
using Uno.UI.Hosting;

namespace Uno.UI.Runtime.Skia.Gtk.Hosting;

internal interface IGtkXamlRootHost : IXamlRootHost
{
	Fixed? NativeOverlayLayer { get; }

	RenderSurfaceType? RenderSurfaceType { get; }

	Container RootContainer { get; }

	UnoEventBox EventBox { get; }

	Task InitializeAsync();
}
