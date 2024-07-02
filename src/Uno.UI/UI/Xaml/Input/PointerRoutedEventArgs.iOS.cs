using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
using Windows.System;
using Foundation;
using UIKit;

#if HAS_UNO_WINUI
using Microsoft.UI.Input;
#else
using Windows.UI.Input;
using Windows.Devices.Input;
#endif

namespace Windows.UI.Xaml.Input
{
	partial class PointerRoutedEventArgs
	{
		private readonly UITouch _nativeTouch;
		private readonly UIEvent _nativeEvent;

		private readonly PointerPointProperties _properties;

		/// <summary>
		/// Creates an hybrid event args which reports the <paramref name="current"/> position, time and original source,
		/// while reporting the state of the <paramref name="previous"/> args (pressed buttons, key modifiers, etc.).
		/// </summary>
		/// <remarks>
		/// This has a very specific usage and should be used cautiously!
		/// </remarks>
		internal PointerRoutedEventArgs(PointerRoutedEventArgs previous, PointerRoutedEventArgs current)
		{
			_nativeTouch = current._nativeTouch;
			_nativeEvent = current._nativeEvent;

			FrameId = current.FrameId;
			Pointer = previous.Pointer;
			KeyModifiers = previous.KeyModifiers;
			OriginalSource = current.OriginalSource;

			_properties = previous._properties;
		}

		internal PointerRoutedEventArgs(uint pointerId, UITouch nativeTouch, UIEvent nativeEvent, UIElement originalSource) : this()
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
			OriginalSource = originalSource;

			_properties = GetProperties(); // Make sure to capture the properties state so we can re-use them in "mixed" ctor
		}

		public PointerPoint GetCurrentPoint(UIElement relativeTo)
		{
			var timestamp = ToTimeStamp(_nativeTouch.Timestamp);
			var device = global::Windows.Devices.Input.PointerDevice.For((global::Windows.Devices.Input.PointerDeviceType)Pointer.PointerDeviceType);
			var rawPosition = (Point)_nativeTouch.GetPreciseLocation(null);
			var position = relativeTo == null
				? rawPosition
				: (Point)_nativeTouch.GetPreciseLocation(relativeTo);

			return new PointerPoint(FrameId, timestamp, device, Pointer.PointerId, rawPosition, position, Pointer.IsInContact, _properties);
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
			=> new()
			{
				IsPrimary = true,
				IsInRange = Pointer.IsInRange,
				IsLeftButtonPressed = Pointer.IsInContact,
				Pressure = (float)(_nativeTouch.Force / _nativeTouch.MaximumPossibleForce),
				PointerUpdateKind = _nativeTouch.Phase switch
				{
					UITouchPhase.Began => PointerUpdateKind.LeftButtonPressed,
					UITouchPhase.Ended => PointerUpdateKind.LeftButtonReleased,
					_ => PointerUpdateKind.Other
				}
			};

		#region Misc static helpers
		private static long? _bootTime;

		private static ulong ToTimeStamp(double timestamp)
		{
			_bootTime ??= DateTime.UtcNow.Ticks - (long)(TimeSpan.TicksPerSecond * new NSProcessInfo().SystemUptime);

			return (ulong)_bootTime.Value + (ulong)(TimeSpan.TicksPerSecond * timestamp);
		}

		private static double? _firstTimestamp;

		private static uint ToFrameId(double timestamp)
		{
			_firstTimestamp ??= timestamp;

			var relativeTimestamp = timestamp - _firstTimestamp;
			var frameId = relativeTimestamp * 120.0; // we allow a precision of 120Hz (8.333 ms per frame)

			// When we cast, we are not overflowing but instead capping to uint.MaxValue.
			// We use modulo to make sure to reset to 0 in that case (1.13 years of app run-time, but we prefer to be safe).
			return (uint)(frameId % uint.MaxValue);
		}
		#endregion
	}
}
