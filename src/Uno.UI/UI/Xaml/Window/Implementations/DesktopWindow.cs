#nullable enable

using System;
using System.Runtime.CompilerServices;
using Uno.Foundation.Logging;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Hosting;

namespace Uno.UI.Xaml.Controls;

internal partial class DesktopWindow : BaseWindowImplementation
{
	private readonly WindowChrome _windowChrome;
	private readonly DesktopWindowXamlSource _desktopWindowXamlSource;

#pragma warning disable CS0649
	public DesktopWindow(Window window) : base(window)
	{
		_windowChrome = new WindowChrome(window);
		_desktopWindowXamlSource = new DesktopWindowXamlSource();
		_desktopWindowXamlSource.AttachToWindow(window);
		_desktopWindowXamlSource.Content = _windowChrome;

		InitializeNativeWindow();
	}

	/// <summary>
	/// For WinUI-based windows, CoreWindow is always null.
	/// </summary>
	public override CoreWindow? CoreWindow => null;

	public override UIElement? Content
	{
		get => _windowChrome.Content as UIElement;
		set => _windowChrome.Content = value;
	}

	public override XamlRoot? XamlRoot => _desktopWindowXamlSource.XamlIsland.XamlRoot;

	public override void Activate() => ContentManager.TryLoadRootVisual(_windowChrome.XamlRoot!);
}
