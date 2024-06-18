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

		internal override void Paint(in PaintingSession session)
		{
			base.Paint(in session);

			if (Brush is IOnlineBrush onlineBrush && onlineBrush.IsOnline)
			{
				onlineBrush.Paint(session, new SKRect(left: 0, top: 0, right: Size.X, bottom: Size.Y));
				return;
			}

			_paint.ColorFilter = session.Filters.OpacityColorFilter;

			if (Brush is CompositionEffectBrush { HasBackdropBrushInput: true })
			{
				// workaround until SkiaSharp adds support for SaveLayerRec, see https://github.com/mono/SkiaSharp/issues/2773

				// Here, we need to draw directly on the surface's canvas, otherwise
				// you can get an AccessViolationException (most likely because DrawSurface needs the
				// receiver SKCanvas object to be the same as the first argument's SKSurface.Canvas).
				// session.Canvas is possibly a RecorderCanvas from an SKPictureRecorder, so we bypass
				// it and use Surface.Canvas.
				var sessionCanvas = session.Surface.Canvas;
				sessionCanvas.SaveLayer(_paint);
				sessionCanvas.Scale(1.0f / sessionCanvas.TotalMatrix.ScaleX);
				sessionCanvas.DrawSurface(session.Surface, new(-sessionCanvas.TotalMatrix.TransX, -sessionCanvas.DeviceClipBounds.Top + sessionCanvas.LocalClipBounds.Top));
				sessionCanvas.Restore();
			}
			else
			{
				session.Canvas.DrawRect(
					new SKRect(left: 0, top: 0, right: Size.X, bottom: Size.Y),
					_paint
				);
			}
		}

		internal override bool RequiresRepaintOnEveryFrame => (Brush?.RequiresRepaintOnEveryFrame ?? false);

		internal override bool CanPaint => true;
	}
}
