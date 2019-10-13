using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Windows.Devices.Input;
using AppKit;
using Foundation;
using Windows.UI.Input;

namespace Windows.UI.Xaml.Input
{
	partial class PointerRoutedEventArgs
	{
		private static long _pseudoNextFrameId;
		private readonly uint _pseudoFrameId = (uint)Interlocked.Increment(ref _pseudoNextFrameId);
		private readonly ulong _pseudoTimestamp = (ulong)DateTime.UtcNow.Ticks;

		private readonly NSEvent _nativeEvent;
		private readonly NSSet _nativeTouches;

		internal PointerRoutedEventArgs(NSSet touches, NSEvent nativeEvent) : this()
		{
			_nativeEvent = nativeEvent;
			_nativeTouches = touches;

			FrameId = _pseudoFrameId;
			Pointer = new Pointer(nativeEvent);
			CanBubbleNatively = true;
		}


		public PointerPoint GetCurrentPoint(UIElement relativeTo)
		{
			var device = PointerDevice.For(PointerDeviceType.Mouse);
			var point = relativeTo.ConvertPointFromView(_nativeEvent.LocationInWindow, null);
			var properties = new PointerPointProperties() { IsInRange = true, IsPrimary = true };

			return new PointerPoint(FrameId, _pseudoTimestamp, device, 0, point, point, true, properties);
		}

		#region Misc static helpers
		private static uint ToFrameId(double timestamp)
		{
			// The precision of the frameId is 10 frame per ms ... which should be enough
			return (uint)(timestamp * 1000.0 * 10.0);
		}
		#endregion
	}
}
