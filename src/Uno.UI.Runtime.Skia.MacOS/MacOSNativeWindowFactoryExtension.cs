#nullable enable

using Microsoft.UI.Xaml;

using Uno.Foundation.Extensibility;
using Uno.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSNativeWindowFactoryExtension : INativeWindowFactoryExtension
{
	public static MacOSNativeWindowFactoryExtension Instance = new();

	public static void Register() => ApiExtensibility.Register(typeof(INativeWindowFactoryExtension), o => Instance);

	private MacOSNativeWindowFactoryExtension()
	{
	}

	public bool SupportsMultipleWindows => true;

	public INativeWindowWrapper CreateWindow(Window window, XamlRoot xamlRoot)
	{
		return new MacOSWindowWrapper(window, xamlRoot);
	}
}
