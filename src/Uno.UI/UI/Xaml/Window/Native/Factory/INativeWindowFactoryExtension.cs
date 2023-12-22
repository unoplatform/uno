using Microsoft.UI.Xaml;

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// Extension providing the ability to create a native window wrapper.
/// </summary>
internal interface INativeWindowFactoryExtension
{
	bool SupportsMultipleWindows { get; }

	INativeWindowWrapper CreateWindow(Microsoft.UI.Xaml.Window window, XamlRoot xamlRoot);
}
