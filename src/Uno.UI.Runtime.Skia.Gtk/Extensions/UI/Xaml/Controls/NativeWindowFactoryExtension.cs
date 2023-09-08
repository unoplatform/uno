#nullable enable

using Uno.UI.Runtime.Skia.Gtk.UI.Controls;
using Uno.UI.Xaml.Controls;
using Windows.UI.Xaml;

namespace Uno.UI.Runtime.Skia.Gtk.Extensions.UI.Xaml.Controls;

internal class NativeWindowFactoryExtension : INativeWindowFactoryExtension
{
	internal NativeWindowFactoryExtension()
	{
	}

	public INativeWindowWrapper CreateWindow(Window window, XamlRoot xamlRoot)
	{
		var unoWpfWindow = new UnoGtkWindow(window, xamlRoot);
		unoWpfWindow.UpdateWindowPropertiesFromPackage();
		unoWpfWindow.UpdateWindowPropertiesFromApplicationView();

		return new GtkWindowWrapper(unoWpfWindow);
	}
}
