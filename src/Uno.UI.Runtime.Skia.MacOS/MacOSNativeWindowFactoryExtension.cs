using Microsoft.UI.Xaml;

using Uno.Foundation.Extensibility;
using Uno.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSNativeWindowFactoryExtension : INativeWindowFactoryExtension
{
	private static readonly MacOSNativeWindowFactoryExtension _instance = new();

	public static void Register() => ApiExtensibility.Register(typeof(INativeWindowFactoryExtension), _ => _instance);

	private MacOSNativeWindowFactoryExtension()
	{
	}

	public bool SupportsClosingCancellation => true;

	public bool SupportsMultipleWindows => true;

	public INativeWindowWrapper CreateWindow(Window window, XamlRoot xamlRoot)
	{
		var native = new MacOSWindowNative(window, xamlRoot, out var initialSize);
		return new MacOSWindowWrapper(native, window, xamlRoot, initialSize);
	}
}
