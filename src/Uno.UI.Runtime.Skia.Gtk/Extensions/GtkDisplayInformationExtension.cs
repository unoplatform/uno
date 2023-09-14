#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Gtk;
using Uno.UI.Runtime.Skia.Gtk.Helpers.Dpi;
using Windows.Graphics.Display;

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
		//TODO:MZ:
		//GtkHost.Current!.MainWindowShown += GtkDisplayInformationExtension_MainWindowShown;
		_dpiHelper = new DpiHelper();
	}

	private void GtkDisplayInformationExtension_MainWindowShown(object? sender, EventArgs e)
	{
		_dpiHelper.DpiChanged += OnDpiChanged;
		OnDpiChanged(null, EventArgs.Empty);
	}

	private Window GetWindow()
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

	private float GetNativeDpi()
	{
		var dpi = _dpiHelper.GetNativeDpi();

		if (GtkHost.Current?.RenderSurfaceType == RenderSurfaceType.Software)
		{
			// Software rendering is not affected by fractional DPI.
			return dpi;
		}

		// We need to make sure that in case of fractional DPI, we use the nearest whole DPI instead,
		// otherwise we get GuardBand related rendering issues.
		var fractionalDpi = dpi / DisplayInformation.BaseDpi;
		var wholeDpi = Math.Max(1.0f, float.Floor(fractionalDpi));
		return wholeDpi * DisplayInformation.BaseDpi;
	}
}
