#nullable enable

using SkiaSharp;
using Uno.UI.Composition;

using Color = global::Windows/*Intentional space for WinUI upgrade tool*/.UI.Color;

namespace Microsoft.UI.Composition
{
	public partial class SpriteVisual : ContainerVisual
	{
		private readonly SKPaint _paint = new() { IsAntialias = true };

		partial void OnBrushChangedPartial(CompositionBrush? brush) => UpdatePaint();

		private void UpdatePaint()
		{
			if (!Brush?.SupportsRender ?? false)
			{
				Brush?.UpdatePaint(_paint, new SKRect(left: 0, top: 0, right: Size.X, bottom: Size.Y));
			}
		}

		internal override void Paint(in PaintingSession session)
		{
			if (Brush is null)
			{
				return;
			}

			if (Brush.SupportsRender)
			{
				Brush.Render(session.Canvas, new SKRect(left: 0, top: 0, right: Size.X, bottom: Size.Y));
				return;
			}

			if (Brush is IOnlineBrush onlineBrush && onlineBrush.IsOnline)
			{
				onlineBrush.Paint(session, new SKRect(left: 0, top: 0, right: Size.X, bottom: Size.Y));
				return;
			}

			_paint.ColorFilter = session.OpacityColorFilter;

			if (Brush is CompositionEffectBrush { HasBackdropBrushInput: true })
			{
				session.Canvas.SaveLayer(new SKCanvasSaveLayerRec { Backdrop = _paint.ImageFilter });
				session.Canvas.Restore();
			}
			else
			{
				session.Canvas.DrawRect(
					new SKRect(left: 0, top: 0, right: Size.X, bottom: Size.Y),
					_paint
				);
			}
		}

		internal override bool CanPaint() => Brush?.CanPaint() ?? false;
		internal override bool RequiresRepaintOnEveryFrame => Brush?.RequiresRepaintOnEveryFrame ?? false;
	}
}
