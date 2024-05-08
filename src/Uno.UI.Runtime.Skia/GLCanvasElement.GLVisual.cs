using Microsoft.UI.Composition;
using SkiaSharp;
using Uno.UI.Composition;

namespace Microsoft.UI.Xaml.Controls;

public abstract partial class GLCanvasElement
{
	private class GLVisual(GLCanvasElement owner, Compositor compositor) : Visual(compositor)
	{
		private readonly SKPixmap _pixmap = new SKPixmap(new SKImageInfo((int)owner._width, (int)owner._height, SKColorType.Bgra8888), owner._pixels);

		internal override void Draw(in DrawingSession session)
		{
			// we clear the drawing area here because in some cases when unloading the GLCanvasElement, the
			// drawing isn't cleared for some reason (a possible hypothesis is timing problems between raw GL and skia).
			session.Canvas.ClipRect(new SKRect(0, 0, owner.Visual.Size.X, owner.Visual.Size.Y));
			session.Canvas.Clear(SKColors.Transparent);
			owner.Render();
			session.Canvas.DrawImage(SKImage.FromPixels(_pixmap), new SKRect(0, 0, owner.Visual.Size.X, owner.Visual.Size.Y));
		}

		private protected override void DisposeInternal()
		{
			base.DisposeInternal();
			_pixmap.Dispose();
		}
	}
}
