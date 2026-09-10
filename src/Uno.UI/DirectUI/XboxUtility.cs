using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Helpers.WinUI;
using Windows.System;

namespace DirectUI;

internal static class XboxUtility
{
	// Uno specific: the cached device-family probe lives in SharedHelpers; forwarding keeps
	// the WinUI call site (XboxUtility::IsOnXbox) without a second copy of the check.
	internal static bool IsOnXbox() => SharedHelpers.IsOnXbox();

	internal static bool IsGamepadNavigationInput(VirtualKey key)
	{
		return (int)key >= (int)VirtualKey.GamepadA && (int)key <= (int)VirtualKey.GamepadRightThumbstickLeft;
	}
}
