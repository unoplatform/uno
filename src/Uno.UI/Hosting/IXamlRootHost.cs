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
	/// True if the host has its own present-completion signal (e.g. a dedicated render
	/// thread that posts back to the UI thread after SwapBuffers/BitBlt). Such hosts call
	/// CompositionTarget.OnFramePresented themselves at the right moment.
	///
	/// When false (the default), CompositionTarget auto-calls OnFramePresented from inside
	/// OnNativePlatformFrameRequested after Draw — that's the platform's vsync callback
	/// (Choreographer on Android, requestAnimationFrame on WASM, etc.) and is the natural
	/// "previous frame is on its way to the display" moment for hosts that don't need
	/// post-present accuracy.
	///
	/// Either way, <c>_waitingForPresent</c> paces render-side work while the previous
	/// frame is still in flight: frame ticks may still be enqueued (so layout and Loaded
	/// events keep flowing), but the Rendering event and Render call inside FrameTick
	/// are gated until the prior frame is consumed by the render path. That gate is
	/// cleared by <c>OnFrameConsumed()</c> at <c>Draw</c> entry; <c>OnFramePresented</c>
	/// is only used for post-present bookkeeping (FrameRendered/FPS).
	///
	/// Currently only Win32 returns true (its render thread is the present signal).
	/// </summary>
	bool SupportsRenderThrottle => false;
}
