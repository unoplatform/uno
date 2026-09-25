namespace Uno.WinUI.Runtime.Skia.X11;

// Kept as a standalone type (rather than an X11PointerInputSource member) so the pure conversion
// math can be unit-tested without pulling in the rest of X11PointerInputSource's dependencies.
internal static class X11PointerCoordinateMath
{
	// XTranslateCoordinates only accepts whole pixels, so callers translate Math.Floor(eventX/Y) and pass the
	// result here together with the original sub-pixel event position, to re-add the fraction the floor dropped.
	internal static Windows.Foundation.Point ApplySubPixelFraction(int translatedX, int translatedY, double eventX, double eventY, double scale)
	{
		var clientX = translatedX + (eventX - System.Math.Floor(eventX));
		var clientY = translatedY + (eventY - System.Math.Floor(eventY));
		return new Windows.Foundation.Point(clientX / scale, clientY / scale);
	}
}
