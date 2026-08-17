#if CROSSRUNTIME
using Microsoft.UI.Composition;
using SkiaSharp;
using Uno.UI.Composition.Drawing;
using Windows.Foundation;

namespace Uno.WinUI.Graphics2DSK;

/// <summary>
/// The composition visual backing <see cref="SKCanvasElement"/>. During paint it draws the user's SkiaSharp
/// straight into the frame's <see cref="SKCanvas"/> (zero-copy) when the active backend exposes one via
/// <see cref="IDrawingSession.NativeSurface"/>; on any other backend that surface is null and nothing is drawn.
/// </summary>
internal sealed class SKCanvasVisual(SKCanvasElement owner, Compositor compositor) : ContainerVisual(compositor)
{
	internal override bool CanPaint() => true;

	internal override void Paint(in PaintingSession session)
	{
		if (session.Session.NativeSurface is not SKCanvas canvas)
		{
			return;
		}

		// Save/restore around the callback so the inheritor's RenderOverride can't leak canvas state into the frame.
		canvas.Save();
		// Clip so drawing stays inside the element's area.
		canvas.ClipRect(new SKRect(0, 0, Size.X, Size.Y), antialias: true);
		owner.InvokeRenderOverride(canvas, new Size(Size.X, Size.Y));
		canvas.Restore();
	}

	public void Invalidate() => Compositor.InvalidateRender(this);
}
#endif
