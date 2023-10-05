using Uno.UI.Xaml.Controls;
using Windows.Foundation;

namespace Windows.UI.Xaml;

public sealed partial class Window
{
	internal static void CleanupCurrentForTestsOnly() => _current = null;

	internal void SetWindowSize(Size size) => NativeWindowWrapper.Instance.RaiseNativeSizeChanged(size.Width, size.Height);
}
