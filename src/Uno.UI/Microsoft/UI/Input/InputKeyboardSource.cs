using Uno.UI.Core;
using Windows.System;
using Windows.UI.Core;

namespace Microsoft.UI.Input;

#if HAS_UNO_WINUI
public
#else
internal
#endif
partial class InputKeyboardSource
{
	public static CoreVirtualKeyStates GetKeyStateForCurrentThread(VirtualKey virtualKey)
		=> KeyboardStateTracker.GetKeyState(virtualKey);
}
