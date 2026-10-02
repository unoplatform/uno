using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Runtime.Skia.Win32;

namespace Uno.UI.Tests.Windows_UI_Input;

/// <summary>
/// Tests for <see cref="Win32PointerCoordinateMath"/>, the pure part of the pointer screen-to-client
/// conversion (GetPointerDeviceRects and ScreenToClient themselves need a live device and HWND).
/// </summary>
[TestClass]
public class Given_Win32PointerCoordinateMath
{
	private const double Tolerance = 0.01;

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24716")]
	public void When_Mouse_Then_ClientPixelsAreDividedByScale()
	{
		var result = Win32PointerCoordinateMath.ComputeClientLogical(
			screenPx: (550, 300),
			clientPx: (150, 200),
			preciseScreenPx: null,
			scale: 2.0);

		Assert.AreEqual(75.0, result.X, Tolerance);
		Assert.AreEqual(100.0, result.Y, Tolerance);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24716")]
	public void When_SyntheticDevice_Then_MatchesPixelLocation()
	{
		// Captured from InjectSyntheticPointerInput on a 7680x2160 display at 150%: Windows sizes a synthetic
		// device at the display's logical DPI, so HIMETRIC lands exactly on the reported pixel.
		Assert.IsTrue(Win32PointerCoordinateMath.TryMapHimetricToScreenPx(
			himetric: (12559, 9613),
			deviceRect: (0, 0, 135466, 38100),
			displayRect: (0, 0, 7680, 2160),
			out var screen));

		var result = Win32PointerCoordinateMath.ComputeClientLogical(
			screenPx: (712, 545),
			clientPx: (101, 200),
			preciseScreenPx: screen,
			scale: 1.5);

		Assert.AreEqual(101 / 1.5, result.X, Tolerance);
		Assert.AreEqual(200 / 1.5, result.Y, Tolerance);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24716")]
	public void When_PanelDensityDiffersFromScale_Then_DeviceRectsDriveTheMapping()
	{
		// 13.3" 2560x1600 touch panel (286.5 x 179.06 mm, ~227 DPI) at 200%. Assuming 96 DPI x scale here
		// would put this touch at ~541 DIP instead of ~640.
		Assert.IsTrue(Win32PointerCoordinateMath.TryMapHimetricToScreenPx(
			himetric: (14330, 8953),
			deviceRect: (0, 0, 28650, 17906),
			displayRect: (0, 0, 2560, 1600),
			out var screen));

		Assert.AreEqual(1280.45, screen.X, Tolerance);
		Assert.AreEqual(800.0, screen.Y, Tolerance);

		var result = Win32PointerCoordinateMath.ComputeClientLogical(
			screenPx: (1280, 800),
			clientPx: (1280, 800),
			preciseScreenPx: screen,
			scale: 2.0);

		Assert.AreEqual(640.22, result.X, Tolerance);
		Assert.AreEqual(400.0, result.Y, Tolerance);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24716")]
	public void When_DisplayIsLeftOfPrimary_Then_DeviceOriginMapsToDisplayOrigin()
	{
		// Touch monitor placed left of the primary: HIMETRIC still starts at 0, the screen position is negative.
		Assert.IsTrue(Win32PointerCoordinateMath.TryMapHimetricToScreenPx(
			himetric: (2650, 1325),
			deviceRect: (0, 0, 53000, 29800),
			displayRect: (-1920, 0, 0, 1080),
			out var screen));

		var result = Win32PointerCoordinateMath.ComputeClientLogical(
			screenPx: (-1824, 48),
			clientPx: (176, 48),
			preciseScreenPx: screen,
			scale: 1.0);

		Assert.AreEqual(176.0, result.X, Tolerance);
		Assert.AreEqual(48.02, result.Y, Tolerance);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24716")]
	public void When_PreciseIsSubPixel_Then_FractionSurvivesClientTranslation()
	{
		var result = Win32PointerCoordinateMath.ComputeClientLogical(
			screenPx: (1000, 500),
			clientPx: (100, 50),
			preciseScreenPx: (1000.6, 500.3),
			scale: 1.5);

		Assert.AreEqual(100.6 / 1.5, result.X, Tolerance);
		Assert.AreEqual(50.3 / 1.5, result.Y, Tolerance);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24716")]
	public void When_PreciseDisagreesWithPixelLocation_Then_PixelLocationWins()
	{
		var result = Win32PointerCoordinateMath.ComputeClientLogical(
			screenPx: (1000, 500),
			clientPx: (100, 50),
			preciseScreenPx: (1180, 500),
			scale: 1.0);

		Assert.AreEqual(100.0, result.X, Tolerance);
		Assert.AreEqual(50.0, result.Y, Tolerance);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24716")]
	public void When_DeviceRectIsEmpty_Then_MappingFails()
	{
		Assert.IsFalse(Win32PointerCoordinateMath.TryMapHimetricToScreenPx(
			himetric: (100, 100),
			deviceRect: (0, 0, 0, 0),
			displayRect: (0, 0, 1920, 1080),
			out _));
	}
}
