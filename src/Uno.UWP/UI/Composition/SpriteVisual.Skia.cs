using System;
using System.Collections.Generic;
using System.Numerics;
using SkiaSharp;
using Uno.Extensions;
using Uno.Logging;

namespace Windows.UI.Composition
{
	public partial class SpriteVisual : ContainerVisual
	{
		private readonly SKPaint _paint
			= new SKPaint()
			{
				IsAntialias = true,
			};

		partial void OnBrushChangedPartial(CompositionBrush brush)
		{
			if (brush is CompositionColorBrush b)
			{
				_paint.Color = new SKColor(b.Color.R, b.Color.G, b.Color.B, b.Color.A);
			}
			else
			{
				this.Log().Error($"The brush type {brush?.GetType()} is not supported for sprite visuals.");
			}
		}

		internal override void Render(SKSurface surface, SKImageInfo info)
		{
			base.Render(surface, info);

			surface.Canvas.Save();

			surface.Canvas.DrawRect(
				new SKRect(left: 0, top: 0, right: Size.X, bottom: Size.Y),
				_paint
			);

			surface.Canvas.Restore();
		}
	}
}
