using System;
using Windows.Foundation;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SkiaSharp;

#if CROSSRUNTIME
using Uno.UI.Graphics;
using Uno.UI.Composition.Drawing;
#endif

namespace Uno.WinUI.Graphics2DSK;

/// <summary>
/// A <see cref="FrameworkElement"/> that exposes the ability to draw directly using SkiaSharp.
/// </summary>
/// <remarks>
/// Zero-copy when the active rendering already uses SkiaSharp: the element draws straight into the window's frame
/// <see cref="SKCanvas"/> through a composition visual. When the active backend is something else (e.g. WebGPU),
/// it falls back to a self-contained Skia-on-GL island (its own <see cref="SkiaSharp.GRContext"/> + framebuffer,
/// read back and composited) so it still works, at the cost of a copy.
/// </remarks>
public abstract partial class SKCanvasElement : Grid
{
#if CROSSRUNTIME
	// Decided once at construction: does the active rendering already use SkiaSharp? If so we draw zero-copy into
	// the frame via a NativeCanvasVisual; otherwise we host a Skia-on-GL island child (copy). The active backend is
	// registered at startup, well before any element is created, so this is stable.
	private readonly bool _direct = NativeCanvasVisual.CanDrawNatively(typeof(SKCanvas));
	private NativeCanvasVisual? _directVisual;
	private SkiaGLCanvasElement? _island;
#endif

	protected SKCanvasElement()
	{
		if (!IsSupportedOnCurrentPlatform())
		{
			throw new PlatformNotSupportedException($"This platform does not support {nameof(SKCanvasElement)}. For more information: https://aka.platform.uno/skcanvaselement");
		}

#if CROSSRUNTIME
		if (!_direct)
		{
			_island = new SkiaGLCanvasElement(this);
			Children.Add(_island);
		}
#endif
	}

#if CROSSRUNTIME
	// Zero-copy path: the element's own visual draws the user's SkiaSharp straight into the frame's SKCanvas.
	private protected override ContainerVisual CreateElementVisual()
		=> _direct
			? _directVisual = new NativeCanvasVisual(Compositor.GetSharedCompositor(), OnDirectPaint)
			: base.CreateElementVisual();

	private void OnDirectPaint(IDrawingSession session, Size size)
	{
		if (session.NativeSurface is SKCanvas canvas)
		{
			RenderOverride(canvas, size);
		}
	}

	internal override bool IsViewHit() => _direct || base.IsViewHit();

	public static bool IsSupportedOnCurrentPlatform() => true;
#else
	public static bool IsSupportedOnCurrentPlatform() => false;
#endif

	/// <summary>
	/// Invalidates the element and triggers a redraw.
	/// </summary>
#if CROSSRUNTIME
	public void Invalidate()
	{
		_directVisual?.Invalidate();
		_island?.Invalidate();
	}

	internal void InvokeRenderOverride(SKCanvas canvas, Size area) => RenderOverride(canvas, area);
#else
#pragma warning disable CS0109 // Member does not hide an inherited member; new keyword is not required
	public new void Invalidate() { }
#pragma warning restore CS0109 // Member does not hide an inherited member; new keyword is not required
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
