using System;
using System.Collections.Generic;
using SkiaSharp;
using Uno.Disposables;
using Windows.UI;

namespace Microsoft.UI.Composition
{
	public partial class CompositionColorBrush
	{
		internal override void Paint(SKCanvas canvas, float opacity, SKRect bounds)
		{
			canvas.Save();
			canvas.ClipRect(bounds, antialias: true);
			canvas.DrawColor(Color.ToSKColor(opacity));
			canvas.Restore();
		}

		internal override bool CanPaint() => Color != Colors.Transparent;
	}
}
