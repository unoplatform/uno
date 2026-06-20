#nullable enable

using System;
using System.Runtime.CompilerServices;
using Uno.Foundation.Logging;
using Windows.Foundation;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;

namespace Uno.UI.Xaml.Controls;

internal class DesktopWindow : BaseWindowImplementation
{
	private WindowChrome? _windowChrome;
	private DesktopWindowXamlSource? _desktopWindowXamlSource;

	public DesktopWindow(Window window) : base(window)
	{
	}

	public override void Initialize()
	{
		// macOS-only: the secondary ALC scenario collides with MacOSWindowHost's
		// native window registration. Skipping native window/chrome/XamlSource here
		// avoids the duplicate registration. Win32/X11 require XamlRoot/RootElement
		// to be set up (DisplayInformation extensions dereference them), so they
		// must continue through the normal Initialize path.
		if (Window.ContentHostOverride is not null && OperatingSystem.IsMacOS())
		{
			return;
		}

		_windowChrome = new WindowChrome(Window);
		_windowChrome.ApplyStylingForMinMaxCloseButtons();
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
				// On macOS hosted ALC scenarios the chrome is intentionally not created;
				// content is redirected at the Window.Content level via ContentHostOverride.
				if (Window.ContentHostOverride is not null && OperatingSystem.IsMacOS())
				{
					return;
				}

				throw new InvalidOperationException(
					"Window content is being set before the application is initialized." +
					"Instead, set the window content later - e.g. in OnLaunched.");
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

	public override void SetTitleBar(UIElement? titleBar)
	{
		_windowChrome?.SetTitleBar(titleBar);
	}
}
