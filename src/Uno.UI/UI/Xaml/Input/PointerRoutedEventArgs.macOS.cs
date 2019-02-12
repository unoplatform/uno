using System;
using System.Collections.Generic;
using System.Text;
using AppKit;
using Foundation;
using Windows.UI.Input;

namespace Windows.UI.Xaml.Input
{
	public partial class PointerRoutedEventArgs
	{
		private readonly NSEvent _nativeEvent;
		private readonly NSSet _nativeTouches;

		internal PointerRoutedEventArgs(NSSet touches, NSEvent nativeEvent)
		{
			_nativeEvent = nativeEvent;
			_nativeTouches = touches;
			Pointer = new Pointer(nativeEvent);
			CanBubbleNatively = true;
		}


		public PointerPoint GetCurrentPoint(UIElement relativeTo)
		{
#if __IOS__
			var point = (_nativeTouches.AnyObject as NSTouch).LocationInView(relativeTo);

			return new PointerPoint(point);
#elif __MACOS__
			throw new NotImplementedException();
#endif
		}
	}
}
