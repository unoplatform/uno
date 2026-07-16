using System;
using System.Collections.Generic;
using Uno.Disposables;
using Uno.UI.Composition.Drawing;
using Windows.Foundation;
using Windows.UI;

namespace Microsoft.UI.Composition
{
	public partial class CompositionColorBrush
	{
		internal override bool TryPaint(IDrawingSession session, float opacity, Rect bounds)
		{
			var color = opacity >= 1f ? Color : Color.FromArgb((byte)(Color.A * opacity), Color.R, Color.G, Color.B);
			session.DrawRect(bounds, color, antialias: true);
			return true;
		}

		internal override bool CanPaint() => Color != Colors.Transparent;
	}
}
