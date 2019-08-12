using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using CoreAnimation;
using CoreGraphics;
using Foundation;

namespace Windows.UI.Xaml.Media
{
	public partial class LinearGradientBrush
	{
		internal CAGradientLayer GetLayer(CGSize size)
		{
			var gradientLayer = new CAGradientLayer();
			gradientLayer.Colors = GradientStops.Select(gs => (CGColor)gs.Color).ToArray();
			gradientLayer.Locations = GradientStops.Select(gs => new NSNumber(gs.Offset)).ToArray();
			var transform = RelativeTransform;

#if __IOS__
			gradientLayer.StartPoint = transform?.TransformPoint(StartPoint) ?? StartPoint;
			gradientLayer.EndPoint = transform?.TransformPoint(EndPoint) ?? EndPoint;
			return gradientLayer;
#elif __MACOS__
			throw new NotImplementedException();
#endif
		}
	}
}
