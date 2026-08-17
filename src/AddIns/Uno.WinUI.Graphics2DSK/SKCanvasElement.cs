using System;
using Windows.Foundation;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SkiaSharp;

namespace Uno.WinUI.Graphics2DSK;

/// <summary>
/// A <see cref="FrameworkElement"/> that exposes the ability to draw directly using SkiaSharp.
/// </summary>
/// <remarks>
/// When the active backend renders with SkiaSharp, the drawing goes ZERO-COPY straight into the window's frame
/// <see cref="SKCanvas"/>. On any other backend (e.g. WebGPU) it falls back to a self-contained Skia-on-GL island
/// (its own <see cref="SkiaSharp.GRContext"/> + framebuffer, read back and composited) so it still works, at the
/// cost of a copy. This is only available on skia-based targets.
/// </remarks>
public abstract partial class SKCanvasElement : Grid
{
#if CROSSRUNTIME
	private SKCanvasVisual? _canvasVisual;
	private SkiaGLCanvasElement? _island;
	private bool _islandRequested;

	private protected override ContainerVisual CreateElementVisual()
		=> _canvasVisual = new SKCanvasVisual(this, Compositor.GetSharedCompositor());

	internal override bool IsViewHit() => true;
#endif

	protected SKCanvasElement()
	{
		if (!IsSupportedOnCurrentPlatform())
		{
			throw new PlatformNotSupportedException($"This platform does not support {nameof(SKCanvasElement)}. For more information: https://aka.platform.uno/skcanvaselement");
		}
	}

#if CROSSRUNTIME
	public static bool IsSupportedOnCurrentPlatform() => true;

	// Called from the paint when the active backend exposes no SKCanvas (NativeSurface is null): bring up the
	// GL island once, off the paint, so it composites the drawing on the next frame.
	internal void EnsureIslandFallback()
	{
		if (_islandRequested)
		{
			return;
		}
		_islandRequested = true;

		DispatcherQueue.TryEnqueue(() =>
		{
			if (_island is null)
			{
				_island = new SkiaGLCanvasElement(this);
				Children.Add(_island);
			}
		});
	}
#else
	public static bool IsSupportedOnCurrentPlatform() => false;
#endif

	/// <summary>
	/// Invalidates the element and triggers a redraw.
	/// </summary>
#if CROSSRUNTIME
	public void Invalidate()
	{
		_canvasVisual?.Invalidate();
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
