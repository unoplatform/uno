#nullable enable

using SkiaSharp;
using Uno.UI.Composition;

namespace Microsoft.UI.Composition
{
	public partial class CompositionMaskBrush : CompositionBrush, IOnlineBrush
	{
		private SKPaint? _sourcePaint;
		private SKPaint? _maskPaint;

		bool IOnlineBrush.IsOnline => Source is IOnlineBrush { IsOnline: true } || Mask is IOnlineBrush { IsOnline: true };

		internal override bool RequiresRepaintOnEveryFrame => ((IOnlineBrush)this).IsOnline;

		internal override void Paint(SKCanvas canvas, SKRect bounds)
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
			Source.Paint(canvas, bounds);
			// The second SaveLayer call with SKBlendMode.DstIn creates the masking effect
			canvas.SaveLayer(new SKCanvasSaveLayerRec { Paint = _spareResultPaint2 });
			Mask.Paint(canvas, bounds);
			canvas.Restore();
			canvas.Restore();
		}

		internal override bool CanPaint() => (Source?.CanPaint() ?? false) || (Mask?.CanPaint() ?? false);

		private static readonly SKPaint _spareResultPaint = new SKPaint();
		private static readonly SKPaint _spareResultPaint2 = new SKPaint();

		void IOnlineBrush.Paint(in Visual.PaintingSession session, SKRect bounds)
		{
			var resultPaint = _spareResultPaint;

			resultPaint.Reset();

			resultPaint.IsAntialias = true;

			UpdatePaint(resultPaint, bounds);

			session.Canvas?.DrawRect(bounds, resultPaint);
		}

		private protected override void DisposeInternal()
		{
			base.DisposeInternal();

			_sourcePaint?.Dispose();
			_maskPaint?.Dispose();
		}
	}
}
