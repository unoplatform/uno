#nullable enable

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Windows.Graphics.Display;

using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSDisplayInformationExtension : IDisplayInformationExtension
{
	public static MacOSDisplayInformationExtension Instance = new();

	private MacOSDisplayInformationExtension()
	{
	}

	internal unsafe static void Register()
	{
		// FIXME: use a single callback method ?
		NativeUno.uno_set_window_did_change_screen_callback(ref SharedScreenData, &Update);
		NativeUno.uno_set_window_did_change_screen_parameters_callback(&UpdateParameters);
		ApiExtensibility.Register(typeof(IDisplayInformationExtension), o => Instance);
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct ScreenData
	{
		public uint ScreenHeightInRawPixels;
		public uint ScreenWidthInRawPixels;
		public uint RawPixelsPerViewPixel;
	}

	private static ScreenData _data;
	private static DisplayOrientations _currentOrientation;
	private static float _logicalDpi;
	private static ResolutionScale _resolutionScale;

	public DisplayOrientations CurrentOrientation => _currentOrientation;

	public uint ScreenHeightInRawPixels => _data.ScreenHeightInRawPixels;

	public uint ScreenWidthInRawPixels => _data.ScreenWidthInRawPixels;

	public float LogicalDpi => _logicalDpi;

	public double RawPixelsPerViewPixel => _data.RawPixelsPerViewPixel;

	public ResolutionScale ResolutionScale => _resolutionScale;

	public double? DiagonalSizeInInches => null;

	internal static ref ScreenData SharedScreenData => ref _data;

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	internal static void Update()
	{
		if (typeof(MacOSDisplayInformationExtension).Log().IsEnabled(LogLevel.Trace))
		{
			typeof(MacOSDisplayInformationExtension).Log().Trace($"MacOSDisplayInformationExtension.Update {_data.ScreenWidthInRawPixels} x {_data.ScreenHeightInRawPixels} @ {_data.RawPixelsPerViewPixel}x");
		}

		// updated calculated fields from the native/shared ones
		_currentOrientation = _data.ScreenWidthInRawPixels > _data.ScreenHeightInRawPixels
			? DisplayOrientations.Landscape : DisplayOrientations.Portrait;

		_logicalDpi = _data.RawPixelsPerViewPixel * /* DisplayInformation.BaseDpi */ 96.0f;

		_resolutionScale = (ResolutionScale)(int)(_data.RawPixelsPerViewPixel * 100.0);
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	internal static void UpdateParameters()
	{
		if (typeof(MacOSDisplayInformationExtension).Log().IsEnabled(LogLevel.Trace))
		{
			typeof(MacOSDisplayInformationExtension).Log().Trace("MacOSDisplayInformationExtension.UpdateParameters");
		}

		DisplayInformation.GetForCurrentView().NotifyDpiChanged();
	}
}
