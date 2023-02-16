#if !HAS_UI_TESTS
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Uno.Disposables;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Media;
using CoreGraphics;
using Uno.UI.Xaml.Media;
#if __IOS__
using UIKit;
using _Image = UIKit.UIImage;
#elif __MACOS__
using AppKit;
using _Image = AppKit.NSImage;
#endif

namespace Microsoft.UI.Xaml.Controls
{
	public partial class Border
	{
		protected override void OnAfterArrange()
		{
			base.OnAfterArrange();
			UpdateBorder();
		}

		partial void OnChildChangedPartial(UIElement previousValue, UIElement newValue)
		{
			previousValue?.RemoveFromSuperview();

			if (newValue != null)
			{
				AddSubview(newValue);
			}

			UpdateBorder();
		}

		internal event EventHandler BoundsPathUpdated;

		internal CGPath BoundsPath { get; private set; }

		bool ICustomClippingElement.AllowClippingToLayoutSlot => CornerRadius == CornerRadius.None && (!(Child is UIElement ue) || ue.RenderTransform == null);
		bool ICustomClippingElement.ForceClippingToLayoutSlot => false;
	}
}
#endif
