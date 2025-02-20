using Uno.UI.Hosting;

namespace Uno.WinUI.Runtime.Skia.X11;

internal static class X11Manager
{
	internal static XamlRootMap<IXamlRootHost> XamlRootMap { get; } = new();
}
