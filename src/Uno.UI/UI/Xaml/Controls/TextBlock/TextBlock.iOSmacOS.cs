using System;
using CoreGraphics;

#if NET6_0_OR_GREATER
using ObjCRuntime;
#endif

namespace Microsoft.UI.Xaml.Controls
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
