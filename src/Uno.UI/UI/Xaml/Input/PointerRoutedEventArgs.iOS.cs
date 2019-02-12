using System;
using System.Collections.Generic;
using System.Text;
using Foundation;
using UIKit;
using Windows.UI.Input;

namespace Windows.UI.Xaml.Input
{
	public partial class PointerRoutedEventArgs
	{
		private readonly UIEvent _nativeEvent;
		private readonly NSSet _nativeTouches;

		internal PointerRoutedEventArgs(NSSet touches, UIEvent nativeEvent)
		{
			_nativeEvent = nativeEvent;
			_nativeTouches = touches;
			Pointer = new Pointer(nativeEvent);
			CanBubbleNatively = true;
		}


		public PointerPoint GetCurrentPoint(UIElement relativeTo)
		{
			var point = (_nativeTouches.AnyObject as UITouch).LocationInView(relativeTo);

			return new PointerPoint(point);
		}
	}
}
