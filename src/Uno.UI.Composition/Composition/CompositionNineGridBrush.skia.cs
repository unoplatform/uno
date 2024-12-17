#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;
using Uno.UI.Composition;

namespace Windows.UI.Composition
{
	public partial class CompositionNineGridBrush : CompositionBrush, IOnlineBrush
	{
		private SKPaint _sourcePaint = new SKPaint() { IsAntialias = true };
		private SKImage? _sourceImage;
		private SKSurface? _surface;
		private SKSurface? _offlineSurface;
		private SKPaint? _filterPaint = new SKPaint() { FilterQuality = SKFilterQuality.High, IsAntialias = true, IsAutohinted = true, IsDither = true };
		private SKRectI _insetRect;

		bool IOnlineBrush.IsOnline => true; // TODO: `Source is IOnlineBrush onlineBrush && onlineBrush.IsOnline`, Implement this after offline rendering is properly implemented

		internal override void UpdatePaint(SKPaint paint, SKRect bounds)
		{
			// TODO: Properly implement offline rendering, this is a temporary workaround

			SKRect sourceBounds;
			if (Source is ISizedBrush sizedBrush && sizedBrush.IsSized && sizedBrush.Size is Vector2 sourceSize)
			{
				sourceBounds = new(0, 0, sourceSize.X, sourceSize.Y);
			}
			else
			{
				sourceBounds = bounds;
			}

			if ((Source is IOnlineBrush onlineBrush && onlineBrush.IsOnline) || _sourcePaint.Shader is null || _sourceImage is null || _offlineSurface is null || _offlineSurface.Canvas.DeviceClipBounds != bounds)
			{
				Source?.UpdatePaint(_sourcePaint, sourceBounds);

				if (_offlineSurface is null || _offlineSurface.Canvas.DeviceClipBounds != sourceBounds)
				{
					_offlineSurface?.Dispose();
					_offlineSurface = SKSurface.Create(new SKImageInfo((int)bounds.Width, (int)bounds.Height, SKImageInfo.PlatformColorType, SKAlphaType.Premul));
				}

				if (_offlineSurface is not null)
				{
					_offlineSurface.Canvas.Clear();
					_offlineSurface.Canvas.DrawRect(sourceBounds, _sourcePaint);
					_offlineSurface.Canvas.Flush();

					if (_sourceImage is null || _sourceImage.Info.Rect != sourceBounds)
					{
						_sourceImage?.Dispose();
						_sourceImage = SKImage.Create(new SKImageInfo((int)sourceBounds.Width, (int)sourceBounds.Height));
					}

					var img = _offlineSurface.Snapshot();
					img?.ReadPixels(_sourceImage.PeekPixels());
					img?.Dispose();

					_offlineSurface.Canvas.Clear();
				}
			}

			if (_sourceImage is not null)
			{
				_insetRect.Top = (int)(TopInset * TopInsetScale);
				_insetRect.Bottom = (int)(sourceBounds.Height - (BottomInset * BottomInsetScale));
				_insetRect.Right = (int)(sourceBounds.Width - (RightInset * RightInsetScale));
				_insetRect.Left = (int)(LeftInset * LeftInsetScale);

				if (IsCenterHollow)
				{
					_offlineSurface?.Canvas.ClipRect(_insetRect, SKClipOperation.Difference, true);
				}

				_offlineSurface?.Canvas.DrawImageNinePatch(_sourceImage, _insetRect, bounds, _filterPaint);

				paint.FilterQuality = SKFilterQuality.High;
				paint.IsAntialias = true;
				paint.IsAutohinted = true;
				paint.IsDither = true;

				paint.Shader = _offlineSurface?.Snapshot().ToShader();
			}
		}

		void IOnlineBrush.Paint(in Visual.PaintingSession session, SKRect bounds)
		{
			SKRect sourceBounds;
			if (Source is ISizedBrush sizedBrush && sizedBrush.IsSized && sizedBrush.Size is Vector2 sourceSize)
			{
				sourceBounds = new(0, 0, sourceSize.X, sourceSize.Y);
			}
			else
			{
				sourceBounds = bounds;
			}

			if ((Source is IOnlineBrush onlineBrush && onlineBrush.IsOnline) || _sourcePaint.Shader is null || _sourceImage is null || _surface is null || _surface.Canvas.DeviceClipBounds != sourceBounds)
			{
				Source?.UpdatePaint(_sourcePaint, sourceBounds);

				if (_surface is null || _surface.Canvas.DeviceClipBounds != sourceBounds)
				{
					_surface?.Dispose();
					_surface = SKSurface.Create(new SKImageInfo((int)sourceBounds.Width, (int)sourceBounds.Height, SKImageInfo.PlatformColorType, SKAlphaType.Premul));
				}

				if (_surface is not null)
				{
					var canvas = _surface.Canvas;
					canvas.Clear();
					canvas.DrawRect(sourceBounds, _sourcePaint);
					canvas.Flush();
					_sourceImage?.Dispose();
					_sourceImage = _surface.Snapshot();
				}
			}

			if (_sourceImage is not null)
			{
				_insetRect.Top = (int)(TopInset * TopInsetScale);
				_insetRect.Bottom = (int)(sourceBounds.Height - (BottomInset * BottomInsetScale));
				_insetRect.Right = (int)(sourceBounds.Width - (RightInset * RightInsetScale));
				_insetRect.Left = (int)(LeftInset * LeftInsetScale);

				if (IsCenterHollow)
				{
					session.Canvas?.ClipRect(_insetRect, SKClipOperation.Difference, true);
				}

				session.Canvas?.DrawImageNinePatch(_sourceImage, _insetRect, bounds, _filterPaint);
			}
		}

		private protected override void DisposeInternal()
		{
			base.Dispose();

			_sourcePaint?.Dispose();
			_sourceImage?.Dispose();
			_surface?.Dispose();
			_filterPaint?.Dispose();
		}
	}
}
