#nullable enable

using System;
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

			// Device resolution, not logical: the nine-slice is stretched into the destination under the session's
			// transform, so a logical-sized texture is magnified (and softened) by exactly that scale.
			var scale = GetRasterizationScale(session);
			var pixelWidth = (int)Math.Ceiling(sourceBounds.Width * scale.X);
			var pixelHeight = (int)Math.Ceiling(sourceBounds.Height * scale.Y);
			if (pixelWidth <= 0 || pixelHeight <= 0)
			{
				return true;
			}

			// Rasterize the source brush into an offscreen backend texture and draw it nine-sliced onto the target
			// (no CPU round-trip — the offscreen result is already the texture the draw verb consumes).
			// The source's own graph has to be built before the offscreen pass opens: parsing it inside would nest
			// another offscreen inside this one, which a backend that cannot re-enter a pass refuses, and it must
			// prepare at the same scale it is about to paint at or it rebuilds inside the pass anyway.
			Source.PrepareForOffscreenRasterization(session.Factory, sourceBounds, scale);
			using var texture = session.Factory.RenderOffscreen(pixelWidth, pixelHeight, s =>
			{
				s.Scale(scale.X, scale.Y);
				Source.TryPaint(s, opacity, sourceBounds);
			});

			// The slice rectangle is in image pixels, so it follows the texture to device resolution.
			var centerSlice = new Rect(
				new Point(LeftInset * LeftInsetScale * scale.X, TopInset * TopInsetScale * scale.Y),
				new Point((sourceBounds.Width - (RightInset * RightInsetScale)) * scale.X, (sourceBounds.Height - (BottomInset * BottomInsetScale)) * scale.Y));

			session.DrawImageNineSlice(texture, centerSlice, bounds, IsCenterHollow);
			return true;
		}

		internal override bool CanPaint() => Source?.CanPaint() ?? false;
	}
}
