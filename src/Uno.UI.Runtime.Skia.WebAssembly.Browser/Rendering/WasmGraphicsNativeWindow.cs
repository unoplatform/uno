#nullable enable

using System;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.Runtime.Skia;

/// <summary>
/// The browser host's GPU-agnostic contribution to graphics init: the HTML &lt;canvas&gt; element id. It names no
/// GPU API — the async WebGPU context factory consumes <see cref="SurfaceId"/> to create the canvas surface.
/// </summary>
internal sealed class WasmGraphicsNativeWindow(string canvasId) : INativeWindow
{
	public NativeWindowKind Kind => NativeWindowKind.Wasm;

	public nint Handle => nint.Zero;

	public nint Display => nint.Zero;

	public int Width => 0;

	public int Height => 0;

	public string? SurfaceId => canvasId;

#pragma warning disable CS0067 // Resize is driven per-frame via AcquireRenderTarget(width, height).
	public event EventHandler? Resized;
#pragma warning restore CS0067
}
