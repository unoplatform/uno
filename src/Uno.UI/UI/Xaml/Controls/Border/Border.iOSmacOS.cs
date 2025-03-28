#if !HAS_UI_TESTS
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Uno.Disposables;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Xaml.Media;
using CoreGraphics;
using Uno.UI.Xaml.Media;
using Uno.UI.Xaml.Controls;

#if __IOS__
using UIKit;
using _Image = UIKit.UIImage;
#elif __MACOS__
using AppKit;
using _Image = AppKit.NSImage;
#endif

namespace Windows.UI.Xaml.Controls;

partial class Border
{
	protected override void OnAfterArrange()
	{
		base.OnAfterArrange();
		BorderRenderer.Update();
	}

	bool ICustomClippingElement.AllowClippingToLayoutSlot => CornerRadius == CornerRadius.None && (!(Child is UIElement ue) || ue.RenderTransform == null);
	bool ICustomClippingElement.ForceClippingToLayoutSlot => false;
}
#endif
