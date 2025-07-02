using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.Xaml.Controls;

internal enum DismissalTriggerFlags : uint
{
	None = 0x0,
	CoreLightDismiss = 0x1,
	WindowSizeChange = 0x2,
	WindowDeactivated = 0x4,
	BackPress = 0x8,
	All = 0xFFFFFFFF    // Note: This flag encompases all present and future flags, hence
						// it is not equivilant to the OR of the current set of flags.
}
