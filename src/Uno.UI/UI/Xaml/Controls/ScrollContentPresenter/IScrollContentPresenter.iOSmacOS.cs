using System;
using System.Collections.Generic;
using System.Text;
using CoreGraphics;

#if __IOS__
using _EdgeInsets = UIKit.UIEdgeInsets;
#elif __MACOS__
using _EdgeInsets = AppKit.NSEdgeInsets;
#endif

namespace Windows.UI.Xaml.Controls
{
	internal partial interface IScrollContentPresenter : IUIScrollView
	{
		_EdgeInsets ContentInset { get; set; }
	}
}
