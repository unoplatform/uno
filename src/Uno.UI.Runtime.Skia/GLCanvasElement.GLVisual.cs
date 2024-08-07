using Microsoft.UI.Composition;
using SkiaSharp;

namespace Microsoft.UI.Xaml.Controls;

public abstract partial class GLCanvasElement
{
	private class GLVisual : Visual
	{
		private SKPixmap? _pixmap;
		private readonly GLCanvasElement _owner;

		public GLVisual(GLCanvasElement owner, Compositor compositor) : base(compositor)
		{
			_owner = owner;
			_owner.Loaded += OnOwnerLoaded;
		}

		private void OnOwnerLoaded(object sender, RoutedEventArgs e)
		{
			_pixmap?.Dispose();
			_pixmap = new SKPixmap(new SKImageInfo((int)_owner._width, (int)_owner._height, SKColorType.Bgra8888, SKAlphaType.Unpremul), _owner._pixels);
		}

		internal override void Paint(in PaintingSession session)
		{
			// we clear the drawing area here because in some cases when unloading the GLCanvasElement, the
			// drawing isn't cleared for some reason (a possible hypothesis is timing problems between raw GL and skia).
			session.Canvas.ClipRect(new SKRect(0, 0, _owner.Visual.Size.X, _owner.Visual.Size.Y));
			session.Canvas.Clear(SKColors.Transparent);
			session.Canvas.Save();
			if (_owner._renderDirty)
			{
				_owner.Render();
			}
			// opengl coordinates go bottom-up, so we concat a matrix to flip horizontally and vertically
			var flip = new SKMatrix(scaleX: -1, scaleY: -1, skewX: 0, skewY: 0, transX: _owner.Visual.Size.X, transY: _owner.Visual.Size.Y, persp0: 0, persp1: 0, persp2: 1);
			session.Canvas.Concat(ref flip);
			session.Canvas.DrawImage(SKImage.FromPixels(_pixmap), new SKRect(0, 0, _owner.Visual.Size.X, _owner.Visual.Size.Y));
			session.Canvas.Restore();
		}

		private protected override void DisposeInternal()
		{
			base.DisposeInternal();
			_pixmap?.Dispose();
		}
	}
}
