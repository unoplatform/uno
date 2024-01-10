namespace Microsoft.UI.Input;

public partial class InputKeyboardSource
{
#if HAS_UNO_WINUI
	public
#else
	internal
#endif
	static Windows.UI.Core.CoreVirtualKeyStates GetKeyStateForCurrentThread(Windows.System.VirtualKey virtualKey)
		=> Microsoft.UI.Xaml.Window.Current.CoreWindow.GetKeyState(virtualKey);
}
