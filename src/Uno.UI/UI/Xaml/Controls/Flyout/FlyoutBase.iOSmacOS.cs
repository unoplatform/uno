using System;
using System.Collections.Generic;
using System.Text;
using CoreGraphics;
using Uno.UI;
using Uno.UI.Xaml.Controls;
#if __IOS__
using UIKit;
using View = UIKit.UIView;
#else
using AppKit;
using View = AppKit.NSView;
#endif

namespace Windows.UI.Xaml.Controls.Primitives;

public partial class FlyoutBase
{
	internal virtual View NativeTarget => null;

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
			Frame = new CGRect(CGPoint.Empty, NativeWindowWrapper.Instance.GetWindowSize())
		};
	}
}
