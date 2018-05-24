using System;
using System.Collections.Generic;
using System.Text;
using CoreGraphics;
using UIKit;

namespace Windows.UI.Xaml.Controls
{
	internal partial interface IScrollContentPresenter
	{
		CGPoint UpperScrollLimit { get; }
		UIEdgeInsets ContentInset { get; set; }
		void SetContentOffset(CGPoint contentOffset, bool animated);
		void SetZoomScale(nfloat scale, bool animated);
	}
}
