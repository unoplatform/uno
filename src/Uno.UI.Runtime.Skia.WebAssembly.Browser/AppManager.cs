using Uno.UI.Hosting;
using Microsoft.UI.Xaml;

namespace Uno.UI.Runtime.Skia.WebAssembly.Browser;

internal static class AppManager
{
	internal static XamlRootMap<IXamlRootHost> XamlRootMap { get; } = new();
}
