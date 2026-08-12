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
		internal override float DamageRegionSamplingMargin => Source?.DamageRegionSamplingMargin ?? 0;

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

			// Rasterize the source brush into an offscreen backend texture and draw it nine-sliced onto the target
			// (no CPU round-trip — the offscreen result is already the texture the draw verb consumes).
			using var texture = DrawingFactory.Current.RenderOffscreen(pixelWidth, pixelHeight, s => Source.TryPaint(s, opacity, sourceBounds));

			var centerSlice = new Rect(
				new Point(LeftInset * LeftInsetScale, TopInset * TopInsetScale),
				new Point(sourceBounds.Width - (RightInset * RightInsetScale), sourceBounds.Height - (BottomInset * BottomInsetScale)));

			session.DrawImageNineSlice(texture, centerSlice, bounds, IsCenterHollow, antialias: true);
			return true;
		}

		internal override bool CanPaint() => Source?.CanPaint() ?? false;
	}
}
