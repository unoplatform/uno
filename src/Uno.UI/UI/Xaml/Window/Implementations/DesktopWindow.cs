#nullable enable

using System;
using System.Runtime.CompilerServices;
using Uno.Foundation.Logging;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Hosting;

namespace Uno.UI.Xaml.Controls;

internal partial class DesktopWindow : BaseWindowImplementation
{
	private WindowChrome? _windowChrome;
	private DesktopWindowXamlSource? _desktopWindowXamlSource;

	public DesktopWindow(Window window) : base(window)
	{
	}

	public override void Initialize()
	{
		_windowChrome = new WindowChrome(Window);
		_desktopWindowXamlSource = new DesktopWindowXamlSource();
		_desktopWindowXamlSource.AttachToWindow(Window);
		_desktopWindowXamlSource.Content = _windowChrome;
		base.Initialize();
	}

	/// <summary>
	/// For WinUI-based windows, CoreWindow is always null.
	/// </summary>
	public override CoreWindow? CoreWindow => null;

	public override UIElement? Content
	{
		get => _windowChrome?.Content as UIElement;
		set
		{
			if (_windowChrome is null)
			{
				throw new InvalidOperationException("Window content is being set before the window is initialized.");
			}
			_windowChrome.Content = value;
		}
	}

	public override XamlRoot? XamlRoot => _desktopWindowXamlSource?.XamlIsland.XamlRoot;

	protected override void OnSizeChanged(Size newSize)
	{
		if (_desktopWindowXamlSource is null)
		{
			return;
		}

		_desktopWindowXamlSource.XamlIsland.Width = newSize.Width;
		_desktopWindowXamlSource.XamlIsland.Height = newSize.Height;
	}
}
