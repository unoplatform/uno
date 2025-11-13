#nullable enable

using Uno.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

namespace Uno.WinUI.Runtime.Skia.X11;

internal class X11NativeWindowFactoryExtension : INativeWindowFactoryExtension
{
	internal X11NativeWindowFactoryExtension()
	{
	}

	public bool SupportsClosingCancellation => true;

	public bool SupportsMultipleWindows => true;

	public INativeWindowWrapper CreateWindow(Window window, XamlRoot xamlRoot)
		=> new X11WindowWrapper(window, xamlRoot);
}
