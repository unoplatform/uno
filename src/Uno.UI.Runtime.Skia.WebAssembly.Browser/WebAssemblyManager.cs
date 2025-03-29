#nullable enable

using Uno.UI.Hosting;

namespace Uno.UI.Runtime.Skia;

internal static class WebAssemblyManager
{
	internal static XamlRootMap<IXamlRootHost> XamlRootMap { get; } = new();
}
