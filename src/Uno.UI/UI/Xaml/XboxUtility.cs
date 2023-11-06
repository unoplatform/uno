using Windows.System;

namespace Uno.UI.Helpers;

internal static class XboxUtility
{
	public static bool IsGamepadNavigationInput(VirtualKey key)
	{
		return key is
			VirtualKey.GamepadA or
			VirtualKey.GamepadB or
			VirtualKey.GamepadX or
			VirtualKey.GamepadY or
			VirtualKey.GamepadRightShoulder or
			VirtualKey.GamepadLeftShoulder or
			VirtualKey.GamepadLeftTrigger or
			VirtualKey.GamepadRightTrigger or
			VirtualKey.GamepadDPadUp or
			VirtualKey.GamepadDPadDown or
			VirtualKey.GamepadDPadLeft or
			VirtualKey.GamepadDPadRight or
			VirtualKey.GamepadMenu or
			VirtualKey.GamepadView or
			VirtualKey.GamepadLeftThumbstickButton or
			VirtualKey.GamepadRightThumbstickButton or
			VirtualKey.GamepadLeftThumbstickUp or
			VirtualKey.GamepadLeftThumbstickDown or
			VirtualKey.GamepadLeftThumbstickRight or
			VirtualKey.GamepadLeftThumbstickLeft or
			VirtualKey.GamepadRightThumbstickUp or
			VirtualKey.GamepadRightThumbstickDown or
			VirtualKey.GamepadRightThumbstickRight or
			VirtualKey.GamepadRightThumbstickLeft;
	}
}
