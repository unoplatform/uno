namespace Uno.UI.Xaml.Controls;

/// <summary>
/// Extension providing the ability to create a native window wrapper.
/// </summary>
internal interface INativeWindowFactoryExtension
{
	INativeWindowWrapper CreateWindow(Windows.UI.Xaml.Window window);
}
