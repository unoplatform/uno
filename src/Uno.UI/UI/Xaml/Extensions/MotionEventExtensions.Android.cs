#nullable enable

using Android.OS;
using Android.Views;
using Windows.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Input;
using Uno.UI;

namespace Windows.UI.Xaml.Extensions;

internal static class MotionEventExtensions
{
	/// <summary>
	/// The stylus is pressed while holding the barrel button
	/// </summary>
	internal const MotionEventActions StylusWithBarrelDown = (MotionEventActions)211;
	/// <summary>
	/// The stylus is moved after having been pressed while holding the barrel button
	/// </summary>
	internal const MotionEventActions StylusWithBarrelMove = (MotionEventActions)213;
	/// <summary>
	/// The stylus is released after having been pressed while holding the barrel button
	/// </summary>
	internal const MotionEventActions StylusWithBarrelUp = (MotionEventActions)212;

	private static readonly ulong _unixEpochMs = (ulong)(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc) - new DateTime()).TotalMilliseconds;

	private const int _pointerIdsCount = (int)MotionEventActions.PointerIndexMask >> (int)MotionEventActions.PointerIndexShift; // 0xff
	private const int _pointerIdsShift = 31 - (int)MotionEventActions.PointerIndexShift; // 23

	public static PointerEventArgs ToPointerEventArgs(this MotionEvent nativeEvent, int pointerIndex)
	{
		var pointerType = nativeEvent.GetToolType(pointerIndex).ToPointerDeviceType();

		var device = Windows.Devices.Input.PointerDevice.For((Windows.Devices.Input.PointerDeviceType)pointerType);
		var pointerId = ((uint)nativeEvent.GetPointerId(pointerIndex) & _pointerIdsCount) << _pointerIdsShift | (uint)nativeEvent.DeviceId;
		var phyX = nativeEvent.GetX(pointerIndex);
		var phyY = nativeEvent.GetY(pointerIndex);
		var position = new Point(phyX, phyY).PhysicalToLogicalPixels();
		var isInContact = GetIsInContact(nativeEvent, (PointerDeviceType)pointerType);
		var currentPoint = new PointerPoint(
			frameId: (uint)nativeEvent.EventTime,
			timestamp: ToTimeStamp(nativeEvent.EventTime),
			device,
			pointerId,
			// TODO: Confirm position calculation.
			rawPosition: position, // TODO: Should be relative to the screen
			position: position,
			isInContact,
			GetProperties(nativeEvent.GetToolType(pointerIndex), nativeEvent, isInContact)
		);

		return new PointerEventArgs(currentPoint, nativeEvent.MetaState.ToVirtualKeyModifiers(), nativeEvent);
	}

	public static PointerDeviceType ToPointerDeviceType(this MotionEventToolType nativeType)
	{
		switch (nativeType)
		{
			case Android.Views.MotionEventToolType.Eraser:
			case Android.Views.MotionEventToolType.Stylus:
				return PointerDeviceType.Pen;
			case Android.Views.MotionEventToolType.Finger:
				return PointerDeviceType.Touch;
			case Android.Views.MotionEventToolType.Mouse:
				return PointerDeviceType.Mouse;
			case Android.Views.MotionEventToolType.Unknown: // used by Xamarin.UITest
			default:
				return default(PointerDeviceType);
		}
	}

	public static VirtualKeyModifiers ToVirtualKeyModifiers(this MetaKeyStates nativeKeys)
	{
		var keys = VirtualKeyModifiers.None;

		if ((nativeKeys & MetaKeyStates.ShiftMask) != 0)
		{
			keys |= VirtualKeyModifiers.Shift;
		}
		if ((nativeKeys & MetaKeyStates.CtrlMask) != 0)
		{
			keys |= VirtualKeyModifiers.Control;
		}
		if ((nativeKeys & MetaKeyStates.MetaMask) != 0)
		{
			keys |= VirtualKeyModifiers.Windows;
		}

		return keys;
	}

	private static readonly Dictionary<MotionEventButtonState, PointerUpdateKind> _none = new(0);
	private static readonly Dictionary<MotionEventButtonState, PointerUpdateKind> _fingerDownUpdates = new()
	{
		{ 0, PointerUpdateKind.LeftButtonPressed },
		{ MotionEventButtonState.Primary, PointerUpdateKind.LeftButtonPressed }
	};
	private static readonly Dictionary<MotionEventButtonState, PointerUpdateKind> _fingerUpUpdates = new()
	{
		{ 0, PointerUpdateKind.LeftButtonReleased },
		{ MotionEventButtonState.Primary, PointerUpdateKind.LeftButtonReleased }
	};
	private static readonly Dictionary<MotionEventButtonState, PointerUpdateKind> _mouseDownUpdates = new()
	{
		{ MotionEventButtonState.Primary, PointerUpdateKind.LeftButtonPressed },
		{ MotionEventButtonState.Tertiary, PointerUpdateKind.MiddleButtonPressed },
		{ MotionEventButtonState.Secondary, PointerUpdateKind.RightButtonPressed }
	};
	private static readonly Dictionary<MotionEventButtonState, PointerUpdateKind> _mouseUpUpdates = new()
	{
		{ MotionEventButtonState.Primary, PointerUpdateKind.LeftButtonReleased },
		{ MotionEventButtonState.Tertiary, PointerUpdateKind.MiddleButtonReleased },
		{ MotionEventButtonState.Secondary, PointerUpdateKind.RightButtonReleased }
	};

	private static PointerPointProperties GetProperties(MotionEventToolType type, MotionEvent nativeEvent, bool isInContact)
	{
		var props = new PointerPointProperties
		{
			IsPrimary = true,
			IsInRange = true,
		};

		var action = nativeEvent.Action;
		var buttons = nativeEvent.ButtonState;
		var pointerIndex = nativeEvent.ActionIndex;
		var isDown = action == /* 0 = */ MotionEventActions.Down || action.HasFlag(MotionEventActions.PointerDown);
		var isUp = action.HasFlag(MotionEventActions.Up) || action.HasFlag(MotionEventActions.PointerUp);
		var updates = _none;
		switch (type)
		{
			case MotionEventToolType.Finger:
			case MotionEventToolType.Unknown: // used by Xamarin.UITest
				props.IsLeftButtonPressed = isInContact;
				updates = isDown ? _fingerDownUpdates : isUp ? _fingerUpUpdates : _none;
				// Pressure = .5f => Keeps default as UWP returns .5 for fingers.
				break;

			case MotionEventToolType.Mouse:
				props.IsLeftButtonPressed = buttons.HasFlag(MotionEventButtonState.Primary);
				props.IsMiddleButtonPressed = buttons.HasFlag(MotionEventButtonState.Tertiary);
				props.IsRightButtonPressed = buttons.HasFlag(MotionEventButtonState.Secondary);
				updates = isDown ? _mouseDownUpdates : isUp ? _mouseUpUpdates : _none;
				// Pressure = .5f => Keeps default as UWP returns .5 for Mouse no matter is button is pressed or not (Android return 1.0 while pressing a button, but 0 otherwise).
				break;

			// Note: On UWP, if you touch screen while already holding the barrel button, you will get a right + barrel,
			//		 ** BUT ** if you touch screen and THEN press the barrel button props will be left + barrel until released.
			//		 On Android this distinction seems to be flagged by the "1101 ****" action flag (i.e. "StylusWithBarrel***" actions),
			//		 so here we set the Is<Left|Right>ButtonPressed based on the action and we don't try to link it to the barrel button state.
			case MotionEventToolType.Stylus when action == StylusWithBarrelDown:
			case MotionEventToolType.Stylus when action == StylusWithBarrelMove:
			case MotionEventToolType.Stylus when action == StylusWithBarrelUp:
				// Note: We still validate the "IsButtonPressed(StylusPrimary)" as the user might release the button while pressed.
				//		 In that case we will still receive moves and up with the "StylusWithBarrel***" actions.
				props.IsBarrelButtonPressed = buttons.HasFlag(MotionEventButtonState.StylusPrimary);
				props.IsRightButtonPressed = isInContact;
				props.Pressure = Math.Min(1f, nativeEvent.GetPressure(pointerIndex)); // Might exceed 1.0 on Android
				break;
			case MotionEventToolType.Stylus:
				props.IsBarrelButtonPressed = buttons.HasFlag(MotionEventButtonState.StylusPrimary);
				props.IsLeftButtonPressed = isInContact;
				props.Pressure = Math.Min(1f, nativeEvent.GetPressure(pointerIndex)); // Might exceed 1.0 on Android
				break;
			case MotionEventToolType.Eraser:
				props.IsEraser = true;
				props.Pressure = Math.Min(1f, nativeEvent.GetPressure(pointerIndex)); // Might exceed 1.0 on Android
				break;

			default:
				break;
		}

		if (Android.OS.Build.VERSION.SdkInt >= BuildVersionCodes.M // ActionButton was introduced with API 23 (https://developer.android.com/reference/android/view/MotionEvent.html#getActionButton())
			&& updates.TryGetValue(nativeEvent.ActionButton, out var update))
		{
			props.PointerUpdateKind = update;
		}

		return props;
	}

	private static bool GetIsInContact(MotionEvent nativeEvent, PointerDeviceType pointerType)
	{
		switch (pointerType)
		{
			case PointerDeviceType.Mouse:
				// For mouse, we cannot only rely on action: We will get a "HoverExit" when we press the left button.
				return nativeEvent.ButtonState != 0;
			case PointerDeviceType.Pen:
				return nativeEvent.GetAxisValue(Axis.Distance, nativeEvent.ActionIndex) == 0;
			default:
			case PointerDeviceType.Touch:
				var action = nativeEvent.Action;
				// WARNING: MotionEventActions.Down == 0, so action.HasFlag(MotionEventActions.Up) is always true!
				return !action.HasFlag(MotionEventActions.Up)
					&& !action.HasFlag(MotionEventActions.PointerUp)
					&& !action.HasFlag(MotionEventActions.Cancel);
		}
	}

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
}
