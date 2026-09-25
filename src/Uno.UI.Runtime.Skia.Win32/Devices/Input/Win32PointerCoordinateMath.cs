namespace Uno.UI.Runtime.Skia.Win32;

// Kept as a standalone type (rather than a Win32WindowWrapper member), using only plain doubles and
// Windows.Foundation.Point, so the pure conversion math can be linked into Uno.UI.UnitTests and unit
// tested there without pulling in Win32WindowWrapper's HWND/P-Invoke dependencies.
internal static class Win32PointerCoordinateMath
{
	// 1 logical px = 1/96 inch; 1 inch = 2540 HIMETRIC (0.01 mm) units.
	internal const double HimetricPerLogicalPx = 2540.0 / 96.0;

	// Split out from Win32WindowWrapper.ToClientLogical so the HIMETRIC/scale arithmetic is testable
	// without a real HWND (the ScreenToClient P/Invoke call stays in the caller).
	internal static Windows.Foundation.Point ComputeClientLogical(
		(double X, double Y) screenPx,
		(double X, double Y) clientPx,
		(double X, double Y) screenHimetric,
		bool useHimetric,
		double scale)
	{
		if (!useHimetric)
		{
			return new Windows.Foundation.Point(clientPx.X / scale, clientPx.Y / scale);
		}

		// HIMETRIC is DPI-independent, so it converts to logical px without the rasterization scale. Only the
		// screen-to-client translation needs it, and that part is whole pixels — subtracting it keeps the
		// sub-pixel precision of the HIMETRIC reading.
		var clientOriginLogicalX = (screenPx.X - clientPx.X) / scale;
		var clientOriginLogicalY = (screenPx.Y - clientPx.Y) / scale;
		return new Windows.Foundation.Point(
			screenHimetric.X / HimetricPerLogicalPx - clientOriginLogicalX,
			screenHimetric.Y / HimetricPerLogicalPx - clientOriginLogicalY);
	}
}
