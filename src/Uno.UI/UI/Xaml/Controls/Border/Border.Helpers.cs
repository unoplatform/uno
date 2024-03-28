using Windows.Foundation;
using Uno.UI;
using Uno.UI.Extensions;
using Uno.UI.Helpers;
using Uno.UI.Xaml.Core;

namespace Windows.UI.Xaml.Controls;

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
}
