#nullable enable

using System.Numerics;
using SkiaSharp;
using Uno.UI.Composition;

namespace Microsoft.UI.Composition
{
	public partial class CompositionNineGridBrush : CompositionBrush
	{
		private static readonly SKPaint _tempPaint = new();
		private SKBitmap? _bitmap;
		private SKCanvas? _bitmapCanvas;
		private SKRectI _insetRect;

		internal override bool RequiresRepaintOnEveryFrame => Source?.RequiresRepaintOnEveryFrame ?? false;

		internal override void Paint(SKCanvas canvas, float opacity, SKRect bounds)
		{
			if (Source is null)
			{
				return;
			}

			SKRect sourceBounds;
			if (Source is ISizedBrush sizedBrush && sizedBrush.Size is Vector2 sourceSize)
			{
				sourceBounds = new(0, 0, sourceSize.X, sourceSize.Y);
			}
			else
			{
				sourceBounds = bounds;
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

			Source.Paint(_bitmapCanvas, opacity, sourceBounds);
			_bitmapCanvas.Flush();
			var image = SKImage.FromPixels(info, _bitmap.GetPixels());

			_insetRect.Top = (int)(TopInset * TopInsetScale);
			_insetRect.Bottom = (int)(sourceBounds.Height - (BottomInset * BottomInsetScale));
			_insetRect.Right = (int)(sourceBounds.Width - (RightInset * RightInsetScale));
			_insetRect.Left = (int)(LeftInset * LeftInsetScale);

			_tempPaint.Reset();
			_tempPaint.IsAntialias = true;
			_tempPaint.IsDither = true;
			if (IsCenterHollow)
			{
				canvas.Save();
				canvas.ClipRect(_insetRect, SKClipOperation.Difference, antialias: true);
				canvas.DrawImageNinePatch(image, _insetRect, bounds, _tempPaint);
				canvas.Restore();
			}
			else
			{
				canvas.DrawBitmapNinePatch(_bitmap, _insetRect, bounds, _tempPaint);
			}
		}

		internal override bool CanPaint() => Source?.CanPaint() ?? false;
	}
}
