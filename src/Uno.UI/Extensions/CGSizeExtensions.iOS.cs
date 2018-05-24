using System;
using System.Collections.Generic;
using System.Text;
using UIKit;
using CoreGraphics;
using Windows.Foundation;

namespace CoreGraphics
{
	public static class CGSizeExtensions
	{
		public static Windows.Foundation.Size ToFoundationSize(this CGSize size)
		{
			return new Size(
				size.Width,
				size.Height
			);
		}

		public static bool HasZeroArea(this CGSize size)
		{
			return size.Height == 0 || size.Width == 0;
		}
	}
}
