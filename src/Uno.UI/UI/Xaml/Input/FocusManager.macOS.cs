using Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Input;

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
