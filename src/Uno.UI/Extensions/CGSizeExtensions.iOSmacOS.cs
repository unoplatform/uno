using System;
using System.Collections.Generic;
using System.Text;
using CoreGraphics;
using Windows.Foundation;
using Windows.UI.Xaml;

namespace CoreGraphics
{
	public static class CGSizeExtensions
	{
		public static Windows.Foundation.Size ToFoundationSize(this CGSize size)
			=> new Size(
				size.Width,
				size.Height
			);

		public static bool HasZeroArea(this CGSize size)
			=> size.Height == 0 || size.Width == 0;

		internal static CGSize Add(this CGSize left, Thickness right)
		{
			return new CGSize(
				left.Width + right.Left + right.Right,
				left.Height + right.Top + right.Bottom
			);
		}
	}
}
