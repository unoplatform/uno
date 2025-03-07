#nullable enable
using System;
using Gtk;
using Uno.UI.Runtime.Skia.Gtk.Helpers.Dpi;
using Uno.UI.Runtime.Skia.Gtk.UI.Controls;
using Windows.Graphics.Display;
using Microsoft.UI.Windowing;

namespace Uno.UI.Runtime.Skia.Gtk;

internal class GtkDisplayInformationExtension : IDisplayInformationExtension
{
	private readonly DisplayInformation _displayInformation;
	private readonly DpiHelper _dpiHelper;
	private Window? _window;
	private float? _dpi;

	public GtkDisplayInformationExtension(object owner)
	{
		_displayInformation = (DisplayInformation)owner;
		UnoGtkWindow.NativeWindowShown += UnoGtkWindow_NativeWindowShown;
		_dpiHelper = new DpiHelper();
	}

	private void UnoGtkWindow_NativeWindowShown(object? sender, UnoGtkWindow e)
	{
		UnoGtkWindow.NativeWindowShown -= UnoGtkWindow_NativeWindowShown;
		_dpiHelper.DpiChanged += OnDpiChanged;
		OnDpiChanged(null, EventArgs.Empty);
	}

	private Window GetWindow()
	{
		if (_window is { })
		{
			return _window;
		}

		// TODO: this is a ridiculous amount of indirection, find something better
		if (AppWindow.GetFromWindowId(_displayInformation.WindowId) is not { } appWindow ||
			Windows.UI.Xaml.Window.GetFromAppWindow(appWindow) is not { } window ||
			UnoGtkWindow.GetGtkWindowFromWindow(window) is not { } gtkWindow)
		{
			throw new InvalidOperationException($"{nameof(GtkDisplayInformationExtension)} couldn't find a GTK window.");
		}

		_window = gtkWindow;
		return _window;
	}

	public DisplayOrientations CurrentOrientation => DisplayOrientations.Landscape;

	public uint ScreenHeightInRawPixels
	{
		get
		{
			if (GetWindow() is not { } window)
			{
				return default;
			}

			return (uint)window.Display.GetMonitorAtWindow(window.Window).Workarea.Height;
		}
	}

	public uint ScreenWidthInRawPixels
	{
		get
		{
			if (GetWindow() is not { } window)
			{
				return default;
			}

			return (uint)window.Display.GetMonitorAtWindow(window.Window).Workarea.Width;
		}
	}

	public float LogicalDpi
	{
		get
		{
			if (_dpi is null)
			{
				var window = GetWindow();
				if (window?.Window is not null)
				{
					_dpi = GetNativeDpi();
				}
			}

			// Native window is not available yet, default to base DPI.
			return _dpi ?? DisplayInformation.BaseDpi;
		}
	}

	public double RawPixelsPerViewPixel => LogicalDpi / DisplayInformation.BaseDpi;

	public ResolutionScale ResolutionScale => (ResolutionScale)(int)(RawPixelsPerViewPixel * 100.0);

	public double? DiagonalSizeInInches => null;

	private void OnDpiChanged(object? sender, EventArgs args)
	{
		_dpi = GetNativeDpi();
		_displayInformation.NotifyDpiChanged();
	}

	private float GetNativeDpi() => _dpiHelper.GetNativeDpi();
}
