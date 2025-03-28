#if __IOS__ || __ANDROID__
using System;
using System.Collections.Generic;
using System.Text;
#if __IOS__
using View = UIKit.UIView;
#else
using Android.Views;
#endif
using Uno.UI;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	partial class FlyoutBasePopupPanel
	{
		protected override View NativeAnchor => _flyout.NativeTarget;
	}
}

#endif
