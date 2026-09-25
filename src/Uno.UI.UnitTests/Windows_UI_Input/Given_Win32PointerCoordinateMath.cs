using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Runtime.Skia.Win32;

namespace Uno.UI.Tests.Windows_UI_Input;

/// <summary>
/// Tests for <see cref="Win32PointerCoordinateMath.ComputeClientLogical"/>, the pure part of the pointer
/// screen-to-client conversion (the P/Invoke ScreenToClient call itself is not testable off Windows).
/// </summary>
[TestClass]
public class Given_Win32PointerCoordinateMath
{
	private const double Tolerance = 0.0001;

	[TestMethod]
	public void When_Mouse_Then_ClientPixelsAreDividedByScale()
	{
		// Mouse never uses HIMETRIC: screenPx/screenHimetric are irrelevant, only clientPx and scale matter.
		var result = Win32PointerCoordinateMath.ComputeClientLogical(
			screenPx: (9999, 9999),
			clientPx: (150, 200),
			screenHimetric: (0, 0),
			useHimetric: false,
			scale: 2.0);

		Assert.AreEqual(75.0, result.X, Tolerance);
		Assert.AreEqual(100.0, result.Y, Tolerance);
	}

	[TestMethod]
	public void When_Touch_AtWindowOrigin_Then_HimetricConvertsAtOneInchPerNinetySixPx()
	{
		// Window at (0,0): screenPx == clientPx, so the origin-offset correction is zero and the result
		// is a pure unit conversion. 1 inch = 2540 HIMETRIC units = 96 logical px at 100% scale.
		var result = Win32PointerCoordinateMath.ComputeClientLogical(
			screenPx: (96, 192),
			clientPx: (96, 192),
			screenHimetric: (2540, 5080), // 1 inch, 2 inches
			useHimetric: true,
			scale: 1.0);

		Assert.AreEqual(96.0, result.X, Tolerance);
		Assert.AreEqual(192.0, result.Y, Tolerance);
	}

	[TestMethod]
	public void When_Touch_WithWindowOffsetAndScale_Then_OriginIsSubtractedInLogicalSpace()
	{
		// Window top-left sits at screen px (400, 100) i.e. clientPx = screenPx - (400, 100), and the
		// display is at 200% scale. The HIMETRIC reading itself carries no scale (it's DPI-independent);
		// only the whole-pixel screen->client offset needs it subtracted in logical space.
		var result = Win32PointerCoordinateMath.ComputeClientLogical(
			screenPx: (496, 292), // arbitrary pixel position, only used for the origin offset
			clientPx: (96, 192), // screenPx - windowOrigin(400, 100)
			screenHimetric: (2540, 5080), // 1 inch, 2 inches -> 96px, 192px at 100%
			useHimetric: true,
			scale: 2.0);

		// HIMETRIC->logical px is scale-independent (96, 192), minus the window origin in logical px
		// ((400, 100) / scale = (200, 50)).
		Assert.AreEqual(96.0 - 200.0, result.X, Tolerance);
		Assert.AreEqual(192.0 - 50.0, result.Y, Tolerance);
	}
}
