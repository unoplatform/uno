#nullable enable

using System.DirectoryServices.ActiveDirectory;
using Uno.UI.Runtime.Skia.Win32.UI.Controls;
using Uno.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

namespace Uno.UI.Runtime.Skia.Win32.Extensions.UI.Xaml.Controls;

internal class Win32NativeWindowFactoryExtension : INativeWindowFactoryExtension
{
	internal Win32NativeWindowFactoryExtension()
	{
	}

	public bool SupportsClosingCancellation => true;

	public bool SupportsMultipleWindows => true;

	public INativeWindowWrapper CreateWindow(Window window, XamlRoot xamlRoot)
	{
		return new Win32WindowWrapper(window, xamlRoot);
	}
}
