#nullable enable

using SkiaSharp;

namespace Microsoft.UI.Composition
{
	public partial class SpriteVisual : ContainerVisual
	{
		private readonly SKPaint _paint
			= new SKPaint()
			{
				IsAntialias = true,
			};

		partial void OnBrushChangedPartial(CompositionBrush? brush)
		{
			UpdatePaint();
		}

		private void UpdatePaint()
		{
			Brush?.UpdatePaint(_paint, new SKRect(left: 0, top: 0, right: Size.X, bottom: Size.Y));
		}

		internal override void Render(SKSurface surface)
		{
			base.Render(surface);

			surface.Canvas.Save();

			if (Compositor.CurrentOpacity != 1.0f)
			{
				_paint.ColorFilter = Compositor.CurrentOpacityColorFilter;
			}
			else
			{
				_paint.ColorFilter = null;
			}

			surface.Canvas.DrawRect(
				new SKRect(left: 0, top: 0, right: Size.X, bottom: Size.Y),
				_paint
			);

			surface.Canvas.Restore();
		}
	}
}
