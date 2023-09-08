#nullable enable

using System;
using System.Runtime.CompilerServices;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Hosting;

namespace Uno.UI.Xaml.Controls;

internal partial class DesktopWindow : IWindowImplementation
{
	private readonly Window _window;

	private readonly WindowChrome _windowChrome;
	private readonly DesktopWindowXamlSource _desktopWindowXamlSource;

#pragma warning disable CS0649
	private readonly INativeWindowWrapper? _nativeWindowWrapper; // TODO:MZ: Implement this

	public DesktopWindow(Window window)
	{
		_window = window;
		_windowChrome = new WindowChrome(window);
		_desktopWindowXamlSource = new DesktopWindowXamlSource();
		_desktopWindowXamlSource.AttachToWindow(window);
		_desktopWindowXamlSource.Content = _windowChrome;

		_nativeWindowWrapper = NativeWindowFactory.CreateWindow(window, _desktopWindowXamlSource.XamlIsland!.XamlRoot!);
		if (_nativeWindowWrapper is null)
		{
			throw new InvalidOperationException("This platform does not support creating multiple windows yet.");
		}

		_nativeWindowWrapper.SizeChanged += OnNativeWindowSizeChanged;
	}

	public event SizeChangedEventHandler? SizeChanged;

	private void OnNativeWindowSizeChanged(object sender, SizeChangedEventArgs args)
	{
		SizeChanged?.Invoke(this, args);
		_windowChrome.XamlRoot?.NotifyChanged();
	}

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

	public XamlRoot? XamlRoot => _desktopWindowXamlSource.XamlIsland.XamlRoot;

	public void Activate()
	{
		ContentManager.TryLoadRootVisual(_windowChrome.XamlRoot!);
	}
}
