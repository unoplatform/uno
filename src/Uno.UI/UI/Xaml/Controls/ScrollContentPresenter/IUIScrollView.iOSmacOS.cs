using System;
using System.Collections.Generic;
using System.Text;
using CoreGraphics;

#if NET6_0_OR_GREATER
using ObjCRuntime;
#endif

namespace Windows.UI.Xaml.Controls
{
	internal interface IUIScrollView
	{
		CGPoint UpperScrollLimit { get; }

		void SetContentOffset(CGPoint contentOffset, bool animated);

#if __IOS__
		void SetZoomScale(nfloat scale, bool animated);
#endif
	}
}
