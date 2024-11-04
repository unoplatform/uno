#nullable enable

using System.DirectoryServices.ActiveDirectory;
using Uno.UI.Runtime.Skia.Win32.UI.Controls;
using Uno.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

namespace Uno.UI.Runtime.Skia.Win32.Extensions.UI.Xaml.Controls;

internal class NativeWindowFactoryExtension : INativeWindowFactoryExtension
{
	internal NativeWindowFactoryExtension()
	{
	}

	public bool SupportsClosingCancellation => true;

	public bool SupportsMultipleWindows => true;

	public INativeWindowWrapper CreateWindow(Window window, XamlRoot xamlRoot)
	{
		var unoWpfWindow = new UnoWpfWindow(window, xamlRoot);
		return new WpfWindowWrapper(unoWpfWindow, window, xamlRoot);
	}
}
