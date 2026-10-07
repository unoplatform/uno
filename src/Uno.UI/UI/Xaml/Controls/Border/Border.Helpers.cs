using System.Diagnostics;
using Windows.Foundation;
using Uno.UI;
using Uno.UI.Extensions;
using Uno.UI.Helpers;
using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Core;

namespace Microsoft.UI.Xaml.Controls;

partial class Border
{
	internal static Size HelperGetCombinedThickness(FrameworkElement element)
	{
		var thickness = element.GetBorderThickness();

		if (element.GetUseLayoutRounding())
		{
			thickness = GetLayoutRoundedThickness(element);
		}

		// Compute the chrome size added by the border
		var border = HelperCollapseThickness(thickness);

		// Compute the chrome size added by the padding.
		// No need to adjust for layout rounding here since padding is not "drawn" by the border.
		var padding = HelperCollapseThickness(element.GetPadding());

		// Combine both.
		var combined = new Size(
			width: border.Width + padding.Width,
			height: border.Height + padding.Height);

		return combined;
	}

	private static Size HelperCollapseThickness(Thickness thickness)
	{
		return new Size(thickness.Left + thickness.Right, thickness.Top + thickness.Bottom);
	}

	internal static Thickness GetLayoutRoundedThickness(FrameworkElement element)
	{
		// Layout rounding will correctly round element sizes and offsets at the current plateau,
		// but does not round BorderThicnkess. Since plateau scale is applied as a scale transform at the
		// root element, all values will be scaled by it including BorderThickness so if a user sets
		// BorderThickness = 1 at PLateau=1.4 this will be scaled to 1.4, producing blurry edges at the
		// inner edges and other rendering artifacts. This method rounds the BorderThickness at the current plateau
		// using plateau-aware LayoutRound utility.
		var roundedThickness = new Thickness();
		var thickness = element.GetBorderThickness();
		roundedThickness.Left = element.LayoutRound(thickness.Left);
		roundedThickness.Right = element.LayoutRound(thickness.Right);
		roundedThickness.Top = element.LayoutRound(thickness.Top);
		roundedThickness.Bottom = element.LayoutRound(thickness.Bottom);

		return roundedThickness;
	}

	internal static Rect HelperGetInnerRect(FrameworkElement element, Size outerSize)
	{
		var thickness = element.GetBorderThickness();
		// Set up the bound rectangle
		var outerRect = new Rect(0, 0, outerSize.Width, outerSize.Height);

		if (element.GetUseLayoutRounding())
		{
			outerRect.Width = element.LayoutRound(outerRect.Width);
			outerRect.Height = element.LayoutRound(outerRect.Height);
			thickness = GetLayoutRoundedThickness(element);
		}

		// Calculate the inner one
		HelperDeflateRect(outerRect, thickness, out var innerRect);
		HelperDeflateRect(innerRect, element.GetPadding(), out var rcChild);

		return rcChild;
	}

	internal static void HelperDeflateRect(Rect rect, Thickness thickness, out Rect innerRect)
	{
		innerRect = rect.DeflateBy(thickness);
	}

	// MUX Reference dxaml\xcp\core\core\elements\Border.cpp, tag winui3/release/2.5.4-experimental
	internal static bool HasNonZeroCornerRadius(CornerRadius cornerRadius)
		=> cornerRadius.TopLeft != 0 || cornerRadius.TopRight != 0 || cornerRadius.BottomRight != 0 || cornerRadius.BottomLeft != 0;

	//------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Test if a point/polygon intersects with the element in local space.
	//
	//------------------------------------------------------------------------
	// TODO Uno: WinUI reads the core-wide flag (CCoreServices::InvisibleHitTestMode), Uno passes it in.
	internal static bool HitTestLocalInternalImpl(FrameworkElement pElement, Point target, bool invisibleHitTestMode)
	{
		Debug.Assert(pElement is Panel or Border or ContentPresenter);

		var borderInfo = (IBorderInfoProvider)pElement;
		var outerRect = new Rect(0, 0, pElement.ActualWidth, pElement.ActualHeight);
		var intersectsTarget = false;

		if (outerRect.Width != 0 && outerRect.Height != 0)
		{
			var cornerRadius = borderInfo.CornerRadius;
			var thickness = borderInfo.BorderThickness;
			var backgroundBrush = borderInfo.Background;
			var borderBrush = borderInfo.BorderBrush;
			var extendBackgroundUnderBorder = borderInfo.BackgroundSizing == BackgroundSizing.OuterBorderEdge;
			var useComplexDrawing = HasNonZeroCornerRadius(cornerRadius);
			var hasValidBorder = borderBrush != null;
			var hasValidBackground = backgroundBrush != null || invisibleHitTestMode;
			HelperDeflateRect(outerRect, thickness, out var innerRect);
			var targetIntersectsInnerRect = false;
			var targetIntersectsOuterRect = false;

			if (hasValidBorder || (hasValidBackground && extendBackgroundUnderBorder))
			{
				targetIntersectsOuterRect = DoesBorderRectIntersectHitType(
					outerRect,
					useComplexDrawing,
					thickness,
					cornerRadius,
					target,
					true /* isOuter */);
			}

			// If there is a background brush that extends underneath the border,
			// we hit test against this outer rect to see if it contains the point.
			// Note that if InvisibleHitTestMode is set and the background is null,
			// we test this element as if it had a solid color background instead,
			// i.e. if the point intersects, it's considered a hit.
			if (hasValidBackground && extendBackgroundUnderBorder && targetIntersectsOuterRect)
			{
				// TODO Uno: Brushes do not clip hit testing (HitTestBrushClipInLocalSpace), the brush is hit wherever it is drawn.
				intersectsTarget = true;
			}

			// If we have not confirmed a hit yet, there are a few other scenarios
			// we have to test. First, we have to determine if we need to test for
			// a hit within the inner rect (if it exists at all). We only need to
			// do that if we have a) a valid inset background, or b) if we have a
			// valid border, because we will use a hit within the inner rect as an
			// exclusion to verify that a hit within the outer rect did indeed hit
			// the border stroke.
			if (!intersectsTarget
				&& ((hasValidBackground && !extendBackgroundUnderBorder) || hasValidBorder)
				&& innerRect.Width != 0
				&& innerRect.Height != 0)
			{
				targetIntersectsInnerRect = DoesBorderRectIntersectHitType(
					innerRect,
					useComplexDrawing,
					thickness,
					cornerRadius,
					target,
					false /* isOuter */);
			}

			// At this point, if the target intersects with the inner rect and
			// InvisibleHitTestMode is set, this is considered a hit. If it is
			// not set, but there is an inset background brush, we do a hit test on
			// the brush.
			if (!intersectsTarget && hasValidBackground && !extendBackgroundUnderBorder && targetIntersectsInnerRect)
			{
				// TODO Uno: Brushes do not clip hit testing (HitTestBrushClipInLocalSpace).
				intersectsTarget = true;
			}

			// If we have not confirmed a hit yet and there is a border brush, then
			// we must test if the point hits this outer stroke. Note that we don't
			// do any additional work for InvisibleHitTestMode here; if the border
			// brush is null, we treat hit testing as if the thickness is zero.
			// Now, the border is the difference of the outer rect and the inner
			// rect, so a hit on the border can be successful only if it lands on
			// the outer rect and not within the inner rect.
			if (!intersectsTarget && hasValidBorder && targetIntersectsOuterRect && !targetIntersectsInnerRect)
			{
				// TODO Uno: Brushes do not clip hit testing (HitTestBrushClipInLocalSpace).
				intersectsTarget = true;
			}
		}

		return intersectsTarget;
	}

	// Perform rounded corner hit-testing for the element itself.
	// In this scenario we care only about hit-testing the actual bounds of the element,
	// clipped down to its rounded corner rectangle shape.  Content is not relevant here.
	internal static bool HitTestRoundedCornerClip(FrameworkElement element, CornerRadius cornerRadius, Point target)
	{
		// TODO Uno: CFrameworkElement::GetCornerRadius is virtual in WinUI, the caller resolves it.
		var actualBounds = new Rect(0, 0, element.ActualWidth, element.ActualHeight);

		// No border thickness and isOuter = false, so the radii are the corner radius fitted into the bounds.
		return DrawRoundedCornersRectangleContains(actualBounds, cornerRadius, default, false, target);
	}

	//------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Verifies if a hit intersects within a given rect constructed
	//      from a border.
	//
	//------------------------------------------------------------------------
	private static bool DoesBorderRectIntersectHitType(
		Rect rect,
		bool useComplexDrawing,
		Thickness borderThickness,
		CornerRadius cornerRadius,
		Point target,
		bool isOuter)
	{
		if (useComplexDrawing)
		{
			return DrawRoundedCornersRectangleContains(rect, cornerRadius, borderThickness, isOuter, target);
		}
		else
		{
			return rect.Contains(target);
		}
	}

	// TODO Uno: WinUI streams CGeometryBuilder::DrawRoundedCornersRectangle into a point hit test geometry sink
	// (with a 0.25 tolerance); Uno tests the same rounded rectangle analytically.
	private static bool DrawRoundedCornersRectangleContains(Rect rc, CornerRadius cornerRadius, Thickness borders, bool fOuter, Point target)
	{
		if (!rc.Contains(target))
		{
			return false;
		}

		var fullRadii = cornerRadius.GetRadii(new Size(rc.Width, rc.Height), borders);
		var radii = fOuter ? fullRadii.Outer : fullRadii.Inner;

		var x = target.X - rc.X;
		var y = target.Y - rc.Y;

		return IsInsideCorner(radii.TopLeft, x, y)
			&& IsInsideCorner(radii.TopRight, rc.Width - x, y)
			&& IsInsideCorner(radii.BottomRight, rc.Width - x, rc.Height - y)
			&& IsInsideCorner(radii.BottomLeft, x, rc.Height - y);

		// dx/dy are the distances from the two edges that meet at the corner.
		static bool IsInsideCorner(global::System.Numerics.Vector2 radius, double dx, double dy)
		{
			if (radius.X <= 0 || radius.Y <= 0 || dx >= radius.X || dy >= radius.Y)
			{
				return true;
			}

			var nx = (radius.X - dx) / radius.X;
			var ny = (radius.Y - dy) / radius.Y;
			return nx * nx + ny * ny <= 1;
		}
	}
}
