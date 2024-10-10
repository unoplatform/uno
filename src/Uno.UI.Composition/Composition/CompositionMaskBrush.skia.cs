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
	public partial class CompositionMaskBrush : CompositionBrush, IOnlineBrush
	{
		private SKPaint? _sourcePaint;
		private SKPaint? _maskPaint;
		private SKPaint? _resultPaint;

		bool IOnlineBrush.IsOnline
		{
			get
			{
				return Source is IOnlineBrush { IsOnline: true } || Mask is IOnlineBrush { IsOnline: true };
			}
		}

		internal override void UpdatePaint(SKPaint paint, SKRect bounds)
		{
			_sourcePaint ??= paint.Clone();
			_maskPaint ??= paint.Clone();

			Source?.UpdatePaint(_sourcePaint, bounds);
			Mask?.UpdatePaint(_maskPaint, bounds);
			paint.Shader = SKShader.CreateCompose(_sourcePaint.Shader, _maskPaint.Shader, SKBlendMode.DstIn);
		}

		void IOnlineBrush.Paint(in Visual.PaintingSession session, SKRect bounds)
		{
			_resultPaint ??= new SKPaint() { IsAntialias = true };

			UpdatePaint(_resultPaint, bounds);
			session.Canvas?.DrawRect(bounds, _resultPaint);
		}

		private protected override void DisposeInternal()
		{
			base.Dispose();

			_sourcePaint?.Dispose();
			_maskPaint?.Dispose();
			_resultPaint?.Dispose();
		}
	}
}
