using Microsoft.UI.Xaml;
using Uno.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Skia.Android;

internal sealed class AndroidSkiaWindowFactory : INativeWindowFactoryExtension
{
	public bool SupportsMultipleWindows => false;

	public INativeWindowWrapper CreateWindow(Window window, XamlRoot xamlRoot) => NativeWindowWrapper.Instance;
}
