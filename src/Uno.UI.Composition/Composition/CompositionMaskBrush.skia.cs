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
		SKPaint? _sourcePaint;
		SKPaint? _maskPaint;
		SKPaint? _resultPaint;

		bool IOnlineBrush.IsOnline
		{
			get
			{
				return (Source is IOnlineBrush onlineSourceBrush && onlineSourceBrush.IsOnline) || (Mask is IOnlineBrush onlineMaskBrush && onlineMaskBrush.IsOnline);
			}
		}

		internal override void UpdatePaint(SKPaint paint, SKRect bounds)
		{
			if (_sourcePaint is null) _sourcePaint = paint.Clone();
			if (_maskPaint is null) _maskPaint = paint.Clone();

			Source?.UpdatePaint(_sourcePaint, bounds);
			Mask?.UpdatePaint(_maskPaint, bounds);
			paint.Shader = SKShader.CreateCompose(_sourcePaint.Shader, _maskPaint.Shader, SKBlendMode.DstIn);
		}

		void IOnlineBrush.Draw(in DrawingSession session, SKRect bounds)
		{
			if (_resultPaint is null)
				_resultPaint = new SKPaint() { IsAntialias = true };

			UpdatePaint(_resultPaint, bounds);
			session.Surface?.Canvas.DrawRect(bounds, _resultPaint);
		}

		public override void Dispose()
		{
			base.Dispose();

			_sourcePaint?.Dispose();
			_maskPaint?.Dispose();
			_resultPaint?.Dispose();
		}
	}
}
