#nullable enable

using System;
using Uno.UI.Composition.Drawing;

namespace Uno.WinUI.Runtime.Skia.X11;

/// <summary>
/// The X11 host's GPU-agnostic contribution to graphics init: the window/display handles and size. It names
/// no GPU API — Uno's context factory consumes this to create a context for the negotiated backend.
/// </summary>
internal sealed class X11GraphicsNativeWindow(X11Window window, int width, int height) : INativeWindow
{
	public NativeWindowKind Kind => NativeWindowKind.X11;

	public nint Handle => window.Window;

	public nint Display => window.Display;

	public int Width { get; } = width;

	public int Height { get; } = height;

#pragma warning disable CS0067 // Resize is driven per-frame via AcquireRenderTarget(width, height) for the software path.
	public event EventHandler? Resized;
#pragma warning restore CS0067
}
