#nullable enable

using System.Collections.Generic;
using Uno.UI.Composition;
using Uno.UI.Composition.Drawing;
using Windows.Foundation;

using Color = global::Windows.UI.Color;

namespace Microsoft.UI.Composition
{
	public partial class SpriteVisual : ContainerVisual
	{
		internal override IGeometry? Paint(in PaintingSession session)
		{
			Brush?.TryPaint(session.Session, session.Opacity, new Rect(0, 0, Size.X, Size.Y));
			return null;
		}

		internal override bool CanPaint() => Brush?.CanPaint() ?? false;

		// Paint draws the Brush across (0, 0, Size.X, Size.Y) — by construction it doesn't go outside.
		internal override bool PaintsWithinOwnSize => true;

		internal override bool RequiresRepaintOnEveryFrame => Brush?.RequiresRepaintOnEveryFrame ?? false;

		private protected override bool TryAddShadowPaths(List<(IGeometry path, float alpha)> output)
		{
			// SpriteVisual fills its bounds with its Brush.
			if (Brush is null)
			{
				return true;
			}
			if (Size.X <= 0 || Size.Y <= 0)
			{
				return true;
			}

			var brush = Brush;
			while (brush is CompositionBrushWrapper wrapper)
			{
				brush = wrapper.WrappedBrush;
			}
			// Surface (image) content: ShadowState only ever carries an elevation shadow (ThemeShadow /
			// ElevatedView), which WinUI casts from the element's BOUNDS, not its sampled alpha — a full-alpha
			// rect silhouette is parity-correct and keeps the walk analytic for image-bearing subtrees.
			if (brush is CompositionSurfaceBrush or CompositionNineGridBrush)
			{
				output.Add((GeometryFactory.Current.CreateRectangleGeometry(new Rect(0, 0, Size.X, Size.Y)), 1f));
				return true;
			}
			if (!TryGetShadowBrushAlpha(brush, out var alpha))
			{
				return false;
			}
			if (alpha <= 0)
			{
				return true;
			}

			output.Add((GeometryFactory.Current.CreateRectangleGeometry(new Rect(0, 0, Size.X, Size.Y)), alpha));
			return true;
		}
	}
}
