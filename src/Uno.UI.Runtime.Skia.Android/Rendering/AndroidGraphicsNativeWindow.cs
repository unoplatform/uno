#nullable enable

using System;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.Runtime.Skia.Android;

/// <summary>
/// The Android host's GPU-agnostic contribution to graphics init: the <c>ANativeWindow</c> and size. It names no
/// GPU API — a per-kind provider (e.g. WebGPU) consumes it to create a surface for the backend.
/// </summary>
internal sealed class AndroidGraphicsNativeWindow(nint aNativeWindow, int width, int height) : INativeWindow
{
	public NativeWindowKind Kind => NativeWindowKind.Android;

	public nint Handle => aNativeWindow;

	public nint Display => nint.Zero;

	public int Width => width;

	public int Height => height;

#pragma warning disable CS0067 // Resize is driven per-frame via AcquireRenderTarget(width, height).
	public event EventHandler? Resized;
#pragma warning restore CS0067
}
