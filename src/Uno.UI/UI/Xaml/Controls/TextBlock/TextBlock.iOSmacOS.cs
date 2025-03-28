using System;
using CoreGraphics;
using ObjCRuntime;

namespace Windows.UI.Xaml.Controls
{
	public partial class TextBlock
	{
		private CGRect GetDrawRect(CGRect rect)
		{
			// avoid calling the getter for each property
			var padding = Padding;

			// Reduce available size by Padding
			rect.Width -= (nfloat)(padding.Left + padding.Right);
			rect.Height -= (nfloat)(padding.Top + padding.Bottom);

			// Offset drawing location by Padding
			rect.X += (nfloat)padding.Left;
			rect.Y += (nfloat)padding.Top;

			return rect;
		}
	}
}
