#nullable disable

using System;
using System.Collections.Generic;
using System.Text;

#if XAMARIN_IOS_UNIFIED
using UIKit;
using CoreAnimation;
using CoreGraphics;
using _Color = UIKit.UIColor;
using _BezierPath = UIKit.UIBezierPath;
#elif __MACOS__
using AppKit;
using CoreAnimation;
using CoreGraphics;
using _Color = AppKit.NSColor;
using _BezierPath = AppKit.NSBezierPath;
#endif


namespace Uno.UI.Extensions
{
	internal static class NSBezierPathExtensions
	{
		public static CGPath ToCGPath(this  _BezierPath nSBezierPath)
		{
#if __IOS__
			return nSBezierPath.CGPath;
#elif __MACOS__

			// Then draw the path elements.
			var numElements = nSBezierPath.ElementCount;

			if (numElements > 0)
			{
				var path = new CGPath();
				var points = new CGPoint[3];
				var didClosePath = true;

				for (var i = 0; i < numElements; i++)
				{
					switch (nSBezierPath.ElementAt(i, out points))
					{
						case NSBezierPathElement.MoveTo:
							path.MoveToPoint(points[0].X, points[0].Y);
							break;

						case NSBezierPathElement.LineTo:
							path.AddLineToPoint(points[0].X, points[0].Y);
							didClosePath = false;
							break;

						case NSBezierPathElement.CurveTo:
							path.AddCurveToPoint(points[0].X, points[0].Y,
												points[1].X, points[1].Y,
												points[2].X, points[2].Y);
							didClosePath = false;
							break;

						case NSBezierPathElement.ClosePath:
							path.CloseSubpath();
							didClosePath = true;
							break;
					}
				}

				// Be sure the path is closed or Quartz may not do valid hit detection.
				if (!didClosePath)
				{
					path.CloseSubpath();
				}

				return path;
			}

			return new CGPath();
#endif
		}

	}
}
