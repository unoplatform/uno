#nullable enable

using Microsoft.UI.Xaml;

namespace Uno.UI.Hosting;

internal interface IXamlRootHost
{
	UIElement? RootElement { get; }

	void InvalidateRender();

	/// <summary>
	/// Resigns native first responder
	/// </summary>
	void ResignNativeFocus() { }

	/// <summary>
	/// When true, CompositionTarget throttles FrameTick scheduling until the host
	/// signals that the previous frame has been presented (via OnFramePresented).
	/// Only Win32 returns true (it has a dedicated render thread for presentation).
	/// </summary>
	bool SupportsRenderThrottle => false;

	/// <summary>
	/// Returns the <see cref="System.Diagnostics.Stopwatch"/>-compatible timestamp of the
	/// most recent VSync that triggered the current frame callback, or 0 if unavailable.
	/// Used by <see cref="Microsoft.UI.Xaml.Media.CompositionTarget"/> to provide accurate
	/// animation timing even when the frame tick is delayed by GC or heavy layout.
	/// On Android, this is the <c>frameTimeNanos</c> from Choreographer which shares the
	/// same CLOCK_MONOTONIC source as <see cref="System.Diagnostics.Stopwatch.GetTimestamp"/>.
	/// </summary>
	long FrameVsyncTimestamp => 0;
}
