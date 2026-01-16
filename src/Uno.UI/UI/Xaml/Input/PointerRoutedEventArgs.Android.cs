using System;
using System.Collections.Generic;
using System.Text;
using Android.OS;
using Android.Views;
using Uno.Extensions;
using Uno.UI;
using Uno.UI.Xaml.Extensions;
using Windows.Foundation;
using Windows.System;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;

using Microsoft.UI.Input;

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

		// _lastNativeEvent.lastArgs is not necessary equal to LastPointerEvent. _lastNativeEvent.lastArgs is
		// the last PointerRoutedEventArgs that was created as part of the native bubbling of _lastNativeEvent.nativeEvent.
		// In other words, if a PointerRoutedEventArgs was created in managed (using the parameterless constructor),
		// then _lastNativeEvent.lastArgs and LastPointerEvent will diverge.
		private static (MotionEvent nativeEvent, PointerRoutedEventArgs lastArgs)? _lastNativeEvent;

		private readonly int _pointerIndex;
		private readonly ulong _timestamp;
		private readonly float _x;
		private readonly float _y;
		private readonly UIElement _receiver;
		private readonly PointerPointProperties _properties;

		internal bool HasPressedButton => _properties.HasPressedButton;

		internal PointerRoutedEventArgs(MotionEvent nativeEvent, int pointerIndex, UIElement originalSource, UIElement receiver) : this()
		{
			_pointerIndex = pointerIndex;
			_receiver = receiver;

			// NOTE: do not keep a reference to nativeEvent, which will be reused by android's native event bubbling and will be mutated as it
			// goes up through the visual tree. Instead, get whatever values you need here and keep them in fields.
			_timestamp = ToTimestamp(nativeEvent.EventTime);
			_x = nativeEvent.GetX(pointerIndex);
			_y = nativeEvent.GetY(pointerIndex);

			var pointerId = PointerHelpers.GetPointerId(nativeEvent, pointerIndex);
			var nativePointerAction = nativeEvent.Action;
			var nativePointerButtons = nativeEvent.ButtonState;
			var nativePointerType = nativeEvent.GetToolType(_pointerIndex);
			var pointerType = nativePointerType.ToPointerDeviceType();
			var basePointerType = (PointerDeviceType)pointerType;
			var isInContact = PointerHelpers.IsInContact(nativeEvent, pointerType, nativePointerAction, nativePointerButtons);
			var keys = nativeEvent.MetaState.ToVirtualKeyModifiers();

			FrameId = (uint)nativeEvent.EventTime;
			Pointer = new Pointer(pointerId, basePointerType, isInContact, isInRange: true);
			KeyModifiers = keys;
			OriginalSource = originalSource;

			// On platforms with managed pointers, we reuse the same PointerRoutedEventArgs instance
			// as we bubble up the event in the visual tree. On Android, the event bubbling is done
			// natively and we create a corresponding (new) managed PointerRoutedEventArgs instance
			// for each element up the tree. This means that parents won't see modifications in the
			// PointerRoutedEventArgs instance that were done by the children. We have to detect
			// this and copy the relevant fields ourselves.
			if (_lastNativeEvent?.nativeEvent == nativeEvent)
			{
				GestureEventsAlreadyRaised = _lastNativeEvent.Value.lastArgs.GestureEventsAlreadyRaised;
			}
			_lastNativeEvent = (nativeEvent, this);

			var inputManager = VisualTree.GetContentRootForElement(originalSource)?.InputManager;
			if (inputManager is not null)
			{
				inputManager.LastInputDeviceType = basePointerType switch
				{
					PointerDeviceType.Mouse => InputDeviceType.Mouse,
					PointerDeviceType.Pen => InputDeviceType.Pen,
					_ => InputDeviceType.Touch
				};
			}

			_properties = new(PointerHelpers.GetProperties(nativeEvent, pointerIndex, nativePointerType, nativePointerAction, nativePointerButtons, isInRange: true, isInContact)); // Last: we need the Pointer property to be set!
		}

		public PointerPoint GetCurrentPoint(UIElement relativeTo)
		{
			var device = global::Windows.Devices.Input.PointerDevice.For((global::Windows.Devices.Input.PointerDeviceType)Pointer.PointerDeviceType);
			var (rawPosition, position) = GetPositions(relativeTo);

			return new PointerPoint(FrameId, _timestamp, device, Pointer.PointerId, rawPosition, position, Pointer.IsInContact, _properties);
		}

		private (Point raw, Point relative) GetPositions(UIElement relativeTo)
		{
			Point raw, relative;
			if (relativeTo == null) // Relative to the window
			{
				var windowToReceiver = new int[2];
				_receiver.GetLocationInWindow(windowToReceiver);

				relative = new Point(_x + windowToReceiver[0], _y + windowToReceiver[1]).PhysicalToLogicalPixels();
			}
			else if (relativeTo == _receiver) // Fast path
			{
				relative = new Point(_x, _y).PhysicalToLogicalPixels();
			}
			else
			{
				var posRelToReceiver = new Point(_x, _y).PhysicalToLogicalPixels();
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

				raw = new Point(_x + screenToReceiver[0], _y + screenToReceiver[1]).PhysicalToLogicalPixels();
			}

			return (raw, relative);
		}

		#region Misc static helpers

		private static readonly ulong _unixEpochMs = (ulong)(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc) - new DateTime()).TotalMilliseconds;

		private static ulong ToTimestamp(long uptimeMillis)
		{
			// Timestamp is in microseconds
			if (FeatureConfiguration.PointerRoutedEventArgs.AllowRelativeTimeStamp)
			{
				return (ulong)(uptimeMillis * 1000);
			}
			else
			{
				// We cannot cache the "bootTime" as the "uptimeMillis" is frozen while in deep sleep
				// (cf. https://developer.android.com/reference/android/os/SystemClock)

				var sleepTime = SystemClock.ElapsedRealtime() - SystemClock.UptimeMillis();
				var realUptime = (ulong)(uptimeMillis + sleepTime);
				return realUptime * 1000;
			}
		}
		#endregion
	}
}
