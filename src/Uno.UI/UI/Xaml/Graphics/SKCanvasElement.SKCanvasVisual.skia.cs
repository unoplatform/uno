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
			session.Canvas.Save();
			// clipping here guards against a naked canvas.Clear() call which would wipe out the entire window.
			session.Canvas.ClipRect(new SKRect(0, 0, Size.X, Size.Y));
			owner.RenderOverride(session.Canvas, Size.ToSize());
			session.Canvas.Restore();
		}
	}
}
