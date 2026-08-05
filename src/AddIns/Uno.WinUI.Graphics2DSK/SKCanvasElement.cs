using System;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SkiaSharp;

namespace Uno.WinUI.Graphics2DSK;

/// <summary>
/// A <see cref="FrameworkElement"/> that exposes the ability to draw directly using SkiaSharp.
/// </summary>
/// <remarks>
/// This is only available on skia-based targets. Drawing is done into a dedicated GL framebuffer through
/// its own <see cref="SkiaSharp.GRContext"/>, so it is independent of the app's active render backend.
/// </remarks>
public abstract partial class SKCanvasElement : Grid
{
#if CROSSRUNTIME
	private readonly SkiaGLCanvasElement _canvas;
#endif

	protected SKCanvasElement()
	{
		if (!IsSupportedOnCurrentPlatform())
		{
			throw new PlatformNotSupportedException($"This platform does not support {nameof(SKCanvasElement)}. For more information: https://aka.platform.uno/skcanvaselement");
		}

#if CROSSRUNTIME
		_canvas = new SkiaGLCanvasElement(this);
		Children.Add(_canvas);
#endif
	}

#if CROSSRUNTIME
	public static bool IsSupportedOnCurrentPlatform() => true;
#else
	public static bool IsSupportedOnCurrentPlatform() => false;
#endif

	/// <summary>
	/// Invalidates the element and triggers a redraw.
	/// </summary>
#if CROSSRUNTIME
	public void Invalidate() => _canvas.Invalidate();
#else
#pragma warning disable CS0109 // Member does not hide an inherited member; new keyword is not required
	public new void Invalidate() { }
#pragma warning restore CS0109 // Member does not hide an inherited member; new keyword is not required
#endif

#if CROSSRUNTIME
	internal void InvokeRenderOverride(SKCanvas canvas, Size area) => RenderOverride(canvas, area);
#endif

	/// <summary>
	/// The SkiaSharp drawing logic goes here.
	/// </summary>
	/// <param name="canvas">The SKCanvas that should be drawn on.</param>
	/// <param name="area">The dimensions of the clipping area.</param>
	/// <remarks>
	/// When called, the <paramref name="canvas"/> is already set up such that the origin (0,0) is at the top-left of the clipping area.
	/// Drawing outside this area (i.e. outside the (0, 0, area.Width, area.Height) rectangle) will be clipped out.
	/// </remarks>
	protected abstract void RenderOverride(SKCanvas canvas, Size area);
}
