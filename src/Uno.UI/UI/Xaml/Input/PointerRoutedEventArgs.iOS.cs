using System;
using System.Collections.Generic;
using System.Text;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.System;
using Foundation;
using UIKit;
using Windows.UI.Input;

namespace Windows.UI.Xaml.Input
{
	partial class PointerRoutedEventArgs
	{
		private readonly UITouch _nativeTouch;
		private readonly UIEvent _nativeEvent;

		/// <summary>
		/// DO NOT USE - LEGACY SUPPORT - Will be removed soon
		/// </summary>
		internal PointerRoutedEventArgs(UIElement receiver)
		{
			Pointer = new Pointer(0, PointerDeviceType.Touch, true, isInRange: true);
			KeyModifiers = VirtualKeyModifiers.None;
			OriginalSource = receiver;
			CanBubbleNatively = true; // Required for gesture recognition, and integration of native components in the visual tree
		}

		internal PointerRoutedEventArgs(NSSet touches, UIEvent nativeEvent, UIElement receiver)
		{
			_nativeTouch = (UITouch)touches.AnyObject;
			_nativeEvent = nativeEvent;

			var pointerId = (uint)_nativeTouch.Type;
			var type = _nativeTouch.Type == UITouchType.Stylus
				? PointerDeviceType.Pen
				: PointerDeviceType.Touch;
			var isInContact = _nativeTouch.Phase == UITouchPhase.Began
				|| _nativeTouch.Phase == UITouchPhase.Moved
				|| _nativeTouch.Phase == UITouchPhase.Stationary;
			
			Pointer = new Pointer(pointerId, type, isInContact, isInRange: true);
			KeyModifiers = VirtualKeyModifiers.None;
			OriginalSource = _nativeTouch.View as UIElement ?? receiver; // TODO: walk the tree
			CanBubbleNatively = true; // Required for gesture recognition, and integration of native components in the visual tree
		}

		public PointerPoint GetCurrentPoint(UIElement relativeTo)
		{
			var frameId = ToFrameId(_nativeTouch.Timestamp);
			var timestamp = ToTimeStamp(_nativeTouch.Timestamp);
			var device = PointerDevice.For(Pointer.PointerDeviceType);
			var position = (Point)_nativeTouch.LocationInView(relativeTo);
			var properties = GetProperties();

			return new PointerPoint(frameId, timestamp, device, Pointer.PointerId, position, Pointer.IsInContact, properties);
		}

		private PointerPointProperties GetProperties()
			=> new PointerPointProperties()
			{
				IsPrimary = true,
				IsInRange = Pointer.IsInRange,
				IsLeftButtonPressed = Pointer.IsInContact // TODO: Pen
			};

		#region Misc static helpers
		private static long? _bootTime;

		private static ulong ToTimeStamp(double timestamp)
		{
			if (!_bootTime.HasValue)
			{
				_bootTime = DateTime.UtcNow.Ticks - (long)(TimeSpan.TicksPerSecond * new NSProcessInfo().SystemUptime);
			}

			return (ulong)_bootTime.Value + (ulong)(TimeSpan.TicksPerSecond * timestamp);
		}

		private static uint ToFrameId(double timestamp)
		{
			// The precision of the frameId is 10 frame per ms ... which should be enough
			return (uint)(timestamp * 1000.0 * 10.0);
		}
		#endregion
	}
}
