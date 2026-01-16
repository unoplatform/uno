using Uno.UI.Core;
using Windows.System;
using Windows.UI.Core;

namespace Microsoft.UI.Input;

public partial class InputKeyboardSource
{
	public static CoreVirtualKeyStates GetKeyStateForCurrentThread(VirtualKey virtualKey)
		=> KeyboardStateTracker.GetKeyState(virtualKey);
}
