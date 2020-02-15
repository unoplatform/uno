using System;
using System.Collections.Generic;
using System.Text;
using CoreGraphics;
using Uno.UI;
#if __IOS__
using UIKit;
#else
using AppKit;
#endif

namespace Windows.UI.Xaml.Controls.Primitives
{ 
	public partial class FlyoutBase
    {
		partial void InitializePopupPanelPartial()
		{
			_popup.PopupPanel = new FlyoutBasePopupPanel(this)
			{
				Visibility = Visibility.Collapsed,
				Background = SolidColorBrushHelper.Transparent,
#if __IOS__
				AutoresizingMask = UIViewAutoresizing.All,
#else
				AutoresizingMask =
					NSViewResizingMask.HeightSizable |
					NSViewResizingMask.WidthSizable |
					NSViewResizingMask.MinXMargin |
					NSViewResizingMask.MaxXMargin |
					NSViewResizingMask.MinYMargin |
					NSViewResizingMask.MaxYMargin,
#endif
				Frame = new CGRect(CGPoint.Empty, ViewHelper.GetScreenSize())
			};
		}
	}
}
