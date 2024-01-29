using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Uno.UI;

namespace Microsoft.UI.Xaml.Shapes;

internal partial class BorderLayerRenderer
{
	/* The border is defined by the inner and outer rounded-rectangles.
	 * Each rounded rectangle is composed of 4x lines and 4x 90' arcs.
	 * ╭─────╮
	 * │A 2 B│
	 * │1   3│
	 * │D 4 C│
	 * ╰─────╯
	 * Three factors determine the border: BorderThickness, CornerRadius, and AvailableSize.
	 * What each part affects:
	 * - CornerRadius="1A2,2B3,3C4,4D1"
	 * - BorderThickness="D1A,A2B,B3C,C4D"
	 * - AvailableSize is used to constrain the radius.
	 *
	 * note: technically CornerRadius (together with AvailableSize) also affects the adjacent corner,
	 * as both points on the same axis will compete for what's available when both add up exceeds
	 * the available length. (see: h/vRatio in CalculateBorderEllipse)
	 */

	public enum Corner { TopLeft, TopRight, BottomRight, BottomLeft }

	public static Rect CalculateBorderEllipseBbox(Rect bbox, Corner corner, CornerRadius cr, Thickness bt, bool inner = false)
	{
		var (cr0, hThickness, vThickness, hComplement, vComplement) = corner switch
		{
			Corner.TopLeft => (cr.TopLeft, bt.Left, bt.Top, cr.TopRight, cr.BottomLeft),
			Corner.TopRight => (cr.TopRight, bt.Right, bt.Top, cr.TopLeft, cr.BottomRight),
			Corner.BottomRight => (cr.BottomRight, bt.Right, bt.Bottom, cr.BottomLeft, cr.TopRight),
			Corner.BottomLeft => (cr.BottomLeft, bt.Left, bt.Bottom, cr.BottomRight, cr.TopLeft),

			_ => throw new ArgumentOutOfRangeException($"Invalid corner: {corner}"),
		};

		// there is still a corner to be painted, albeit not rounded.
		if (cr0 == 0) return AlignCorner(bbox, corner, default);

		// The ellipse can only grow up to twice the available length.
		// This is further limited by the ratio between the corner-radius
		// of that corner and the adjacent corner on the same line.
		var hRatio = hComplement == 0 ? 1 : (cr0 / (cr0 + hComplement));
		var vRatio = vComplement == 0 ? 1 : (cr0 / (cr0 + vComplement));

		// if size is empty here, there is still a corner to be painted, just not rounded.
		var size = new Size(
			width: Math.Max(0, Math.Min(bbox.Width * 2 * hRatio, cr0 * 2 + (inner ? -hThickness : hThickness))),
			height: Math.Max(0, Math.Min(bbox.Height * 2 * vRatio, cr0 * 2 + (inner ? -vThickness : vThickness)))
		);
		var result = AlignCorner(bbox, corner, size);

		return result;
	}

	/// <summary>
	/// Arrange a size on the bounding-<paramref name="bbox"/> so that the borders neighboring
	/// to the <paramref name="corner"/> are overlapped.
	/// </summary>
	/// <remarks>The resulting rect can be outside of the bounding-box.</remarks>
	private static Rect AlignCorner(Rect bbox, Corner corner, Size size)
	{
		// note: the ellipse can project outside the bounding-box
		// because only a quarter needs to be constrained within.
		var location = (Point)(corner switch
		{
			Corner.TopLeft => new(bbox.Left, bbox.Top),
			Corner.TopRight => new(bbox.Left + bbox.Width - size.Width, bbox.Top),
			Corner.BottomRight => new(bbox.Left + bbox.Width - size.Width, bbox.Top + bbox.Height - size.Height),
			Corner.BottomLeft => new(bbox.Left, bbox.Top + bbox.Height - size.Height),

			_ => throw new ArgumentOutOfRangeException($"Invalid corner: {corner}"),
		});

		return new(location, size);
	}
}
