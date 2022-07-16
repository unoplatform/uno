#nullable enable
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

	public float LogicalDpi => _dpi ??= GetLogicalDpi();

	public double RawPixelsPerViewPixel => LogicalDpi / DisplayInformation.BaseDpi;

	public ResolutionScale ResolutionScale => (ResolutionScale)(int)(RawPixelsPerViewPixel * 100.0);

	private void OnDpiChanged(object? sender, EventArgs args)
	{
		_dpi = GetLogicalDpi();
		_displayInformation.NotifyDpiChanged();
	}

	private float GetLogicalDpi() => _dpiHelper.GetLogicalDpi();
}
