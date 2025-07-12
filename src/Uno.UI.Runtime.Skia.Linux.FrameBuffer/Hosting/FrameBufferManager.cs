using Uno.UI.Hosting;
using Microsoft.UI.Xaml;

namespace Uno.WinUI.Runtime.Skia.Linux.FrameBuffer;

internal static class FrameBufferManager
{
	internal static XamlRootMap<IXamlRootHost> XamlRootMap { get; } = new();
}
