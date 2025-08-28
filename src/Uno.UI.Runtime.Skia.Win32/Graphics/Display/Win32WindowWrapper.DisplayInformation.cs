using System;
using System.Runtime.InteropServices;
using Windows.Graphics.Display;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia.Win32;

internal partial class Win32WindowWrapper : IDisplayInformationExtension
{
	private float _refreshRate = FeatureConfiguration.CompositionTarget.FrameRate;
	private DisplayInfo _displayInfo = DisplayInfo.Default;
	private DisplayInformation? _displayInformation;

	private readonly record struct DisplayInfo(
		DisplayOrientations CurrentOrientation,
		uint ScreenHeightInRawPixels,
		uint ScreenWidthInRawPixels,
		uint LogicalDpi)
	{
		public static DisplayInfo Default => new DisplayInfo(
			DisplayOrientations.Landscape,
			0,
			0,
			1);
	}

	public void SetDisplayInformation(DisplayInformation displayInformation)
	{
		if (_displayInformation != null)
		{
			throw new InvalidOperationException("DisplayInformation has already been set.");
		}
		_displayInformation = displayInformation;
	}

	private void UpdateDisplayInfo()
	{
		var newInfo = GetDisplayInfo();
		var oldInfo = _displayInfo;
		var oldRefreshRate = _refreshRate;
		(_displayInfo, _refreshRate) = newInfo;
		if (oldInfo.LogicalDpi != _displayInfo.LogicalDpi)
		{
			RasterizationScale = (float)_displayInfo.LogicalDpi / PInvoke.USER_DEFAULT_SCREEN_DPI;
			_displayInformation?.NotifyDpiChanged();
		}

		if (oldRefreshRate != _refreshRate)
		{
			XamlRoot?.VisualTree.ContentRoot.CompositionTarget.SetRefreshRate((int)_refreshRate);
		}
	}

	private unsafe (DisplayInfo, float) GetDisplayInfo()
	{
		var hMonitor = PInvoke.MonitorFromWindow(_hwnd, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);

		var monitorInfo = new MONITORINFOEXW
		{
			monitorInfo = new MONITORINFO
			{
				cbSize = (uint)Marshal.SizeOf<MONITORINFOEXW>()
			}
		};

		if (!PInvoke.GetMonitorInfo(hMonitor, (MONITORINFO*)(&monitorInfo)))
		{
			this.LogError()?.Error($"{nameof(PInvoke.GetMonitorInfo)} failed: {Win32Helper.GetErrorMessage()}");
			return (_displayInfo, _refreshRate);
		}

		var devMode = new DEVMODEW
		{
			dmSize = (ushort)Marshal.SizeOf<DEVMODEW>(),
			dmFields =
				DEVMODE_FIELD_FLAGS.DM_DISPLAYORIENTATION |
				DEVMODE_FIELD_FLAGS.DM_PELSWIDTH |
				DEVMODE_FIELD_FLAGS.DM_PELSHEIGHT |
				DEVMODE_FIELD_FLAGS.DM_DISPLAYFREQUENCY
		};
		if (!PInvoke.EnumDisplaySettingsEx(monitorInfo.szDevice.ToString(), ENUM_DISPLAY_SETTINGS_MODE.ENUM_CURRENT_SETTINGS, ref devMode, ENUM_DISPLAY_SETTINGS_FLAGS.EDS_RAWMODE))
		{
			this.LogError()?.Error($"{nameof(PInvoke.EnumDisplaySettingsEx)} failed: {Win32Helper.GetErrorMessage()}");
			return (_displayInfo, _refreshRate);
		}

		var width = devMode.dmPelsWidth;
		var height = devMode.dmPelsHeight;

		var orientation = (width > height, devMode.Anonymous1.Anonymous2.dmDisplayOrientation) switch
		{
			(true, DEVMODE_DISPLAY_ORIENTATION.DMDO_DEFAULT) => DisplayOrientations.Landscape,
			(true, DEVMODE_DISPLAY_ORIENTATION.DMDO_90) => DisplayOrientations.Landscape,
			(true, DEVMODE_DISPLAY_ORIENTATION.DMDO_180) => DisplayOrientations.LandscapeFlipped,
			(true, DEVMODE_DISPLAY_ORIENTATION.DMDO_270) => DisplayOrientations.LandscapeFlipped,
			(false, DEVMODE_DISPLAY_ORIENTATION.DMDO_DEFAULT) => DisplayOrientations.Portrait,
			(false, DEVMODE_DISPLAY_ORIENTATION.DMDO_90) => DisplayOrientations.Portrait,
			(false, DEVMODE_DISPLAY_ORIENTATION.DMDO_180) => DisplayOrientations.PortraitFlipped,
			(false, DEVMODE_DISPLAY_ORIENTATION.DMDO_270) => DisplayOrientations.PortraitFlipped,
			_ => throw new ArgumentOutOfRangeException()
		};

		var dpi = PInvoke.GetDpiForWindow(_hwnd);
		if (dpi == 0)
		{
			this.LogError()?.Error($"{nameof(PInvoke.GetDpiForWindow)} failed: {Win32Helper.GetErrorMessage()}");
			dpi = 96;
		}

		return (new DisplayInfo(orientation, height, width, dpi), devMode.dmDisplayFrequency);
	}

	public DisplayOrientations CurrentOrientation => _displayInfo.CurrentOrientation;
	public uint ScreenHeightInRawPixels => _displayInfo.ScreenHeightInRawPixels;
	public uint ScreenWidthInRawPixels => _displayInfo.ScreenWidthInRawPixels;
	public float LogicalDpi => _displayInfo.LogicalDpi;
	public double RawPixelsPerViewPixel => _displayInfo.LogicalDpi * 1.0 / DisplayInformation.BaseDpi;
	public ResolutionScale ResolutionScale => (ResolutionScale)(RawPixelsPerViewPixel * 100);
	public double? DiagonalSizeInInches => null;
}
