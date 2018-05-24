using System;
using System.Collections.Generic;
using System.Text;
using CoreGraphics;
using UIKit;
using Uno.UI;

namespace Windows.UI.Xaml.Controls
{ 
	public partial class Flyout
    {
		partial void InitializePartial()
		{
			_popup.PopupPanel = new FlyoutPopupPanel(this)
			{
				Visibility = Visibility.Collapsed,
				Background = SolidColorBrushHelper.Transparent,
				AutoresizingMask = UIViewAutoresizing.All,
				Frame = new CGRect(CGPoint.Empty, ViewHelper.GetScreenSize())
			};
		}
	}
}
