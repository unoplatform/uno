using Microsoft.UI.Xaml;

namespace Uno.UI.Xaml.Controls;

internal class SingletonWindowFactory : INativeWindowFactoryExtension
{
	private readonly NativeWindowWrapperBase _instance;

	public bool SupportsMultipleWindows => false;

	public SingletonWindowFactory(NativeWindowWrapperBase instance)
	{
		_instance = instance;
	}

	public INativeWindowWrapper CreateWindow(Microsoft.UI.Xaml.Window window, XamlRoot xamlRoot) => _instance; // TODO: MZ: Set XamlRoot
}
