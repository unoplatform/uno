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

namespace Microsoft.UI.Xaml.Input
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

			var deviceType = nativeTouch.Type.ToPointerDeviceType();
			var isInContact = _nativeTouch.Phase == UITouchPhase.Began
				|| _nativeTouch.Phase == UITouchPhase.Moved
				|| _nativeTouch.Phase == UITouchPhase.Stationary;

			FrameId = PointerHelpers.ToFrameId(_nativeTouch.Timestamp);
			Pointer = new Pointer(pointerId, deviceType, isInContact, isInRange: true);
			KeyModifiers = VirtualKeyModifiers.None;
			OriginalSource = originalSource;

			_properties = GetProperties(); // Make sure to capture the properties state so we can re-use them in "mixed" ctor
		}

		public PointerPoint GetCurrentPoint(UIElement relativeTo)
		{
			var timestamp = PointerHelpers.ToTimeStamp(_nativeTouch.Timestamp);
			var device = global::Windows.Devices.Input.PointerDevice.For((global::Windows.Devices.Input.PointerDeviceType)Pointer.PointerDeviceType);
			var rawPosition = (Point)_nativeTouch.GetPreciseLocation(null);
			var position = relativeTo == null
				? rawPosition
				: (Point)_nativeTouch.GetPreciseLocation(relativeTo);

			return new PointerPoint(FrameId, timestamp, device, Pointer.PointerId, rawPosition, position, Pointer.IsInContact, _properties);
		}

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
	}
}
