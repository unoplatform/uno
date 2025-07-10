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

		internal override void Paint(SKCanvas canvas, SKRect bounds)
		{
			_tempPaint.Reset();
			_tempPaint.Color = Color.ToSKColor();
			canvas.DrawRect(bounds, _tempPaint);
		}

		internal override bool CanPaint() => Color != Colors.Transparent;
	}
}
