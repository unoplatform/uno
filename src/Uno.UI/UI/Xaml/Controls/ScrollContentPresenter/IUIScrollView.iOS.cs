using System;
using System.Collections.Generic;
using System.Text;
using CoreGraphics;

namespace Windows.UI.Xaml.Controls
{
	internal interface IUIScrollView
	{
		CGPoint UpperScrollLimit { get; }
		void SetContentOffset(CGPoint contentOffset, bool animated);
		void SetZoomScale(nfloat scale, bool animated);
	}
}
