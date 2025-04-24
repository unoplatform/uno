#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;
using Uno.UI.Composition;

namespace Microsoft.UI.Composition
{
	public partial class CompositionMaskBrush : CompositionBrush, IOnlineBrush
	{
		private SKPaint? _sourcePaint;
		private SKPaint? _maskPaint;

		bool IOnlineBrush.IsOnline
		{
			get
			{
				return Source is IOnlineBrush { IsOnline: true } || Mask is IOnlineBrush { IsOnline: true };
			}
		}

		internal override bool RequiresRepaintOnEveryFrame => ((IOnlineBrush)this).IsOnline;

		internal override void UpdatePaint(SKPaint paint, SKRect bounds)
		{
			_sourcePaint ??= paint.Clone();
			_maskPaint ??= paint.Clone();

			Source?.UpdatePaint(_sourcePaint, bounds);
			Mask?.UpdatePaint(_maskPaint, bounds);
			paint.Shader = SKShader.CreateCompose(_sourcePaint.Shader, _maskPaint.Shader, SKBlendMode.DstIn);
		}

		internal override bool CanPaint() => (Source?.CanPaint() ?? false) || (Mask?.CanPaint() ?? false);

		private static SKPaint _spareResultPaint = new SKPaint();

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
