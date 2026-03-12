#nullable enable

using Microsoft.UI.Xaml;

namespace Uno.UI.Hosting;

internal interface IXamlRootHost
{
	UIElement? RootElement { get; }

	void InvalidateRender();

	/// <summary>
	/// When true, CompositionTarget throttles FrameTick scheduling until the host
	/// signals that the previous frame has been presented (via OnFramePresented).
	/// Only Win32 returns true (it has a dedicated render thread for presentation).
	/// </summary>
	bool SupportsRenderThrottle => false;
}
