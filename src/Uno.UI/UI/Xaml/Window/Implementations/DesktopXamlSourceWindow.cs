#nullable enable

using System.Runtime.CompilerServices;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Hosting;

namespace Uno.UI.Xaml.Controls;

internal partial class DesktopXamlSourceWindow : IWindowImplementation
{
	private readonly Window _window;

	private readonly WindowChrome _windowChrome;
	private readonly DesktopWindowXamlSource _desktopWindowXamlSource;

#pragma warning disable CS0649
	private readonly INativeWindowWrapper? _nativeWindowWrapper; // TODO:MZ: Implement this

	public DesktopXamlSourceWindow(Window window)
	{
		_window = window;
		_windowChrome = new WindowChrome(window);
		_desktopWindowXamlSource = new DesktopWindowXamlSource();
		_desktopWindowXamlSource.Content = _windowChrome;

		_nativeWindowWrapper = NativeWindowFactory.CreateWindow(window);
		_nativeWindowWrapper.SizeChanged += OnNativeWindowSizeChanged;
	}

	public event SizeChangedEventHandler? SizeChanged;

	private void OnNativeWindowSizeChanged(object sender, SizeChangedEventArgs args) => SizeChanged?.Invoke(this, args);

	/// <summary>
	/// For WinUI-based windows, CoreWindow is always null.
	/// </summary>
	public CoreWindow? CoreWindow => null;

	public bool Visible => _nativeWindowWrapper?.Visible ?? false;

	public UIElement? Content
	{
		get => _windowChrome.Content as UIElement;
		set => _windowChrome.Content = value;
	}
}
