#nullable enable

using System;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.Runtime.Skia.AppleUIKit;

/// <summary>
/// The AppleUIKit host's GPU-agnostic contribution to graphics init: the view's <c>CAMetalLayer</c> and DPI scale.
/// It names no GPU API — a per-kind provider (e.g. WebGPU) consumes it to create a surface for the backend.
/// </summary>
internal sealed class AppleMetalGraphicsNativeWindow(nint metalLayer, float rasterizationScale) : INativeWindow
{
	public NativeWindowKind Kind => NativeWindowKind.Metal;

	public nint Handle => metalLayer;

	public nint Display => nint.Zero;

	public int Width => 0;

	public int Height => 0;

	public float RasterizationScale => rasterizationScale;

#pragma warning disable CS0067 // Resize is driven per-frame via AcquireRenderTarget(width, height).
	public event EventHandler? Resized;
#pragma warning restore CS0067
}
