using System;
using System.Collections.Generic;
using System.Text;
using Windows.Devices.Input;
using AppKit;
using Foundation;
using Windows.UI.Input;

namespace Windows.UI.Xaml.Input
{
	partial class PointerRoutedEventArgs
	{
		private readonly NSEvent _nativeEvent;
		private readonly NSSet _nativeTouches;
		private readonly ulong _pseudoTimestamp = (ulong)DateTime.UtcNow.Ticks;

		internal PointerRoutedEventArgs(NSSet touches, NSEvent nativeEvent) : this()
		{
			_nativeEvent = nativeEvent;
			_nativeTouches = touches;
			Pointer = new Pointer(nativeEvent);
			CanBubbleNatively = true;
		}


		public PointerPoint GetCurrentPoint(UIElement relativeTo)
		{
			var device = PointerDevice.For(PointerDeviceType.Mouse);
			var point = relativeTo.ConvertPointFromView(_nativeEvent.LocationInWindow, null);
			var properties = new PointerPointProperties() { IsInRange = true, IsPrimary = true };

			return new PointerPoint(0, _pseudoTimestamp, device, 0, point, point, true, properties);
		}
	}
}
