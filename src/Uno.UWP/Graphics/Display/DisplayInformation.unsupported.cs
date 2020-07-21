using Uno;

namespace Windows.Graphics.Display
{
	public partial class DisplayInformation
    {

#if __WASM__
		/// <summary>
		//// Gets the native orientation of the display monitor, 
		///  which is typically the orientation where the buttons
		///  on the device match the orientation of the monitor.
		/// </summary>
		public DisplayOrientations NativeOrientation { get; private set; } = DisplayOrientations.None;
#endif

#if __WASM__ || __IOS__ || __MACOS__
		/// <summary>
		/// Gets the raw dots per inch (DPI) along the x axis of the display monitor.
		/// </summary>
		/// <remarks>
		/// As per <see href="https://docs.microsoft.com/en-us/uwp/api/windows.graphics.display.displayinformation.rawdpix#remarks">Docs</see> 
		/// defaults to 0 if not set
		/// </remarks>
		[NotImplemented]
		public float RawDpiX { get; private set; } = 0;

		/// <summary>
		/// Gets the raw dots per inch (DPI) along the y axis of the display monitor.
		/// </summary>
		/// <remarks>
		/// As per <see href="https://docs.microsoft.com/en-us/uwp/api/windows.graphics.display.displayinformation.rawdpiy#remarks">Docs</see> 
		/// defaults to 0 if not set
		/// </remarks>
		[NotImplemented]
		public float RawDpiY { get; private set; } = 0;
#endif

#if __WASM__ || __IOS__ || __MACOS__
		/// <summary>
		/// Diagonal size of the display in inches.
		/// </summary>
		/// <remarks>
		/// As per <see href="https://docs.microsoft.com/en-us/uwp/api/windows.graphics.display.displayinformation.diagonalsizeininches#property-value">Docs</see> 
		/// defaults to null if not set
		/// </remarks>
		[NotImplemented]
		public double? DiagonalSizeInInches { get; private set; }
#endif
	}
}
