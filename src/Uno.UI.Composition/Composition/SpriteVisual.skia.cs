#nullable enable

using SkiaSharp;
using Uno.UI.Composition;

namespace Windows.UI.Composition
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

		internal override void Draw(in DrawingSession session)
		{
			base.Draw(in session);

			if (Brush is IOnlineBrush onlineBrush && onlineBrush.IsOnline)
			{
				onlineBrush.Draw(session, new SKRect(left: 0, top: 0, right: Size.X, bottom: Size.Y));
				return;
			}

			_paint.ColorFilter = session.Filters.OpacityColorFilter;

			if (Brush is CompositionEffectBrush { HasBackdropBrushInput: true })
			{
				// workaround until SkiaSharp adds support for SaveLayerRec
				session.Surface.Canvas.SaveLayer(_paint);
				session.Surface.Canvas.Scale(1.0f / session.Surface.Canvas.TotalMatrix.ScaleX);
				session.Surface.Canvas.DrawSurface(session.Surface, new(-session.Surface.Canvas.TotalMatrix.TransX, -session.Surface.Canvas.DeviceClipBounds.Top + session.Surface.Canvas.LocalClipBounds.Top));
				session.Surface.Canvas.Restore();
			}
			else
			{
				session.Surface.Canvas.DrawRect(
					new SKRect(left: 0, top: 0, right: Size.X, bottom: Size.Y),
					_paint
				);
			}
		}
	}
}
