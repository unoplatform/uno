#nullable enable

using Windows.UI.Xaml;
using Windows.UI.Xaml.Hosting;

namespace Uno.UI.Xaml.Controls;

internal class DesktopXamlSourceWindow : IWindowImplementation
{
	private readonly Window _window;

	private readonly WindowChrome _windowChrome;
	private readonly DesktopWindowXamlSource _desktopWindowXamlSource;

	public DesktopXamlSourceWindow(Window window)
	{
		_window = window;
		_windowChrome = new WindowChrome(window);
		_desktopWindowXamlSource = new DesktopWindowXamlSource();
		_desktopWindowXamlSource.Content = _windowChrome;
	}

	public UIElement? Content
	{
		get => _windowChrome.Content as UIElement;
		set => _windowChrome.Content = value;
	}
}
