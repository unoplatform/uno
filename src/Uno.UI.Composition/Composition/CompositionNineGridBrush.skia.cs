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
		private SKPaint? _filterPaint = new SKPaint() { FilterQuality = SKFilterQuality.High, IsAntialias = true, IsAutohinted = true, IsDither = true };
		private SKRectI _insetRect;

		bool IOnlineBrush.IsOnline => true; // TODO: `Source is IOnlineBrush onlineBrush && onlineBrush.IsOnline` 

		internal override void UpdatePaint(SKPaint paint, SKRect bounds)
		{
			Source?.UpdatePaint(_sourcePaint, bounds);

			// TODO: Implement offline rendering
		}

		void IOnlineBrush.Draw(in DrawingSession session, SKRect bounds)
		{
			SKRect sourceBounds;
			if (Source is ISizedBrush sizedBrush && sizedBrush.IsSized && sizedBrush.Size is Vector2 sourceSize)
				sourceBounds = new(0, 0, sourceSize.X, sourceSize.Y);
			else
				sourceBounds = bounds;

			if ((Source is IOnlineBrush onlineBrush && onlineBrush.IsOnline) || _sourcePaint.Shader is null || _sourceImage is null)
			{
				Source?.UpdatePaint(_sourcePaint, sourceBounds);

				if (_surface is null)
					_surface = SKSurface.Create(new SKImageInfo((int)sourceBounds.Width, (int)sourceBounds.Height, SKImageInfo.PlatformColorType, SKAlphaType.Premul));

				if (_surface is not null)
				{
					_surface.Canvas.DrawRect(sourceBounds, _sourcePaint);
					_surface.Canvas.Flush();
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
					session.Surface?.Canvas.ClipRect(_insetRect, SKClipOperation.Difference, true);

				session.Surface?.Canvas.DrawImageNinePatch(_sourceImage, _insetRect, bounds, _filterPaint);
			}
		}
	}
}
