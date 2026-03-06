using System;
using GameController;

namespace Windows.Devices.Input;

public partial class KeyboardCapabilities
{
	private partial int GetKeyboardPresent()
	{
		if (OperatingSystem.IsIOSVersionAtLeast(14))
		{
			return GCKeyboard.CoalescedKeyboard is not null ? 1 : 0;
		}

		return 0;
	}
}
