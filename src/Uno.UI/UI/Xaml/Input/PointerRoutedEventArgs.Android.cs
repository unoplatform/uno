using System;
using System.Collections.Generic;
using System.Text;
using Windows.Devices.Input;
using Android.Views;
using Uno.UI;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Input;
using Windows.UI.Xaml.Extensions;
using Uno.Extensions;

namespace Windows.UI.Xaml.Input
{
	partial class PointerRoutedEventArgs
	{
		private readonly MotionEvent _nativeEvent;

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

		internal PointerRoutedEventArgs(MotionEvent nativeEvent, UIElement receiver)
		{
			_nativeEvent = nativeEvent;

			var pointerId = (uint)nativeEvent.DeviceId;
			var type = nativeEvent.GetToolType(0).ToPointerDeviceType();
			var isInContact = nativeEvent.Action.HasFlag(MotionEventActions.Down)
				|| nativeEvent.Action.HasFlag(MotionEventActions.Move);
			var keys = nativeEvent.MetaState.ToVirtualKeyModifiers();

			Pointer = new Pointer(pointerId, type, isInContact, isInRange: true);
			KeyModifiers = keys;
			OriginalSource = receiver;
			CanBubbleNatively = true; // Required for gesture recognition, and integration of native components in the visual tree
		}

		public PointerPoint GetCurrentPoint(UIElement relativeTo)
		{
			var frameId = (uint)_nativeEvent.EventTime;
			var timestamp = ToTimeStamp(_nativeEvent.EventTime);
			var device = PointerDevice.For(Pointer.PointerDeviceType);
			var position = GetPosition(relativeTo);
			var properties = GetProperties();

			return new PointerPoint(frameId, timestamp, device, Pointer.PointerId, position, Pointer.IsInContact, properties);
		}

		private Point GetPosition(UIElement relativeTo)
		{
			int xOrigin = 0, yOrigin = 0;
			if (relativeTo != null)
			{
				var viewCoords = new int[2];
				relativeTo.GetLocationInWindow(viewCoords);
				xOrigin = viewCoords[0];
				yOrigin = viewCoords[1];
			}
			var physicalPoint = new Point(_nativeEvent.RawX - xOrigin, _nativeEvent.RawY - yOrigin);
			var logicalPoint = physicalPoint.PhysicalToLogicalPixels();

			return logicalPoint;
		}

		private PointerPointProperties GetProperties()
		{
			var props = new PointerPointProperties
			{
				IsPrimary = true,
				IsInRange = Pointer.IsInRange
			};

			var type = _nativeEvent.GetToolType(0);
			var action = _nativeEvent.Action;
			var isDown = action.HasFlag(MotionEventActions.Down);
			var isUp = action.HasFlag(MotionEventActions.Up);
			var updates = _none;
			switch (type)
			{
				case MotionEventToolType.Finger:
					props.IsLeftButtonPressed = Pointer.IsInContact;
					updates = isDown ? _fingerDownUpdates : isUp ? _fingerUpUpdates : _none;
					break;
				case MotionEventToolType.Mouse:
					props.IsLeftButtonPressed = _nativeEvent.IsButtonPressed(MotionEventButtonState.Primary);
					props.IsMiddleButtonPressed = _nativeEvent.IsButtonPressed(MotionEventButtonState.Tertiary);
					props.IsRightButtonPressed = _nativeEvent.IsButtonPressed(MotionEventButtonState.Secondary);
					updates = isDown ? _mouseDownUpdates : isUp ? _mouseUpUpdates : _none;
					break;
				case MotionEventToolType.Stylus:
					props.IsBarrelButtonPressed = _nativeEvent.IsButtonPressed(MotionEventButtonState.StylusPrimary);
					props.IsLeftButtonPressed = !props.IsBarrelButtonPressed;
					break;
				case MotionEventToolType.Eraser:
					props.IsEraser = true;
					break;
				case MotionEventToolType.Unknown: // used by Xamarin.UITest
				default:
					break;
			}

			if (updates.TryGetValue(_nativeEvent.ActionButton, out var update))
			{
				props.PointerUpdateKind = update;
			}

			return props;
		}

		#region Misc static helpers
		private static readonly Dictionary<MotionEventButtonState, PointerUpdateKind> _none = new Dictionary<MotionEventButtonState, PointerUpdateKind>(0);
		private static readonly Dictionary<MotionEventButtonState, PointerUpdateKind> _fingerDownUpdates = new Dictionary<MotionEventButtonState, PointerUpdateKind>
		{
			{ MotionEventButtonState.Primary, PointerUpdateKind.LeftButtonPressed }
		};
		private static readonly Dictionary<MotionEventButtonState, PointerUpdateKind> _fingerUpUpdates = new Dictionary<MotionEventButtonState, PointerUpdateKind>
		{
			{ MotionEventButtonState.Primary, PointerUpdateKind.LeftButtonReleased }
		};
		private static readonly Dictionary<MotionEventButtonState, PointerUpdateKind> _mouseDownUpdates = new Dictionary<MotionEventButtonState, PointerUpdateKind>
		{
			{ MotionEventButtonState.Primary, PointerUpdateKind.LeftButtonPressed },
			{ MotionEventButtonState.Tertiary, PointerUpdateKind.MiddleButtonPressed },
			{ MotionEventButtonState.Secondary, PointerUpdateKind.RightButtonPressed }
		};
		private static readonly Dictionary<MotionEventButtonState, PointerUpdateKind> _mouseUpUpdates = new Dictionary<MotionEventButtonState, PointerUpdateKind>
		{
			{ MotionEventButtonState.Primary, PointerUpdateKind.LeftButtonReleased },
			{ MotionEventButtonState.Tertiary, PointerUpdateKind.MiddleButtonReleased },
			{ MotionEventButtonState.Secondary, PointerUpdateKind.RightButtonReleased }
		};

		private static readonly ulong _unixEpochMs = (ulong)(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc) - new DateTime()).TotalMilliseconds;

		private static ulong ToTimeStamp(long uptimeMillis)
		{
			// We cannot cache the "bootTime" as the "uptimeMillis" is frozen while in deep sleep
			// (cf. https://developer.android.com/reference/android/os/SystemClock)

			var sleepTime = Android.OS.SystemClock.ElapsedRealtime() - Android.OS.SystemClock.UptimeMillis();
			var realUptime = (ulong)(uptimeMillis + sleepTime);
			var timestamp = _unixEpochMs + realUptime;

			return timestamp;
		}
		#endregion
	}
}
