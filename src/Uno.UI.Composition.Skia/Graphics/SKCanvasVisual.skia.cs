using System;
using System.Numerics;
using Windows.Foundation;
using Microsoft.UI.Composition;
using SkiaSharp;

namespace Uno.UI.Graphics;

internal class SKCanvasVisual(Action<object, Size> renderCallback, Compositor compositor) : SKCanvasVisualBase(renderCallback, compositor)
{
	internal override void Paint(in PaintingSession session)
	{
		// This visual hands raw SkiaSharp to user code (RenderCallback), so it reaches the canvas directly.
		var canvas = ((Uno.UI.Composition.Drawing.SkiaDrawingSession)session.Session).Canvas;
		// We save and restore the canvas state ourselves so that the inheritor doesn't accidentally forget to.
		canvas.Save();
		// clipping here guarantees that drawing doesn't get outside the intended area
		canvas.ClipRect(new SKRect(0, 0, Size.X, Size.Y), antialias: true);
		RenderCallback(canvas, Size.ToSize());
		canvas.Restore();
	}

	internal override bool CanPaint() => true;
	public override void Invalidate() => Compositor.InvalidateRender(this);
}
