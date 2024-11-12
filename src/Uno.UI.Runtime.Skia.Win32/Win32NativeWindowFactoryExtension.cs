using Uno.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

namespace Uno.UI.Runtime.Skia.Win32;

internal class Win32NativeWindowFactoryExtension : INativeWindowFactoryExtension
{
	internal Win32NativeWindowFactoryExtension()
	{
	}

	public bool SupportsClosingCancellation => true;

	public bool SupportsMultipleWindows => true;

	public INativeWindowWrapper CreateWindow(Window window, XamlRoot xamlRoot) => new Win32WindowWrapper(window, xamlRoot);
}
