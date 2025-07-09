using System;
using System.Collections.Generic;
using SkiaSharp;
using Uno.Disposables;
using Windows.UI;

namespace Microsoft.UI.Composition
{
	public partial class CompositionColorBrush
	{
		private static readonly SKPaint _tempPaint = new();

		internal override void UpdatePaint(SKPaint paint, SKRect bounds)
		{
			paint.Color = Color.ToSKColor();
		}

		internal override void Render(SKCanvas canvas, SKRect bounds)
		{
			_tempPaint.Reset();
			_tempPaint.Color = Color.ToSKColor();
			canvas.DrawRect(bounds, _tempPaint);
		}

		internal override bool SupportsRender => true;
		internal override bool CanPaint() => Color != Colors.Transparent;
	}
}
