using Microsoft.UI.Xaml;
using Uno.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Skia.Android;

internal sealed class AndroidSkiaWindowFactory : INativeWindowFactoryExtension
{
	private static AndroidSkiaXamlRootHost? _xamlRootHost;

	public bool SupportsMultipleWindows => false;

	public bool SupportsClosingCancellation => false;

	public INativeWindowWrapper CreateWindow(Window window, XamlRoot xamlRoot)
	{
		// While we are currently not having something very useful in the root host, instantiating it has side effects that we need.
		// So this line isn't unnecessary ;)
		_xamlRootHost?.Dispose();
		_xamlRootHost = new AndroidSkiaXamlRootHost(xamlRoot);
		NativeWindowWrapper.Instance.SetWindow(window, xamlRoot);
		return NativeWindowWrapper.Instance;
	}

	internal static void OnNativeWindowClosed()
	{
		_xamlRootHost?.Dispose();
		_xamlRootHost = null;
	}
}
