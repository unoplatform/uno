using System;
using System.Collections.Generic;
using System.Text;

using UIKit;
using CoreAnimation;
using CoreGraphics;
using _Color = UIKit.UIColor;
using _BezierPath = UIKit.UIBezierPath;

namespace Uno.UI.Extensions
{
	internal static class NSBezierPathExtensions
	{
		public static CGPath ToCGPath(this _BezierPath nSBezierPath)
		{
			return nSBezierPath.CGPath;
		}
	}
}
