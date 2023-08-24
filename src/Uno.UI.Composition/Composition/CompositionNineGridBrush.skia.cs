#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
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
		//private SKPaint? _hollowPaint; // TODO: Implement IsCenterHollow
		private SKRectI _insetRect;

		bool IOnlineBrush.IsOnline => true; // TODO: `Source is IOnlineBrush onlineBrush && onlineBrush.IsOnline` 

		internal override void UpdatePaint(SKPaint paint, SKRect bounds)
		{
			Source?.UpdatePaint(_sourcePaint, bounds);

			// TODO: Implement offline rendering
		}

		void IOnlineBrush.Draw(in DrawingSession session, SKRect bounds)
		{
			if ((Source is IOnlineBrush onlineBrush && onlineBrush.IsOnline) || _sourcePaint.Shader is null || _sourceImage is null)
			{
				Source?.UpdatePaint(_sourcePaint, bounds);

				if (_surface is null)
					_surface = SKSurface.Create(new SKImageInfo((int)bounds.Width, (int)bounds.Height, SKImageInfo.PlatformColorType, SKAlphaType.Premul));

				if (_surface is not null)
				{
					_surface.Canvas.DrawRect(bounds, _sourcePaint);
					_surface.Canvas.Flush();
					_sourceImage?.Dispose();
					_sourceImage = _surface.Snapshot();
				}
			}

			if (_sourceImage is not null)
			{
				_insetRect.Top = (int)(TopInset * TopInsetScale);
				_insetRect.Bottom = (int)(bounds.Height - (BottomInset * BottomInsetScale));
				_insetRect.Right = (int)(bounds.Width - (RightInset * RightInsetScale));
				_insetRect.Left = (int)(LeftInset * LeftInsetScale);

				session.Surface?.Canvas.DrawImageNinePatch(_sourceImage, _insetRect, bounds);
			}
		}
	}
}
