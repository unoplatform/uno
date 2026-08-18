#if CROSSRUNTIME
using Microsoft.UI.Composition;
using SkiaSharp;
using Uno.UI.Composition.Drawing;
using Windows.Foundation;

namespace Uno.WinUI.Graphics2DSK;

/// <summary>
/// The composition visual backing <see cref="SKCanvasElement"/>. Because <see cref="SKCanvasElement"/> is a
/// <c>Grid</c> — an <c>IBorderInfoProvider</c> — this must be a <see cref="BorderVisual"/> (the framework asserts the
/// visual of a border-info provider is one, and it renders the element's background/border). On top of that it draws
/// the user's SkiaSharp straight into the frame's <see cref="SKCanvas"/> (zero-copy) when the active backend exposes
/// one via <see cref="IDrawingSession.NativeSurface"/>; on any other backend it asks the owner to fall back to a GL
/// island — a child that <c>base.Paint</c> composites.
/// </summary>
internal sealed class SKCanvasVisual(SKCanvasElement owner, Compositor compositor) : BorderVisual(compositor)
{
	internal override bool CanPaint() => true;

	internal override IGeometry? Paint(in PaintingSession session)
	{
		// Background, children (including the GL island fallback once created) and border first...
		var ownPath = base.Paint(in session);

		// ...then the element's own SkiaSharp drawing on top. Save/restore around the callback so the inheritor's
		// RenderOverride can't leak canvas state into the frame, and clip so drawing stays inside the element's area.
		if (session.Session.NativeSurface is SKCanvas canvas)
		{
			canvas.Save();
			canvas.ClipRect(new SKRect(0, 0, Size.X, Size.Y), antialias: true);
			owner.InvokeRenderOverride(canvas, new Size(Size.X, Size.Y));
			canvas.Restore();
		}
		else
		{
			// The active backend exposes no SKCanvas (e.g. WebGPU) — bring up the GL island. It's added as a child
			// and painted by base.Paint on the next frame (EnsureIslandFallback re-invalidates once it's created).
			owner.EnsureIslandFallback();
		}

		return ownPath;
	}

	public void Invalidate() => Compositor.InvalidateRender(this);
}
#endif
