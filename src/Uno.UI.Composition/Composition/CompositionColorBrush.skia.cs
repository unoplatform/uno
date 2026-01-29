using System;
using System.Collections.Generic;
using SkiaSharp;
using Uno.Disposables;
using Windows.UI;

namespace Microsoft.UI.Composition
{
	public partial class CompositionColorBrush
	{
		// We don't call SKPaint.Reset() after usage, so make sure
		// that only SKPaint.Color is being set
		private static readonly SKPaint _tempPaint = new() { IsAntialias = true };

		internal override void Paint(SKCanvas canvas, float opacity, SKRect bounds)
		{
			_tempPaint.Color = Color.ToSKColor(opacity);
			canvas.DrawRect(bounds, _tempPaint);
		}

		internal override bool CanPaint() => Color != Colors.Transparent;
	}
}
