#nullable enable

using System;
using Android.Views;
using Windows.Gaming.Input;

namespace Uno.Gaming.Input.Internal;

/// <summary>
/// Implementation based on https://developer.android.com/training/game-controllers/controller-input.
/// </summary>
internal static class GamepadDpad
{
	private const float Epsilon = 0.001f;

	public static GamepadButtons? GetDirectionPressed(InputEvent inputEvent)
	{
		if (!IsDpadDevice(inputEvent))
		{
			return null;
		}

		GamepadButtons directionPressed = GamepadButtons.None;

		// If the input event is a MotionEvent, check its hat axis values.
		if (inputEvent is MotionEvent)
		{
			// Use the hat axis value to find the D-pad direction
			MotionEvent motionEvent = (MotionEvent)inputEvent;

			float xaxis = motionEvent.GetAxisValue(Axis.HatX);
			float yaxis = motionEvent.GetAxisValue(Axis.HatY);

			// Check if the AXIS_HAT_X value is -1 or 1, and set the D-pad
			// LEFT and RIGHT direction accordingly.
			if (Math.Abs(xaxis - (-1.0f)) < Epsilon)
			{
				directionPressed = directionPressed | GamepadButtons.DPadLeft;
			}
			else if (Math.Abs(xaxis - 1.0f) < Epsilon)
			{
				directionPressed = directionPressed | GamepadButtons.DPadRight;
			}
			// Check if the AXIS_HAT_Y value is -1 or 1, and set the D-pad
			// UP and DOWN direction accordingly.
			if (Math.Abs(yaxis - (-1.0f)) < Epsilon)
			{
				directionPressed = directionPressed | GamepadButtons.DPadUp;
			}
			else if (Math.Abs(yaxis - 1.0f) < Epsilon)
			{
				directionPressed = directionPressed | GamepadButtons.DPadDown;
			}
		}
		return directionPressed;
	}

	private static bool IsDpadDevice(InputEvent inputEvent)
	{
		// Check that input comes from a device with directional pads.
		return
			inputEvent.Source.HasFlag(InputSourceType.Dpad) ||
			inputEvent.Source.HasFlag(InputSourceType.Joystick) ||
			inputEvent.Source.HasFlag(InputSourceType.Gamepad);
	}
}
