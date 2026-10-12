namespace Uno.UI.Runtime.Win32;

// Kept as a standalone type (rather than a Win32WindowWrapper member), using only plain doubles and
// Windows.Foundation.Point, so the pure conversion math can be linked into Uno.UI.UnitTests and unit
// tested there without pulling in Win32WindowWrapper's HWND/P-Invoke dependencies.
internal static class Win32PointerCoordinateMath
{
	// ptPixelLocation is the same reading rounded to whole pixels, so a correct mapping is never further off than this.
	internal const double MaxPreciseDeviationPx = 1.0;

	// HIMETRIC is in the digitizer's own coordinate space (its physical size, in 0.01 mm), which
	// GetPointerDeviceRects maps onto a display rect in screen pixels. Its density is per device:
	// it only matches 96 DPI x scale when the panel happens to, never for an external pen tablet.
	internal static bool TryMapHimetricToScreenPx(
		(double X, double Y) himetric,
		(double Left, double Top, double Right, double Bottom) deviceRect,
		(double Left, double Top, double Right, double Bottom) displayRect,
		out (double X, double Y) screenPx)
	{
		var deviceWidth = deviceRect.Right - deviceRect.Left;
		var deviceHeight = deviceRect.Bottom - deviceRect.Top;
		if (deviceWidth <= 0 || deviceHeight <= 0)
		{
			screenPx = default;
			return false;
		}

		screenPx = (
			displayRect.Left + (himetric.X - deviceRect.Left) * (displayRect.Right - displayRect.Left) / deviceWidth,
			displayRect.Top + (himetric.Y - deviceRect.Top) * (displayRect.Bottom - displayRect.Top) / deviceHeight);
		return true;
	}

	internal static Windows.Foundation.Point ComputeClientLogical(
		(double X, double Y) screenPx,
		(double X, double Y) clientPx,
		(double X, double Y)? preciseScreenPx,
		double scale)
	{
		// The screen-to-client translation is a whole-pixel offset, so subtracting it keeps any sub-pixel precision.
		var clientOriginX = screenPx.X - clientPx.X;
		var clientOriginY = screenPx.Y - clientPx.Y;

		// Disagreeing by more than a pixel means the device-to-display mapping isn't what we assume
		// (e.g. a rotated display), so the integer position the system resolved wins.
		var (x, y) = preciseScreenPx is { } precise
			&& System.Math.Abs(precise.X - screenPx.X) <= MaxPreciseDeviationPx
			&& System.Math.Abs(precise.Y - screenPx.Y) <= MaxPreciseDeviationPx
				? precise
				: screenPx;

		return new Windows.Foundation.Point((x - clientOriginX) / scale, (y - clientOriginY) / scale);
	}
}
