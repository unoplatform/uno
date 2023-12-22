#nullable enable

using Uno.UI.Runtime.Skia.Gtk.UI.Controls;
using Uno.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

namespace Uno.UI.Runtime.Skia.Gtk.Extensions.UI.Xaml.Controls;

internal class NativeWindowFactoryExtension : INativeWindowFactoryExtension
{
	internal NativeWindowFactoryExtension()
	{
	}

	public bool SupportsMultipleWindows => true;

	public INativeWindowWrapper CreateWindow(Window window, XamlRoot xamlRoot)
	{
		var unoGtkWindow = new UnoGtkWindow(window, xamlRoot);
		return new GtkWindowWrapper(unoGtkWindow);
	}
}
