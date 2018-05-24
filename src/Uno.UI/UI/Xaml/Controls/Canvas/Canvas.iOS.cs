using System;
using Foundation;

#if XAMARIN_IOS_UNIFIED
using UIKit;
using Uno.UI.Extensions;
using Windows.UI.Xaml.Input;
#elif XAMARIN_IOS
using MonoTouch.UIKit;
using CGRect = System.Drawing.RectangleF;
using nfloat = System.Single;
using CGPoint = System.Drawing.PointF;
using nint = System.Int32;
using CGSize = System.Drawing.SizeF;
#endif

namespace Windows.UI.Xaml.Controls
{
	public partial class Canvas : Panel
	{
		// TODO: Implement properly in UIElement

		public new event PointerEventHandler PointerPressed;
		
		public override void TouchesBegan(NSSet touches, UIEvent evt)
		{
			if (evt.IsTouchInView(this))
			{
				PointerPressed?.Invoke(this, new PointerRoutedEventArgs());
			}
		}
	}
}

