using SkiaSharp;
using Windows.UI.Xaml.Media;

namespace Uno.UI.UI.Xaml.Media
{
	internal static class FillRuleExtensions
	{
		internal static SKPathFillType ToSkiaFillType(this FillRule fillRule)
		{
			return fillRule == FillRule.EvenOdd ? SKPathFillType.EvenOdd : SKPathFillType.Winding;
		}
	}
}
