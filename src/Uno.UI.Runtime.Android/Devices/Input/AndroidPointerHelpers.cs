using System;
using Android.Views;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Core;
using PointerDeviceType = Windows.Devices.Input.PointerDeviceType;

namespace Uno.UI.Runtime.Android;

internal static class AndroidPointerHelpers
{
	private const MotionEventButtonState MouseButtons =
		MotionEventButtonState.Primary
		| MotionEventButtonState.Secondary
		| MotionEventButtonState.Tertiary
		| MotionEventButtonState.Back
		| MotionEventButtonState.Forward;

	/// <summary>
	/// The stylus is pressed while holding the barrel button (legacy Samsung S Pen action)
	/// </summary>
	internal const MotionEventActions StylusWithBarrelDown = (MotionEventActions)211;
	/// <summary>
	/// The stylus is moved after having been pressed while holding the barrel button (legacy Samsung S Pen action)
	/// </summary>
	internal const MotionEventActions StylusWithBarrelMove = (MotionEventActions)213;
	/// <summary>
	/// The stylus is released after having been pressed while holding the barrel button (legacy Samsung S Pen action)
	/// </summary>
	internal const MotionEventActions StylusWithBarrelUp = (MotionEventActions)212;

	private const int _pointerIdsCount = (int)MotionEventActions.PointerIndexMask >> (int)MotionEventActions.PointerIndexShift; // 0xff
	private const int _pointerIdsShift = 31 - (int)MotionEventActions.PointerIndexShift; // 23

	internal static uint GetPointerId(MotionEvent nativeEvent, int pointerIndex)
	{
		// Here we assume that usually pointerId is 'PointerIndexShift' bits long (8 bits / 255 ids),
		// and that usually the deviceId is [0, something_not_too_big_hopefully_less_than_0x00ffffff].
		// If deviceId is greater than 0x00ffffff, we might have a conflict but only in case of multi touch
		// and with a high variation of deviceId. We assume that's safe enough.
		// Note: Make sure to use the GetPointerId in order to make sure to keep the same id while: down_1 / down_2 / up_1 / up_2
		//		 otherwise up_2 will be with the id of 1

		return ((uint)nativeEvent.GetPointerId(pointerIndex) & _pointerIdsCount) << _pointerIdsShift | (uint)nativeEvent.DeviceId;
	}

	internal static bool IsStylusWithBarrelAction(MotionEventActions action)
		=> action is StylusWithBarrelDown or StylusWithBarrelMove or StylusWithBarrelUp;

	internal static bool HasMouseButtonPressed(MotionEventButtonState buttons)
		=> (buttons & MouseButtons) != 0;

	internal static bool IsPenInContact(MotionEvent nativeEvent, int pointerIndex, MotionEventActions action, bool wasInContact)
	{
		switch (action)
		{
			// The up still carries the pressure of the last contact, so the action prevails over the axes.
			case StylusWithBarrelUp:
			case MotionEventActions.Up:
			case MotionEventActions.PointerUp:
			case MotionEventActions.Cancel:
			case MotionEventActions.HoverMove:
				return false;

			case StylusWithBarrelDown:
			case StylusWithBarrelMove:
			case MotionEventActions.Down:
			case MotionEventActions.PointerDown:
			case MotionEventActions.Move:
				return true;

			case MotionEventActions.ButtonPress:
			case MotionEventActions.ButtonRelease:
				return wasInContact;
		}

		if (nativeEvent.GetAxisValue(Axis.Distance, pointerIndex) is not 0)
		{
			return false;
		}

		if (nativeEvent.GetAxisValue(Axis.Pressure, pointerIndex) is not 0)
		{
			return true;
		}

		// We should either have a distance or a pressure, not both at 0 ... but this can happen on some devices (e.g. Surface Duo).
		// The system raises an exit/enter when the pen is pressed/released, so we return true (safer):
		// the input source defers the exit to confirm we don't receive a press right after.
		return true;
	}

	internal static bool IsTouchInContact(MotionEventActions action)
		=> action is not (MotionEventActions.Up or MotionEventActions.PointerUp or MotionEventActions.Cancel or MotionEventActions.HoverExit);

	internal static PointerPointProperties GetMouseProperties(MotionEventButtonState buttons, bool isTouchPad)
		=> new()
		{
			IsPrimary = true,
			IsInRange = true,
			IsTouchPad = isTouchPad,
			IsLeftButtonPressed = buttons.HasFlag(MotionEventButtonState.Primary),
			IsRightButtonPressed = buttons.HasFlag(MotionEventButtonState.Secondary),
			IsMiddleButtonPressed = buttons.HasFlag(MotionEventButtonState.Tertiary),
			IsXButton1Pressed = buttons.HasFlag(MotionEventButtonState.Back),
			IsXButton2Pressed = buttons.HasFlag(MotionEventButtonState.Forward),
			// Pressure stays at its .5 default, as on WinUI, no matter if a button is pressed or not.
		};

	/// <summary>
	/// Gets the properties of a pen pointer.
	/// </summary>
	/// <param name="isRightContact">
	/// Whether the current contact started while the barrel button was held: like on WinUI, such a contact is
	/// reported as a right button until released, while pressing the barrel during a contact keeps it a left one.
	/// </param>
	internal static PointerPointProperties GetPenProperties(MotionEvent nativeEvent, int pointerIndex, MotionEventToolType toolType, MotionEventButtonState buttons, bool isInContact, bool isRightContact)
	{
		var isEraser = toolType is MotionEventToolType.Eraser;

		return new()
		{
			IsPrimary = true,
			IsInRange = true,
			IsEraser = isEraser,
			IsInverted = isEraser,
			IsBarrelButtonPressed = buttons.HasFlag(MotionEventButtonState.StylusPrimary),
			IsLeftButtonPressed = isInContact && !isRightContact,
			IsRightButtonPressed = isInContact && isRightContact,
			Pressure = Math.Min(1f, nativeEvent.GetPressure(pointerIndex)), // Might exceed 1.0 on Android
		};
	}

	internal static PointerPointProperties GetTouchProperties(MotionEventActions action, bool isInContact)
		=> new()
		{
			IsPrimary = true,
			IsInRange = true,
			IsLeftButtonPressed = isInContact,
			PointerUpdateKind = action switch
			{
				MotionEventActions.Down or MotionEventActions.PointerDown => PointerUpdateKind.LeftButtonPressed,
				MotionEventActions.Up or MotionEventActions.PointerUp => PointerUpdateKind.LeftButtonReleased,
				_ => PointerUpdateKind.Other,
			},
			// Pressure stays at its .5 default, as on WinUI for fingers.
		};

	/// <summary>
	/// Converts the scroll axis of an ACTION_SCROLL into a WinUI wheel delta.
	/// </summary>
	/// <remarks>
	/// Android reports wheel detents as ±1 (fractional for high resolution wheels), positive being up / right,
	/// which matches the sign of WinUI's <see cref="PointerPointProperties.MouseWheelDelta"/>.
	/// </remarks>
	internal static int ToMouseWheelDelta(float axisValue)
		=> (int)Math.Round(axisValue * ScrollContentPresenter.ScrollViewerDefaultMouseWheelDelta);

	internal static PointerIconType ToPointerIconType(CoreCursorType type)
		=> type switch
		{
			CoreCursorType.Cross => PointerIconType.Crosshair,
			CoreCursorType.Hand => PointerIconType.Hand,
			CoreCursorType.Help => PointerIconType.Help,
			CoreCursorType.IBeam => PointerIconType.Text,
			CoreCursorType.SizeAll => PointerIconType.AllScroll,
			CoreCursorType.SizeNortheastSouthwest => PointerIconType.TopRightDiagonalDoubleArrow,
			CoreCursorType.SizeNorthSouth => PointerIconType.VerticalDoubleArrow,
			CoreCursorType.SizeNorthwestSoutheast => PointerIconType.TopLeftDiagonalDoubleArrow,
			CoreCursorType.SizeWestEast => PointerIconType.HorizontalDoubleArrow,
			CoreCursorType.UniversalNo => PointerIconType.NoDrop,
			CoreCursorType.Wait => PointerIconType.Wait,
			_ => PointerIconType.Arrow,
		};
}
