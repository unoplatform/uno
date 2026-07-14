#nullable enable

using System.Collections.Generic;
using SkiaSharp;
using Uno.UI.Composition;
using Uno.UI.Composition.Drawing;
using Windows.Foundation;

using Color = global::Windows.UI.Color;

namespace Microsoft.UI.Composition
{
	public partial class SpriteVisual : ContainerVisual
	{
		internal override void Paint(in PaintingSession session) => Brush?.Paint(session.Canvas, session.Opacity, new SKRect(left: 0, top: 0, right: Size.X, bottom: Size.Y));

		internal override bool CanPaint() => Brush?.CanPaint() ?? false;

		// Paint draws the Brush across (0, 0, Size.X, Size.Y) — by construction it doesn't go outside.
		internal override bool PaintsWithinOwnSize => true;

		internal override bool RequiresRepaintOnEveryFrame => Brush?.RequiresRepaintOnEveryFrame ?? false;

		private protected override bool TryAddShadowPaths(List<(IGeometry path, float alpha)> output)
		{
			// SpriteVisual fills its bounds with its Brush. Only solid-color brushes are describable
			// analytically
			if (Brush is null)
			{
				return true;
			}
			if (Brush is not CompositionColorBrush color)
			{
				return false;
			}
			if (color.Color.A == 0 || Size.X <= 0 || Size.Y <= 0)
			{
				return true;
			}

			output.Add((DrawingBackend.Current.CreateRectangleGeometry(new Rect(0, 0, Size.X, Size.Y)), color.Color.A / 255f));
			return true;
		}
	}
}
