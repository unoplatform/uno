using System;
using System.Collections.Generic;
using SkiaSharp;
using Uno.Disposables;
using Windows.UI;

namespace Windows.UI.Composition
{
	public partial class CompositionColorBrush
	{
		internal override void UpdatePaint(SKPaint paint, SKRect bounds)
		{
			paint.Color = Color.ToSKColor();
		}
	}
}
