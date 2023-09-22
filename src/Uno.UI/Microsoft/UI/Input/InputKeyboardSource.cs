namespace Microsoft.UI.Input;

namespace Microsoft.UI.Input;

#if HAS_UNO_WINUI
public
#else
internal
#endif
partial class InputKeyboardSource
{
#if HAS_UNO_WINUI
	public
#else
	internal
#endif
	static Windows.UI.Core.CoreVirtualKeyStates GetKeyStateForCurrentThread(Windows.System.VirtualKey virtualKey)
		=> Windows.UI.Xaml.Window.Current.CoreWindow.GetKeyState(virtualKey);
}
