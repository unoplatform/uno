using System;
using System.Collections.Generic;
using System.Text;
using Android.OS;
using Android.Views;
using Microsoft.UI.Xaml.Extensions;
using Uno.Extensions;
using Uno.UI;
using Uno.UI.Xaml.Extensions;
using Windows.Foundation;
using Windows.System;

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
		/// <summary>
		/// The stylus is pressed while holding the barrel button
		/// </summary>
		internal const MotionEventActions StylusWithBarrelDown = PointerHelpers.StylusWithBarrelDown;
		/// <summary>
		/// The stylus is moved after having been pressed while holding the barrel button
		/// </summary>
		internal const MotionEventActions StylusWithBarrelMove = PointerHelpers.StylusWithBarrelMove;
		/// <summary>
		/// The stylus is released after having been pressed while holding the barrel button
		/// </summary>
		internal const MotionEventActions StylusWithBarrelUp = PointerHelpers.StylusWithBarrelUp;

		private const int _pointerIdsCount = (int)MotionEventActions.PointerIndexMask >> (int)MotionEventActions.PointerIndexShift; // 0xff
		private const int _pointerIdsShift = 31 - (int)MotionEventActions.PointerIndexShift; // 23

		private readonly MotionEvent _nativeEvent;
		private readonly int _pointerIndex;
		private readonly UIElement _receiver;
		private readonly PointerPointProperties _properties;

		internal bool HasPressedButton => _properties.HasPressedButton;

		internal PointerRoutedEventArgs(MotionEvent nativeEvent, int pointerIndex, UIElement originalSource, UIElement receiver) : this()
		{
			_nativeEvent = nativeEvent;
			_pointerIndex = pointerIndex;
			_receiver = receiver;

			// Here we assume that usually pointerId is 'PointerIndexShift' bits long (8 bits / 255 ids),
			// and that usually the deviceId is [0, something_not_too_big_hopefully_less_than_0x00ffffff].
			// If deviceId is greater than 0x00ffffff, we might have a conflict but only in case of multi touch
			// and with a high variation of deviceId. We assume that's safe enough.
			// Note: Make sure to use the GetPointerId in order to make sure to keep the same id while: down_1 / down_2 / up_1 / up_2
			//		 otherwise up_2 will be with the id of 1
			var pointerId = ((uint)nativeEvent.GetPointerId(pointerIndex) & _pointerIdsCount) << _pointerIdsShift | (uint)nativeEvent.DeviceId;
			var nativePointerAction = nativeEvent.Action;
			var nativePointerButtons = nativeEvent.ButtonState;
			var nativePointerType = nativeEvent.GetToolType(_pointerIndex);
			var pointerType = nativePointerType.ToPointerDeviceType();
			var isInContact = PointerHelpers.IsInContact(nativeEvent, pointerType, nativePointerAction, nativePointerButtons);
			var keys = nativeEvent.MetaState.ToVirtualKeyModifiers();

			FrameId = (uint)_nativeEvent.EventTime;
			Pointer = new Pointer(pointerId, (PointerDeviceType)pointerType, isInContact, isInRange: true);
			KeyModifiers = keys;
			OriginalSource = originalSource;

			_properties = new(PointerHelpers.GetProperties(nativeEvent, pointerIndex, nativePointerType, nativePointerAction, nativePointerButtons, isInRange: true, isInContact)); // Last: we need the Pointer property to be set!
		}

		public PointerPoint GetCurrentPoint(UIElement relativeTo)
		{
			var timestamp = ToTimeStamp(_nativeEvent.EventTime);
			var device = global::Windows.Devices.Input.PointerDevice.For((global::Windows.Devices.Input.PointerDeviceType)Pointer.PointerDeviceType);
			var (rawPosition, position) = GetPositions(relativeTo);

			return new PointerPoint(FrameId, timestamp, device, Pointer.PointerId, rawPosition, position, Pointer.IsInContact, _properties);
		}

		private (Point raw, Point relative) GetPositions(UIElement relativeTo)
		{
			var phyX = _nativeEvent.GetX(_pointerIndex);
			var phyY = _nativeEvent.GetY(_pointerIndex);

			Point raw, relative;
			if (relativeTo == null) // Relative to the window
			{
				var windowToReceiver = new int[2];
				_receiver.GetLocationInWindow(windowToReceiver);

				relative = new Point(phyX + windowToReceiver[0], phyY + windowToReceiver[1]).PhysicalToLogicalPixels();
			}
			else if (relativeTo == _receiver) // Fast path
			{
				relative = new Point(phyX, phyY).PhysicalToLogicalPixels();
			}
			else
			{
				var posRelToReceiver = new Point(phyX, phyY).PhysicalToLogicalPixels();
				var posRelToTarget = UIElement.GetTransform(from: _receiver, to: relativeTo).Transform(posRelToReceiver);

				relative = posRelToTarget;
			}

			// Raw coordinates are relative to the screen (easier for the gesture recognizer to track fingers for manipulations)
			// if (ANDROID > 10)
			// {
			//		var raw = new Point(_nativeEvent.getRawX(_pointerIndex), _nativeEvent.getRawY(_pointerIndex)).PhysicalToLogicalPixels();
			// }
			// else
			{
				var screenToReceiver = new int[2];
				_receiver.GetLocationOnScreen(screenToReceiver);

				raw = new Point(phyX + screenToReceiver[0], phyY + screenToReceiver[1]).PhysicalToLogicalPixels();
			}

			return (raw, relative);
		}

		#region Misc static helpers

		private static readonly ulong _unixEpochMs = (ulong)(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc) - new DateTime()).TotalMilliseconds;

		private static ulong ToTimeStamp(long uptimeMillis)
		{
			if (FeatureConfiguration.PointerRoutedEventArgs.AllowRelativeTimeStamp)
			{
				return (ulong)(TimeSpan.TicksPerMillisecond * uptimeMillis);
			}
			else
			{
				// We cannot cache the "bootTime" as the "uptimeMillis" is frozen while in deep sleep
				// (cf. https://developer.android.com/reference/android/os/SystemClock)

				var sleepTime = Android.OS.SystemClock.ElapsedRealtime() - Android.OS.SystemClock.UptimeMillis();
				var realUptime = (ulong)(uptimeMillis + sleepTime);
				var timestamp = TimeSpan.TicksPerMillisecond * (_unixEpochMs + realUptime);

				return timestamp;
			}
		}
		#endregion
	}
}
