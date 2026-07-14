#nullable enable

using System.Numerics;
using SkiaSharp;
using Uno.UI.Composition;
using Uno.UI.Composition.Drawing;
using Windows.Foundation;

namespace Microsoft.UI.Composition
{
	public partial class CompositionNineGridBrush : CompositionBrush
	{
		private static readonly SKPaint _tempPaint = new();
		private SKBitmap? _bitmap;
		private SKCanvas? _bitmapCanvas;
		private SKRectI _insetRect;

		internal override bool RequiresRepaintOnEveryFrame => Source?.RequiresRepaintOnEveryFrame ?? false;

		internal override bool TryPaint(IDrawingSession session, float opacity, Rect bounds)
		{
			if (Source is null)
			{
				return true;
			}

			SKRect sourceBounds;
			if (Source is ISizedBrush sizedBrush && sizedBrush.Size is Vector2 sourceSize)
			{
				sourceBounds = new(0, 0, sourceSize.X, sourceSize.Y);
			}
			else
			{
				sourceBounds = bounds.ToSKRect();
			}

			var newSize = new SKSizeI((int)sourceBounds.Width, (int)sourceBounds.Height);
			var info = new SKImageInfo(newSize.Width, newSize.Height, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
			if (_bitmap is null || _bitmapCanvas is null || _bitmap.Info.Size != newSize)
			{
				_bitmap?.Dispose();
				_bitmapCanvas?.Dispose();
				_bitmap = new SKBitmap(info);
				_bitmapCanvas = new SKCanvas(_bitmap);
			}
			else
			{
				_bitmapCanvas.Clear(SKColors.Transparent);
			}

			Source.TryPaint(new SkiaDrawingSession(_bitmapCanvas), opacity, sourceBounds.ToRect());
			_bitmapCanvas.Flush();
			var image = SKImage.FromPixels(info, _bitmap.GetPixels());

			_insetRect.Top = (int)(TopInset * TopInsetScale);
			_insetRect.Bottom = (int)(sourceBounds.Height - (BottomInset * BottomInsetScale));
			_insetRect.Right = (int)(sourceBounds.Width - (RightInset * RightInsetScale));
			_insetRect.Left = (int)(LeftInset * LeftInsetScale);

			// DrawImageNinePatch/DrawBitmapNinePatch have no backend-neutral verb; reach the Skia canvas
			// directly (contained escape hatch, like the offscreen bitmap above).
			var canvas = ((SkiaDrawingSession)session).Canvas;
			var skBounds = bounds.ToSKRect();
			_tempPaint.Reset();
			_tempPaint.IsAntialias = true;
			_tempPaint.IsDither = true;
			if (IsCenterHollow)
			{
				canvas.Save();
				canvas.ClipRect(_insetRect, SKClipOperation.Difference, antialias: true);
				canvas.DrawImageNinePatch(image, _insetRect, skBounds, _tempPaint);
				canvas.Restore();
			}
			else
			{
				canvas.DrawBitmapNinePatch(_bitmap, _insetRect, skBounds, _tempPaint);
			}
			return true;
		}

		internal override bool CanPaint() => Source?.CanPaint() ?? false;
	}
}
