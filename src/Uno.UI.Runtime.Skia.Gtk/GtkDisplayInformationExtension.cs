using System;
using System.Runtime.InteropServices;
using Gtk;
using Uno.UI.Runtime.Skia.Helpers.Dpi;
using Windows.Graphics.Display;

namespace Uno.UI.Runtime.Skia;

internal class GtkDisplayInformationExtension : IDisplayInformationExtension
{
	private readonly DisplayInformation _displayInformation;
	private readonly Window _window;
	private readonly DpiHelper _dpiHelper;

	private float? _dpi;

	public GtkDisplayInformationExtension(object owner, Gtk.Window window)
	{
		_displayInformation = (DisplayInformation)owner;
		_window = window;
		_dpiHelper = new DpiHelper(_window);
		_dpiHelper.DpiChanged += OnDpiChanged;
	}

	public DisplayOrientations CurrentOrientation => DisplayOrientations.Landscape;

	public uint ScreenHeightInRawPixels => (uint)_window.Display.GetMonitorAtWindow(_window.Window).Workarea.Height;

	public uint ScreenWidthInRawPixels => (uint)_window.Display.GetMonitorAtWindow(_window.Window).Workarea.Width;

	public float LogicalDpi
		// GetLogicalDpi cannot be used here to get the actual
		// scale factor, as GTK does not support fractional scaling.
		// For instance, 2.5x will become 2x.
		=> _dpi ??= _window.Display.GetMonitorAtWindow(_window.Window).ScaleFactor * DisplayInformation.BaseDpi;

	public double RawPixelsPerViewPixel => LogicalDpi / DisplayInformation.BaseDpi;

	public ResolutionScale ResolutionScale => (ResolutionScale)(int)(RawPixelsPerViewPixel * 100.0);

	public double? DiagonalSizeInInches => null;

	private void OnDpiChanged(object? sender, EventArgs args)
	{
		_dpi = _window.Display.GetMonitorAtWindow(_window.Window).ScaleFactor * DisplayInformation.BaseDpi;
		_displayInformation.NotifyDpiChanged();
	}

	private float GetLogicalDpi() => _dpiHelper.GetLogicalDpi();
}
