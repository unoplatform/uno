using Uno.UI.Xaml.Input;
using Windows.System;
using Windows.UI.Xaml.Input;

namespace Uno.UI.DirectUI.Components;

internal static class XboxUtility
{
	/// <summary>
	/// Is Uno on Xbox? Nah!
	/// </summary>
	internal static bool IsOnXbox() => false;

	internal static bool IsGamepadNavigationInput(VirtualKey key) => (int)key >= (int)VirtualKey.GamepadA && (int)key <= (int)VirtualKey.GamepadRightThumbstickLeft;

	internal static bool IsGamepadNavigationDirection(VirtualKey key) =>
		IsGamepadNavigationRight(key) ||
		IsGamepadNavigationLeft(key) ||
		IsGamepadNavigationUp(key) ||
		IsGamepadNavigationDown(key);

	internal static bool IsGamepadPageNavigationDirection(VirtualKey key) =>
		key == VirtualKey.GamepadLeftShoulder ||
		key == VirtualKey.GamepadRightShoulder ||
		key == VirtualKey.GamepadLeftTrigger ||
		key == VirtualKey.GamepadRightTrigger;

	internal static bool IsGamepadNavigationRight(VirtualKey key) => key == VirtualKey.GamepadLeftThumbstickRight || key == VirtualKey.GamepadDPadRight;

	internal static bool IsGamepadNavigationLeft(VirtualKey key) => key == VirtualKey.GamepadLeftThumbstickLeft || key == VirtualKey.GamepadDPadLeft;

	internal static bool IsGamepadNavigationUp(VirtualKey key) => key == VirtualKey.GamepadLeftThumbstickUp || key == VirtualKey.GamepadDPadUp;

	internal static bool IsGamepadNavigationDown(VirtualKey key) => key == VirtualKey.GamepadLeftThumbstickDown || key == VirtualKey.GamepadDPadDown;

	internal static bool IsGamepadNavigationAccept(VirtualKey key) => key == VirtualKey.GamepadA;

	internal static bool IsGamepadNavigationCancel(VirtualKey key) => key == VirtualKey.GamepadB;

	internal static FocusNavigationDirection GetPageNavigationDirection(VirtualKey key)
	{
		FocusNavigationDirection gamepadDirection = FocusNavigationDirection.None;
		switch (key)
		{
			case VirtualKey.GamepadLeftShoulder:
				gamepadDirection = FocusNavigationDirection.Left;
				break;
			case VirtualKey.GamepadRightShoulder:
				gamepadDirection = FocusNavigationDirection.Right;
				break;
			case VirtualKey.GamepadLeftTrigger:
				gamepadDirection = FocusNavigationDirection.Up;
				break;
			case VirtualKey.GamepadRightTrigger:
				gamepadDirection = FocusNavigationDirection.Down;
				break;
		}

		return gamepadDirection;
	}

	internal static FocusNavigationDirection GetNavigationDirection(VirtualKey key)
	{
		if (IsGamepadNavigationDirection(key) == false) { return FocusNavigationDirection.None; }

		return FocusSelection.GetNavigationDirection(key);
	}
}
