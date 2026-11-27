using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Runtime.Win32;

namespace Uno.UI.Tests.Windows_Graphics_Display;

/// <summary>
/// Tests for <see cref="Win32DisplayScale"/>, which reads <c>UNO_DISPLAY_SCALE_OVERRIDE</c> for the Win32 host.
/// </summary>
[TestClass]
public class Given_Win32DisplayScale
{
	// Frames of a WS_OVERLAPPEDWINDOW (window size minus client size) as AdjustWindowRectExForDpi reports them on
	// Windows 11. They don't scale evenly with the DPI: 120 DPI gives 18 x 47, not 1.25 x (16 x 39).
	private static readonly (int Width, int Height) Frame96 = (16, 39);
	private static readonly (int Width, int Height) Frame120 = (18, 47);
	private static readonly (int Width, int Height) Frame144 = (22, 56);

	private static readonly (int Width, int Height) LaunchSize = (1024, 768);

	[TestMethod]
	[DataRow("1", 1f)]
	[DataRow("1.5", 1.5f)]
	[DataRow("2", 2f)]
	[DataRow(" 1.25 ", 1.25f)]
	[DataRow("1e0", 1f)]
	public void When_Override_Is_Positive_Number_Then_It_Is_Used(string value, float expected)
	{
		Assert.IsTrue(Win32DisplayScale.TryParseOverride(value, out var scale));
		Assert.AreEqual(expected, scale);
	}

	[TestMethod]
	[DataRow(null)]
	[DataRow("")]
	[DataRow("abc")]
	[DataRow("0")]
	[DataRow("-1")]
	[DataRow("NaN")]
	[DataRow("Infinity")]
	[DataRow("1,5")]
	public void When_Override_Is_Not_A_Scale_Then_It_Is_Ignored(string value)
	{
		Assert.IsFalse(Win32DisplayScale.TryParseOverride(value, out _));
	}

	[TestMethod]
	[DataRow(1e-45f, 0.25f)]
	[DataRow(0.1f, 0.25f)]
	[DataRow(0.25f, 0.25f)]
	[DataRow(1f, 1f)]
	[DataRow(8f, 8f)]
	[DataRow(20f, 8f)]
	public void When_Override_Is_Out_Of_Range_Then_It_Is_Clamped(float value, float expected)
	{
		Assert.AreEqual(expected, Win32DisplayScale.ClampOverride(value));
	}

	[TestMethod]
	[DataRow(96u, 96f)]
	[DataRow(120u, 120f)]
	[DataRow(144u, 144f)]
	public void When_No_Override_Then_Window_Dpi_Is_Used(uint windowDpi, float expected)
	{
		Assert.AreEqual(expected, Win32DisplayScale.GetLogicalDpi(windowDpi, null));
	}

	[TestMethod]
	[DataRow(120u, 1f, 96f)]
	[DataRow(96u, 1.5f, 144f)]
	[DataRow(96u, 2f, 192f)]
	[DataRow(144u, 1.25f, 120f)]
	public void When_Override_Then_It_Wins_Over_Window_Dpi(uint windowDpi, float scaleOverride, float expected)
	{
		Assert.AreEqual(expected, Win32DisplayScale.GetLogicalDpi(windowDpi, scaleOverride));
	}

	[TestMethod]
	[DataRow(0.25f, 24u)]
	[DataRow(1f, 96u)]
	[DataRow(1.25f, 120u)]
	[DataRow(1.5f, 144u)]
	[DataRow(8f, 768u)]
	public void When_Override_Then_Frame_Is_Measured_At_Its_Dpi(float scaleOverride, uint expected)
	{
		Assert.AreEqual(expected, Win32DisplayScale.GetOverrideDpi(scaleOverride));
	}

	[TestMethod]
	public void When_Override_Is_Fractional_Then_Resolution_Scale_Is_Rounded()
	{
		var raw = Win32DisplayScale.GetLogicalDpi(96, 1.15f) / 96.0;

		Assert.AreEqual(115, Win32DisplayScale.GetResolutionScalePercent(1.15f, raw));
		Assert.AreEqual(150, Win32DisplayScale.GetResolutionScalePercent(1.5f, 1.5));
	}

	[TestMethod]
	public void When_No_Override_Then_Resolution_Scale_Is_Truncated_As_Before()
	{
		Assert.AreEqual(125, Win32DisplayScale.GetResolutionScalePercent(null, 120 / 96.0));
		Assert.AreEqual(114, Win32DisplayScale.GetResolutionScalePercent(null, 110 / 96.0));
	}

	[TestMethod]
	public void When_Override_1_On_120_Dpi_Then_Client_Area_Is_That_Of_96_Dpi()
	{
		var size = Win32DisplayScale.GetWindowSizeForOverride(LaunchSize, 1f, Frame96, Frame120);

		Assert.AreEqual((1026, 776), size);
		AssertClientSize(size, Frame120, 1f, (1008, 729));
	}

	[TestMethod]
	public void When_Override_1_On_144_Dpi_Then_Client_Area_Is_That_Of_96_Dpi()
	{
		var size = Win32DisplayScale.GetWindowSizeForOverride(LaunchSize, 1f, Frame96, Frame144);

		Assert.AreEqual((1030, 785), size);
		AssertClientSize(size, Frame144, 1f, (1008, 729));
	}

	[TestMethod]
	public void When_Override_1_5_On_96_Dpi_Then_Client_Area_Is_That_Of_144_Dpi()
	{
		var size = Win32DisplayScale.GetWindowSizeForOverride(LaunchSize, 1.5f, Frame144, Frame96);

		Assert.AreEqual((1530, 1135), size);
		AssertClientSize(size, Frame96, 1.5f, (1514 / 1.5, 1096 / 1.5));
	}

	[TestMethod]
	public void When_Override_Equals_Monitor_Scale_Then_Size_Is_What_Windows_Scaling_Gives()
	{
		// Without an override, the window is resized to its launch size times the monitor's scale.
		var size = Win32DisplayScale.GetWindowSizeForOverride(LaunchSize, 1.25f, Frame120, Frame120);

		Assert.AreEqual((1280, 960), size);
	}

	private static void AssertClientSize((int Width, int Height) windowSize, (int Width, int Height) realFrame, float scale, (double Width, double Height) expectedLogical)
	{
		Assert.AreEqual(expectedLogical.Width, (windowSize.Width - realFrame.Width) / scale, 0.01);
		Assert.AreEqual(expectedLogical.Height, (windowSize.Height - realFrame.Height) / scale, 0.01);
	}
}
