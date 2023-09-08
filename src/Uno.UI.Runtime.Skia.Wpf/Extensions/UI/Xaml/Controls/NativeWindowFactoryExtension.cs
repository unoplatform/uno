#nullable enable

using Uno.UI.Runtime.Skia.Wpf.UI.Controls;
using Uno.UI.Xaml.Controls;
using Windows.UI.Xaml;

namespace Uno.UI.Runtime.Skia.Wpf.Extensions.UI.Xaml.Controls;

internal class NativeWindowFactoryExtension : INativeWindowFactoryExtension
{
	internal NativeWindowFactoryExtension()
	{
	}

	public INativeWindowWrapper CreateWindow(Window window, XamlRoot xamlRoot)
	{
		var unoWpfWindow = new UnoWpfWindow(window, xamlRoot);
		unoWpfWindow.UpdateWindowPropertiesFromPackage();
		unoWpfWindow.UpdateWindowPropertiesFromApplicationView();

		return new WpfWindowWrapper(unoWpfWindow);
	}
}
