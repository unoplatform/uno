using Microsoft.UI.Composition;
using SkiaSharp;
using Uno.UI.Composition;

namespace Microsoft.UI.Xaml.Controls;

public abstract partial class GLCanvasElement
{
	private class GLVisual(GLCanvasElement owner, Compositor compositor) : Visual(compositor)
	{
		private readonly SKPixmap _pixmap = new SKPixmap(new SKImageInfo((int)owner._width, (int)owner._height, SKColorType.Bgra8888), owner._pixels);

		internal override void Paint(in PaintingSession session)
		{
			// we clear the drawing area here because in some cases when unloading the GLCanvasElement, the
			// drawing isn't cleared for some reason (a possible hypothesis is timing problems between raw GL and skia).
			session.Canvas.ClipRect(new SKRect(0, 0, owner.Visual.Size.X, owner.Visual.Size.Y));
			session.Canvas.Clear(SKColors.Transparent);
			session.Canvas.Save();
			owner.Render();
			using var image = SKImage.FromPixels(_pixmap);
			// opengl coordinates go bottom-up, so we concat a matrix to flip horizontally and vertically
			var flip = new SKMatrix(scaleX: -1, scaleY: -1, skewX: 0, skewY: 0, transX: owner.Visual.Size.X, transY: owner.Visual.Size.Y, persp0: 0, persp1: 0, persp2: 1);
			session.Canvas.Concat(ref flip);
			session.Canvas.DrawImage(image, new SKRect(0, 0, owner.Visual.Size.X, owner.Visual.Size.Y));
			session.Canvas.Restore();
		}

		private protected override void DisposeInternal()
		{
			base.DisposeInternal();
			_pixmap.Dispose();
		}
	}
}
