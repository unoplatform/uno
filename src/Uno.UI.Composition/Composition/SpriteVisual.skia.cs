#nullable enable

using SkiaSharp;
using Uno.UI.Composition;

using Color = global::Windows/*Intentional space for WinUI upgrade tool*/.UI.Color;

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

		/// <param name="color">color to set SKPaint to, null to reset</param>
		internal void SetPaintColor(Color? color)
		{
			if (color is { } c)
			{
				_paint.Color = c;
			}
			else
			{
				_paint.Color = SKColors.Black; // resets to default, equivalent to `_paint.Color = new SKPaint().Color`
			}
			UpdatePaint();
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
				// workaround until SkiaSharp adds support for SaveLayerRec, see https://github.com/mono/SkiaSharp/issues/2773
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
