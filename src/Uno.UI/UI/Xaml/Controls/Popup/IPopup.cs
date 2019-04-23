using System;
using System.Collections.Generic;
using System.Text;
#if XAMARIN_ANDROID
using View = Android.Views.View;
#elif XAMARIN_IOS
using View = UIKit.UIView;
#elif __MACOS__
using View = AppKit.NSView;
#else
using View = Windows.UI.Xaml.FrameworkElement;
#endif

namespace Windows.UI.Xaml.Controls
{
	internal interface IPopup
	{
		event EventHandler<object> Closed;
		event EventHandler<object> Opened;

		bool IsOpen { get; set; }
		View Child { get; set; }

	}
}
