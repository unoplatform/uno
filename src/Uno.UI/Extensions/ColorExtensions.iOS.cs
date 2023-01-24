#if __IOS__
using System;
using System.Collections.Generic;
using System.Text;
using CoreGraphics;
using UIKit;

namespace Uno.Extensions
{
	internal static class UIColorExtensions
	{
		internal static UIImage ToUIImage(this UIColor color)
		{
			var rect = new CGRect(0, 0, 1, 1);

			UIGraphics.BeginImageContext(rect.Size);

			var context = UIGraphics.GetCurrentContext();
			context.SetFillColor(color.CGColor);
			context.FillRect(rect);
			var image = UIGraphics.GetImageFromCurrentImageContext();

			UIGraphics.EndImageContext();

			return image;
		}
	}
}
#endif
