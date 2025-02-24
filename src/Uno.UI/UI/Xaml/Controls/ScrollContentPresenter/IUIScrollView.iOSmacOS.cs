using System;
using System.Collections.Generic;
using System.Text;
using CoreGraphics;
using ObjCRuntime;

namespace Windows.UI.Xaml.Controls
{
	internal interface IUIScrollView
	{
		CGPoint UpperScrollLimit { get; }

		CGPoint ContentOffset { get; }

		void ApplyContentOffset(CGPoint contentOffset, bool animated);

#if __IOS__
		nfloat ZoomScale { get; }

		void ApplyZoomScale(nfloat scale, bool animated);
#endif
	}
}
