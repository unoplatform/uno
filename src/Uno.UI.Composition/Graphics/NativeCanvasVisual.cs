#nullable enable

using System;
using Windows.Foundation;
using Microsoft.UI.Composition;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.Graphics;

/// <summary>
/// A composition visual that, during its paint, invokes a callback with the frame's neutral
/// <see cref="IDrawingSession"/> (already clipped to the visual's size). This is the framework hook a
/// <c>&lt;Api&gt;CanvasElement</c> uses to draw straight into the frame: the callback reads
/// <see cref="IDrawingSession.NativeSurface"/> and, when it matches its graphics API, draws ZERO-COPY into the
/// window's surface. Backend-neutral — it names no graphics library, exposes no render-loop internal
/// (<c>PaintingSession</c>/<c>Visual.Paint</c> stay internal), and requires no InternalsVisibleTo.
/// </summary>
public sealed class NativeCanvasVisual : ContainerVisual
{
	private readonly Action<IDrawingSession, Size> _render;

	/// <param name="render">Invoked each paint with the frame's neutral drawing session and the visual's size
	/// (origin already at the visual's top-left, clipped to its bounds).</param>
	public NativeCanvasVisual(Compositor compositor, Action<IDrawingSession, Size> render) : base(compositor)
		=> _render = render;

	/// <summary>
	/// Whether the active rendering already uses the graphics API whose native surface type is
	/// <paramref name="nativeSurfaceType"/> — i.e. whether a <c>&lt;Api&gt;CanvasElement</c> can draw ZERO-COPY into
	/// the frame (via <see cref="IDrawingSession.NativeSurface"/> in the paint callback) rather than through its own
	/// offscreen island. A load-time check (no paint needed); false when no backend is negotiated yet.
	/// </summary>
	public static bool CanDrawNatively(Type nativeSurfaceType)
		=> DrawingFactory.CurrentOrNull?.NativeSurfaceType == nativeSurfaceType;

	/// <summary>Requests a repaint of this visual.</summary>
	public void Invalidate() => Compositor.InvalidateRender(this);

	internal override bool CanPaint() => true;

	internal override void Paint(in PaintingSession session)
	{
		var count = session.Session.Save();
		session.Session.ClipRect(new Rect(0, 0, Size.X, Size.Y), antialias: true);
		_render(session.Session, new Size(Size.X, Size.Y));
		session.Session.RestoreToCount(count);
	}
}
