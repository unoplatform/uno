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
			session.DrawRect(bounds, new PaintParams(Color) { Opacity = opacity, IsAntialias = true });
			return true;
		}

		internal override bool CanPaint() => Color != Colors.Transparent;
	}
}
