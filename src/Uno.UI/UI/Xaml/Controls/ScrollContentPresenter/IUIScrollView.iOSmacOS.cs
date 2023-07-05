#nullable disable

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

		CGPoint ContentOffset { get; }

		void ApplyContentOffset(CGPoint contentOffset, bool animated);

#if __IOS__
		nfloat ZoomScale { get; }

		void ApplyZoomScale(nfloat scale, bool animated);
#endif
	}
}
