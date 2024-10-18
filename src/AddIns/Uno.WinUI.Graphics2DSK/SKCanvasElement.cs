using System.Numerics;
using Windows.Foundation;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using SkiaSharp;

namespace Uno.WinUI.Graphics2DSK;

/// <summary>
/// A <see cref="FrameworkElement"/> that exposes the ability to draw directly on the window using SkiaSharp.
/// </summary>
/// <remarks>This is only available on skia-based targets.</remarks>
public abstract class SKCanvasElement : FrameworkElement
{
	private protected override ShapeVisual CreateElementVisual() => new SKCanvasVisual(this, Compositor.GetSharedCompositor());

	/// <summary>
	/// Queue a rendering cycle that will call <see cref="RenderOverride"/>.
	/// </summary>
	public void Invalidate() => _visual.Compositor.InvalidateRender(_visual);

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

	private class SKCanvasVisual(SKCanvasElement owner, Compositor compositor) : ShapeVisual(compositor)
	{
		internal override void Paint(in PaintingSession session)
		{
			// We save and restore the canvas state ourselves so that the inheritor doesn't accidentally forget to.
			session.Canvas.Save();
			// clipping here guarantees that drawing doesn't get outside the intended area
			session.Canvas.ClipRect(new SKRect(0, 0, Size.X, Size.Y));
			owner.RenderOverride(session.Canvas, Size.ToSize());
			session.Canvas.Restore();
		}
	}
}
