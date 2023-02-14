using System;
using System.Collections.Generic;
using System.Text;
using CoreGraphics;

#if NET6_0_OR_GREATER
using ObjCRuntime;
#endif

namespace Microsoft.UI.Xaml.Controls
{
	internal interface IUIScrollView
	{
		CGPoint UpperScrollLimit { get; }

		CGPoint ContentOffset { get; }

		void SetContentOffset(CGPoint contentOffset, bool animated);

		void ApplyContentOffset(CGPoint contentOffset, bool animated);

#if __IOS__
		nfloat ZoomScale { get; }

		void SetZoomScale(nfloat scale, bool animated);

		void ApplyZoomScale(nfloat scale, bool animated);
#endif
	}
}
