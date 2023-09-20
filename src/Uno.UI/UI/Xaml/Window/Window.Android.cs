#nullable enable

using Uno.UI.Xaml.Controls;

namespace Windows.UI.Xaml;

public sealed partial class Window
{
	public bool IsStatusBarTranslucent() => NativeWindowWrapper.Instance.IsStatusBarTranslucent(); //TODO:MZ: Can remove as breaking change?
}
