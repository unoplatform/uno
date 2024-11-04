using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.Win32.Hosting;

namespace Uno.UI.Runtime.Skia.Win32;

internal static class WpfManager
{
	internal static XamlRootMap<IWpfXamlRootHost> XamlRootMap { get; } = new();
}
