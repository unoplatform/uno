using System.Numerics;
using Microsoft.UI.Composition;
using SkiaSharp;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// A <see cref="FrameworkElement"/> that exposes the ability to draw directly on the window using SkiaSharp.
/// </summary>
/// <remarks>This is only available on skia-based targets.</remarks>
public abstract partial class SKCanvasElement
{
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
