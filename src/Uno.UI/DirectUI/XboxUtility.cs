using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System;

namespace DirectUI;

internal static class XboxUtility
{
	// Uno specific: tvOS shares the Xbox "10-foot", remote-first input model.
	internal static bool IsOnXbox() => OperatingSystem.IsTvOS();

	internal static bool IsGamepadNavigationInput(VirtualKey key)
	{
		return (int)key >= (int)VirtualKey.GamepadA && (int)key <= (int)VirtualKey.GamepadRightThumbstickLeft;
	}
}
