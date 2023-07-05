using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Gtk;
using Uno.UI.Runtime.Skia.Helpers.Dpi;
using Windows.Graphics.Display;

namespace Uno.UI.Runtime.Skia;

internal class GtkDisplayInformationExtension : IDisplayInformationExtension
{
	private readonly DisplayInformation _displayInformation;
	private readonly DpiHelper _dpiHelper;
	private Gtk.Window? _window;
	private float? _dpi;

	public GtkDisplayInformationExtension(object owner)
	{
		_displayInformation = (DisplayInformation)owner;
		GtkHost.Current!.MainWindowShown += GtkDisplayInformationExtension_MainWindowShown;
		_dpiHelper = new DpiHelper();
	}

	private void GtkDisplayInformationExtension_MainWindowShown(object? sender, EventArgs e)
	{
		_dpiHelper.DpiChanged += OnDpiChanged;
	}

	private Gtk.Window GetWindow()
	{
		_window ??= GtkHost.Current?.MainWindow;
		if (_window is null)
		{
			throw new InvalidOperationException("Main window is not set yet.");
		}

		return _window;
	}

	public DisplayOrientations CurrentOrientation => DisplayOrientations.Landscape;

	public uint ScreenHeightInRawPixels => (uint)GetWindow().Display.GetMonitorAtWindow(GetWindow().Window).Workarea.Height;

	public uint ScreenWidthInRawPixels => (uint)GetWindow().Display.GetMonitorAtWindow(GetWindow().Window).Workarea.Width;

	public float LogicalDpi
	{
		get
		{
			if (_dpi is null)
			{
				var window = GetWindow();
				var nativeWindow = window.Window;
				if (nativeWindow is not null)
				{
					_dpi = window.Display.GetMonitorAtWindow(nativeWindow).ScaleFactor * DisplayInformation.BaseDpi;
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
		_dpi = GetWindow().Display.GetMonitorAtWindow(GetWindow().Window).ScaleFactor * DisplayInformation.BaseDpi;
		_displayInformation.NotifyDpiChanged();
	}
}
