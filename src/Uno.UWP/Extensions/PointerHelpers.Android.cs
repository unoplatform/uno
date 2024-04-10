using System;
using System.Collections.Generic;
using Android.OS;
using Android.Views;
using Windows.Devices.Input;
using Windows.UI.Input;

namespace Microsoft.UI.Xaml.Extensions;

internal static class PointerHelpers
{
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

	internal static bool IsInContact(MotionEvent nativeEvent, PointerDeviceType pointerType, MotionEventActions action, MotionEventButtonState buttons)
	{
		switch (pointerType)
		{
			case PointerDeviceType.Mouse:
				// For mouse, we cannot only rely on action: We will get a "HoverExit" when we press the left button.
				return buttons != 0;

			case PointerDeviceType.Pen:
				return nativeEvent.GetAxisValue(Axis.Distance, nativeEvent.ActionIndex) == 0;

			default:
			case PointerDeviceType.Touch:
				// WARNING: MotionEventActions.Down == 0, so action.HasFlag(MotionEventActions.Up) is always true!
				return !action.HasFlag(MotionEventActions.Up)
					&& !action.HasFlag(MotionEventActions.PointerUp)
					&& !action.HasFlag(MotionEventActions.Cancel);
		}
	}

	internal static PointerPointProperties GetProperties(MotionEvent nativeEvent, int pointerIndex, MotionEventToolType type, MotionEventActions action, MotionEventButtonState buttons, bool isInRange, bool isInContact)
	{
		var props = new PointerPointProperties
		{
			IsPrimary = true,
			IsInRange = isInRange
		};

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
}
