using Windows.UI.Xaml.Media;
using CoreAnimation;
using Foundation;

namespace Uno.UI.UI.Xaml.Media
{
	internal static class FillRuleExtensions
	{
		internal static NSString ToCAShapeLayerFillRule(this FillRule fillRule)
		{
			return fillRule == FillRule.EvenOdd ? CAShapeLayer.FillRuleEvenOdd : CAShapeLayer.FillRuleNonZero;
		}
	}
}
