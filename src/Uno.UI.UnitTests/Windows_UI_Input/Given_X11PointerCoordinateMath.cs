using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Runtime.X11;

namespace Uno.UI.Tests.Windows_UI_Input;

/// <summary>
/// Tests for <see cref="X11PointerCoordinateMath.ApplySubPixelFraction"/>, the pure part of the
/// XTranslateCoordinates workaround (XTranslateCoordinates itself only accepts whole pixels, so
/// the sub-pixel fraction of the XInput2 event position has to be re-added after translation).
/// </summary>
[TestClass]
public class Given_X11PointerCoordinateMath
{
	private const double Tolerance = 0.0001;

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24716")]
	public void When_EventIsWholePixel_Then_FractionIsZero()
	{
		var result = X11PointerCoordinateMath.ApplySubPixelFraction(
			translatedX: 100,
			translatedY: 200,
			eventX: 10.0,
			eventY: 20.0,
			scale: 1.0);

		Assert.AreEqual(100.0, result.X, Tolerance);
		Assert.AreEqual(200.0, result.Y, Tolerance);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24716")]
	public void When_EventHasSubPixelFraction_Then_FractionSurvivesTranslation()
	{
		// data.event_x/y = 10.75/20.25, so the caller floors to (10, 20) before calling
		// XTranslateCoordinates, and the 0.75/0.25 fraction must reappear in the result.
		var result = X11PointerCoordinateMath.ApplySubPixelFraction(
			translatedX: 100, // XTranslateCoordinates(..., floor(10.75)=10, floor(20.25)=20, ...) -> 100
			translatedY: 200,
			eventX: 10.75,
			eventY: 20.25,
			scale: 1.0);

		Assert.AreEqual(100.75, result.X, Tolerance);
		Assert.AreEqual(200.25, result.Y, Tolerance);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24716")]
	public void When_ScaleIsAppliedAfterFraction_Then_FractionIsNotLostToTruncation()
	{
		// At scale 1.5, a naive int-based path would have truncated the 0.5 fraction before dividing.
		var result = X11PointerCoordinateMath.ApplySubPixelFraction(
			translatedX: 90,
			translatedY: 90,
			eventX: 5.5,
			eventY: 5.5,
			scale: 1.5);

		Assert.AreEqual(90.5 / 1.5, result.X, Tolerance);
		Assert.AreEqual(90.5 / 1.5, result.Y, Tolerance);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24716")]
	public void When_EventIsNegative_Then_FractionIsInZeroToOneRange()
	{
		// The caller floors -1.5 to -2 (not -1, as a truncating cast would), so the fraction is 0.5.
		var result = X11PointerCoordinateMath.ApplySubPixelFraction(
			translatedX: 98, // XTranslateCoordinates(..., floor(-1.5)=-2, floor(-0.25)=-1, ...) -> 98 / 199
			translatedY: 199,
			eventX: -1.5,
			eventY: -0.25,
			scale: 1.0);

		Assert.AreEqual(98.5, result.X, Tolerance);
		Assert.AreEqual(199.75, result.Y, Tolerance);
	}
}
