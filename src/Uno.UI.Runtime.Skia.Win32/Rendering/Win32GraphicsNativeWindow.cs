#nullable enable

using System;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.Runtime.Skia.Win32;

/// <summary>
/// The Win32 host's GPU-agnostic contribution to graphics init: the HWND + HINSTANCE and size/scale. It names
/// no GPU API — a per-kind provider (e.g. WebGPU) consumes it to create a context/surface for the backend.
/// </summary>
internal sealed class Win32GraphicsNativeWindow(nint hwnd, nint hinstance, int width, int height, float rasterizationScale) : INativeWindow
{
	public NativeWindowKind Kind => NativeWindowKind.Win32;

	public nint Handle => hwnd;

	public nint Display => hinstance;

	public int Width => width;

	public int Height => height;

	public float RasterizationScale => rasterizationScale;

#pragma warning disable CS0067 // Resize is driven per-frame via AcquireRenderTarget(width, height).
	public event EventHandler? Resized;
#pragma warning restore CS0067
}
