using Uno.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Input;

public partial class FocusManager
{
	private static void FocusNative(UIElement control)
	{
		if (control?.AcceptsFirstResponder() == true)
		{
			NativeWindowWrapper.Instance.NativeWindow.MakeFirstResponder(control);
		}
	}
}
