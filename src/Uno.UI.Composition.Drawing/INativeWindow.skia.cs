#nullable enable

using System;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// The host's contribution to graphics init: a tagged native output handle plus size/resize. This is the
/// only thing that is both platform-specific and GPU-agnostic, so it is <em>all</em> the host provides —
/// the host references no GPU API and no backend. A per-kind context provider consumes it to create a
/// context/surface for the backend.
/// </summary>
public interface INativeWindow
{
	NativeWindowKind Kind { get; }

	/// <summary>The primary native handle (X11 <c>Window</c>, Win32 <c>HWND</c>, <c>ANativeWindow</c>, <c>CAMetalLayer</c>, …).</summary>
	nint Handle { get; }

	/// <summary>The secondary handle where the windowing system needs one (X11 <c>Display</c>, Win32 <c>HINSTANCE</c>); otherwise <see cref="nint.Zero"/>.</summary>
	nint Display { get; }

	int Width { get; }

	int Height { get; }

	/// <summary>The display (DPI) scale of the window; a provider that sizes its swapchain/targets in physical pixels uses it. Defaults to 1.</summary>
	float RasterizationScale => 1f;

	/// <summary>Raised when the window is resized; providers reconfigure their swapchain in response.</summary>
	event EventHandler? Resized;
}
