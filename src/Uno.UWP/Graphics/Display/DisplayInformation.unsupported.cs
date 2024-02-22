using Uno;
using System;

namespace Windows.Graphics.Display
{
	public partial class DisplayInformation
	{
#if IS_UNIT_TESTS || __SKIA__ || __NETSTD_REFERENCE__
		/// <summary>
		//// Gets the native orientation of the display monitor, 
		///  which is typically the orientation where the buttons
		///  on the device match the orientation of the monitor.
		/// </summary>
		[NotImplemented("IS_UNIT_TESTS", "__SKIA__", "__NETSTD_REFERENCE__")]
		public DisplayOrientations NativeOrientation { get; private set; } = DisplayOrientations.None;
#endif

#if __WASM__ || __IOS__ || __MACOS__ || IS_UNIT_TESTS || __SKIA__ || __NETSTD_REFERENCE__
		/// <summary>
		/// Gets the raw dots per inch (DPI) along the x axis of the display monitor.
		/// </summary>
		/// <remarks>
		/// As per <see href="https://docs.microsoft.com/en-us/uwp/api/windows.graphics.display.displayinformation.rawdpix#remarks">Docs</see> 
		/// defaults to 0 if not set
		/// </remarks>
		[NotImplemented("__IOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public float RawDpiX { get; private set; } = 0;

		/// <summary>
		/// Gets the raw dots per inch (DPI) along the y axis of the display monitor.
		/// </summary>
		/// <remarks>
		/// As per <see href="https://docs.microsoft.com/en-us/uwp/api/windows.graphics.display.displayinformation.rawdpiy#remarks">Docs</see> 
		/// defaults to 0 if not set
		/// </remarks>
		[NotImplemented("__IOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public float RawDpiY { get; private set; } = 0;
#endif

#if __WASM__ || __IOS__ || __MACOS__ || IS_UNIT_TESTS || __NETSTD_REFERENCE__
		/// <summary>
		/// Diagonal size of the display in inches.
		/// </summary>
		/// <remarks>
		/// As per <see href="https://docs.microsoft.com/en-us/uwp/api/windows.graphics.display.displayinformation.diagonalsizeininches#property-value">Docs</see> 
		/// defaults to null if not set
		/// </remarks>
		[global::Uno.NotImplemented("__IOS__", "IS_UNIT_TESTS", "__WASM__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public double? DiagonalSizeInInches { get; private set; }
#endif

#if IS_UNIT_TESTS || __NETSTD_REFERENCE__
		[NotImplemented("IS_UNIT_TESTS", "__SKIA__", "__NETSTD_REFERENCE__")]
		public DisplayOrientations CurrentOrientation => DisplayOrientations.None;

		[NotImplemented("IS_UNIT_TESTS", "__SKIA__", "__NETSTD_REFERENCE__")]
		public uint ScreenHeightInRawPixels => 0;

		[NotImplemented("IS_UNIT_TESTS", "__SKIA__", "__NETSTD_REFERENCE__")]
		public uint ScreenWidthInRawPixels => 0;

		[NotImplemented("IS_UNIT_TESTS", "__SKIA__", "__NETSTD_REFERENCE__")]
		public double RawPixelsPerViewPixel => 0;

		[NotImplemented("IS_UNIT_TESTS", "__SKIA__", "__NETSTD_REFERENCE__")]
		public float LogicalDpi => BaseDpi;

		[NotImplemented("IS_UNIT_TESTS", "__SKIA__", "__NETSTD_REFERENCE__")]
		public ResolutionScale ResolutionScale => Display.ResolutionScale.Invalid;
#endif
	}
}
