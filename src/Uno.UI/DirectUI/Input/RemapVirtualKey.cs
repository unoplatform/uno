using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System;

namespace DirectUI;

internal static class InputUtility
{
	private static Dictionary<VirtualKey, VirtualKey> _keyMap = new()
	{
		{ VirtualKey.GamepadA, VirtualKey.Space },
		{ VirtualKey.GamepadB, VirtualKey.Escape },
		{ VirtualKey.GamepadX, VirtualKey.GamepadX },
		{ VirtualKey.GamepadY, VirtualKey.GamepadY },
		{ VirtualKey.GamepadRightShoulder, VirtualKey.GamepadRightShoulder },
		{ VirtualKey.GamepadLeftShoulder, VirtualKey.GamepadLeftShoulder },
		{ VirtualKey.GamepadLeftTrigger, VirtualKey.GamepadLeftTrigger },
		{ VirtualKey.GamepadRightTrigger, VirtualKey.GamepadRightTrigger },
		{ VirtualKey.GamepadDPadUp, VirtualKey.Up },
		{ VirtualKey.GamepadDPadDown, VirtualKey.Down },
		{ VirtualKey.GamepadDPadLeft, VirtualKey.Left },
		{ VirtualKey.GamepadDPadRight, VirtualKey.Right },
		{ VirtualKey.GamepadMenu, VirtualKey.GamepadMenu },
		{ VirtualKey.GamepadView, VirtualKey.GamepadView },
		{ VirtualKey.GamepadLeftThumbstickButton, VirtualKey.GamepadLeftThumbstickButton },
		{ VirtualKey.GamepadRightThumbstickButton, VirtualKey.GamepadRightThumbstickButton },
		{ VirtualKey.GamepadLeftThumbstickUp, VirtualKey.Up },
		{ VirtualKey.GamepadLeftThumbstickDown, VirtualKey.Down },
		{ VirtualKey.GamepadLeftThumbstickRight, VirtualKey.Right },
		{ VirtualKey.GamepadLeftThumbstickLeft, VirtualKey.Left },
		{ VirtualKey.GamepadRightThumbstickUp, VirtualKey.GamepadRightThumbstickUp },
		{ VirtualKey.GamepadRightThumbstickDown, VirtualKey.GamepadRightThumbstickDown },
		{ VirtualKey.GamepadRightThumbstickRight, VirtualKey.GamepadRightThumbstickRight },
		{ VirtualKey.GamepadRightThumbstickLeft, VirtualKey.GamepadRightThumbstickLeft },
	};

	internal static VirtualKey RemapVirtualKey(VirtualKey key) => _keyMap.TryGetValue(key, out var remappedKey) ? remappedKey : key;
}
