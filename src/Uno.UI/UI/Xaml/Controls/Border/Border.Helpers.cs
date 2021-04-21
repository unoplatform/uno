using Windows.Foundation;
using Uno.UI;
using Uno.UI.Extensions;
using Uno.UI.Helpers;

namespace Windows.UI.Xaml.Controls
{
	partial class Border
	{
		internal static Size HelperGetCombinedThickness(FrameworkElement element)
		{
			var thickness = element.GetBorderThickness();

			if (Border.UseLayoutRoundingForBorderThickness(element))
			{
				thickness = GetLayoutRoundedThickness(element);
			}

			var border = HelperCollapseThickness(thickness);

			var padding = HelperCollapseThickness(element.GetPadding());

			var combined = new Size(
				width: border.Width + padding.Width,
				height: border.Height + padding.Height);

			return combined;
		}

		private static Size HelperCollapseThickness(Thickness thickness)
		{
			return new Size(thickness.Left + thickness.Right, thickness.Top + thickness.Bottom);
		}

		internal static bool UseLayoutRoundingForBorderThickness(FrameworkElement element)
		{
			// Only snap BorderThickness if:
			// 1) LayoutRounding is enabled - prerequisite for any pixel snapping.
			// 2) There is no CornerRadius - we cannot guarantee snapping when there is a CornerRadius.
			// 3) Plateau != 1.0 - this snapping is to ensure that integral values of BorderThickness don't cause subpixel rendering at high plateau.
			// TODO: Remove check for plateau, BorderThickness should be consistently rounded at all plateaus.
			//return (RootScale.GetRasterizationScaleForElement(element) != 1.0f) &&
			//       element.GetUseLayoutRounding() &&
			//       (!HasNonZeroCornerRadius(element.GetCornerRadius()));
			return (RootScale.GetRasterizationScaleForElement(element) != 1.0f) &&
				   element.GetUseLayoutRounding() &&
				   (!HasNonZeroCornerRadius(element.GetCornerRadius()));
		}

		internal static bool HasNonZeroCornerRadius(CornerRadius cornerRadius)
		{
			return (cornerRadius.TopLeft > 0.0f) ||
			       (cornerRadius.TopRight > 0.0f) ||
			       (cornerRadius.BottomRight > 0.0f) ||
			       (cornerRadius.BottomLeft > 0.0f);
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

			if (UseLayoutRoundingForBorderThickness(element))
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
	}
}
