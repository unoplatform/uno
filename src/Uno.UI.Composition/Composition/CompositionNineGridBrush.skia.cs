#nullable enable

using System.Numerics;
using Uno.UI.Composition;
using Uno.UI.Composition.Drawing;
using Windows.Foundation;

namespace Microsoft.UI.Composition
{
	public partial class CompositionNineGridBrush : CompositionBrush
	{
		internal override bool RequiresRepaintOnEveryFrame => Source?.RequiresRepaintOnEveryFrame ?? false;

		internal override bool TryPaint(IDrawingSession session, float opacity, Rect bounds)
		{
			if (Source is null)
			{
				return true;
			}

			var sourceBounds = Source is ISizedBrush { Size: Vector2 sourceSize }
				? new Rect(0, 0, sourceSize.X, sourceSize.Y)
				: bounds;

			var pixelWidth = (int)sourceBounds.Width;
			var pixelHeight = (int)sourceBounds.Height;
			if (pixelWidth <= 0 || pixelHeight <= 0)
			{
				return true;
			}

			// Rasterize the source brush into an offscreen image, then draw it nine-sliced onto the target.
			var image = DrawingBackend.Current.RenderOffscreen(pixelWidth, pixelHeight, s => Source.TryPaint(s, opacity, sourceBounds));

			var centerSlice = new Rect(
				new Point(LeftInset * LeftInsetScale, TopInset * TopInsetScale),
				new Point(sourceBounds.Width - (RightInset * RightInsetScale), sourceBounds.Height - (BottomInset * BottomInsetScale)));

			session.DrawImageNineSlice(image, centerSlice, bounds, IsCenterHollow, new PaintParams(global::Windows.UI.Colors.White) { IsAntialias = true });
			return true;
		}

		internal override bool CanPaint() => Source?.CanPaint() ?? false;
	}
}
