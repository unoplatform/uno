#nullable enable

using SkiaSharp;
using Uno.UI.Composition;

namespace Microsoft.UI.Composition
{
	public partial class CompositionMaskBrush : CompositionBrush
	{
		internal override bool RequiresRepaintOnEveryFrame => Source is not null && Mask is not null && (Source.RequiresRepaintOnEveryFrame || Mask.RequiresRepaintOnEveryFrame);

		internal override void Paint(SKCanvas canvas, float opacity, SKRect bounds)
		{
			if (Source is null || Mask is null)
			{
				return;
			}
			_spareResultPaint.Reset();
			_spareResultPaint.BlendMode = SKBlendMode.SrcOver;
			_spareResultPaint2.Reset();
			_spareResultPaint2.BlendMode = SKBlendMode.DstIn;
			// The first SaveLayer call along with DrawColor(Transparent) basically create a clean secondary drawing surface
			// but without having to call SKSurface.Create and having to deal with all the details like HWA.
			canvas.SaveLayer(new SKCanvasSaveLayerRec { Paint = _spareResultPaint });
			canvas.ClipRect(bounds);
			canvas.DrawColor(SKColors.Transparent);
			Source.Paint(canvas, opacity, bounds);
			// The second SaveLayer call with SKBlendMode.DstIn creates the masking effect
			canvas.SaveLayer(new SKCanvasSaveLayerRec { Paint = _spareResultPaint2 });
			Mask.Paint(canvas, opacity, bounds);
			canvas.Restore();
			canvas.Restore();
		}

		internal override bool CanPaint() => (Source?.CanPaint() ?? false) || (Mask?.CanPaint() ?? false);

		private static readonly SKPaint _spareResultPaint = new SKPaint();
		private static readonly SKPaint _spareResultPaint2 = new SKPaint();
	}
}
