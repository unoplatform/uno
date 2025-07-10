#nullable enable

using System.Numerics;
using SkiaSharp;
using Uno.UI.Composition;

namespace Microsoft.UI.Composition
{
	public partial class CompositionNineGridBrush : CompositionBrush
	{
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
			if (_bitmap is null || _bitmapCanvas is null || _bitmap.Info.Size != newSize)
			{
				_bitmap?.Dispose();
				_bitmapCanvas?.Dispose();
				_bitmap = new SKBitmap(new SKImageInfo(newSize.Width, newSize.Height, SKImageInfo.PlatformColorType, SKAlphaType.Premul));
				_bitmapCanvas = new SKCanvas(_bitmap);
			}
			else
			{
				_bitmapCanvas.Clear(SKColors.Transparent);
			}

			Source.Paint(_bitmapCanvas, opacity, sourceBounds);
			_bitmapCanvas.Flush();

			_insetRect.Top = (int)(TopInset * TopInsetScale);
			_insetRect.Bottom = (int)(sourceBounds.Height - (BottomInset * BottomInsetScale));
			_insetRect.Right = (int)(sourceBounds.Width - (RightInset * RightInsetScale));
			_insetRect.Left = (int)(LeftInset * LeftInsetScale);

			if (IsCenterHollow)
			{
				canvas.Save();
				canvas.ClipRect(_insetRect, SKClipOperation.Difference, true);
				canvas.DrawBitmapNinePatch(_bitmap, _insetRect, bounds);
				canvas.Restore();
			}
			else
			{
				canvas.DrawBitmapNinePatch(_bitmap, _insetRect, bounds);
			}
		}

		internal override bool CanPaint() => Source?.CanPaint() ?? false;
	}
}
