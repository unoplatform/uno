#nullable enable

using Gtk;
using Uno.UI.Xaml.Hosting;

namespace Uno.UI.Runtime.Skia.GTK.Hosting;

internal interface IGtkXamlRootHost : IXamlRootHost
{
	Fixed? NativeOverlayLayer { get; }
}
