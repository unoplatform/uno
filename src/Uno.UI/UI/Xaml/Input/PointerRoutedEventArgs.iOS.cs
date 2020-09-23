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
		internal PointerRoutedEventArgs(UIElement receiver) : this()
		{
			Pointer = new Pointer(0, PointerDeviceType.Touch, true, isInRange: true);
			KeyModifiers = VirtualKeyModifiers.None;
			OriginalSource = receiver;
			CanBubbleNatively = true; // Required for native gesture recognition (i.e. ScrollViewer), and integration of native components in the visual tree
		}

		internal PointerRoutedEventArgs(uint pointerId, UITouch nativeTouch, UIEvent nativeEvent, UIElement receiver) : this()
		{
			_nativeTouch = nativeTouch;
			_nativeEvent = nativeEvent;

			var deviceType = GetPointerDeviceType(nativeTouch.Type);
						
			var isInContact = _nativeTouch.Phase == UITouchPhase.Began
				|| _nativeTouch.Phase == UITouchPhase.Moved
				|| _nativeTouch.Phase == UITouchPhase.Stationary;

			FrameId = ToFrameId(_nativeTouch.Timestamp);
			Pointer = new Pointer(pointerId, deviceType, isInContact, isInRange: true);
			KeyModifiers = VirtualKeyModifiers.None;
			OriginalSource = FindOriginalSource(_nativeTouch) ?? receiver;
		}

		public PointerPoint GetCurrentPoint(UIElement relativeTo)
		{
			var timestamp = ToTimeStamp(_nativeTouch.Timestamp);
			var device = PointerDevice.For(Pointer.PointerDeviceType);
			var rawPosition = (Point)_nativeTouch.GetPreciseLocation(null);
			var position = relativeTo == null
				? rawPosition
				: (Point)_nativeTouch.GetPreciseLocation(relativeTo);
			var properties = GetProperties();

			return new PointerPoint(FrameId, timestamp, device, Pointer.PointerId, rawPosition, position, Pointer.IsInContact, properties);
		}

		private PointerDeviceType GetPointerDeviceType(UITouchType touchType) =>
			touchType switch
			{				
				UITouchType.Stylus => PointerDeviceType.Pen,
				UITouchType.IndirectPointer => PointerDeviceType.Mouse,
				UITouchType.Indirect => PointerDeviceType.Mouse,
				_ => PointerDeviceType.Touch // Use touch as default fallback.
			};

		private PointerPointProperties GetProperties()
			=> new PointerPointProperties()
			{
				IsPrimary = true,
				IsInRange = Pointer.IsInRange,
				IsLeftButtonPressed = Pointer.IsInContact,
				Pressure = (float)(_nativeTouch.Force / _nativeTouch.MaximumPossibleForce)
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

		private static UIElement FindOriginalSource(UITouch touch)
		{
			var view = touch.View;
			while (view != null)
			{
				if (view is UIElement elt)
				{
					return elt;
				}

				view = view.Superview;
			}

			return null;
		}
		#endregion
	}
}
