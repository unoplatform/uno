using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.Wpf.Hosting;

namespace Uno.UI.Runtime.Skia.Wpf;

internal static class WpfManager
{
	internal static XamlRootMap<IWpfXamlRootHost> XamlRootMap { get; } = new();
}
