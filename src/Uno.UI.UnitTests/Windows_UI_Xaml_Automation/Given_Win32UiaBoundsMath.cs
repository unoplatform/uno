using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Runtime.Win32;

namespace Uno.UI.Tests.Windows_UI_Xaml_Automation;

/// <summary>
/// Tests for <see cref="Win32UiaBoundsMath"/>, the logical-to-physical conversion behind the Win32 UIA BoundingRectangle.
/// </summary>
[UITestClass]
public class Given_Win32UiaBoundsMath
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26757")]
	public void When_FloatBounds_At125Percent_Then_EdgesSnapToCoveredPixels()
	{
		// Global bounds come from a float transform: 853.6 DIP is 853.5999755859375.
		var result = Win32UiaBoundsMath.ToPhysicalScreenRect(
			((double)853.6f, (double)100f, (double)93.6f, (double)40f),
			scale: 1.25,
			clientOrigin: (0, 0));

		Assert.AreEqual(1067, (int)result.Left);
		Assert.AreEqual(125, (int)result.Top);
		Assert.AreEqual(117, (int)result.Width);
		Assert.AreEqual(50, (int)result.Height);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26757")]
	public void When_ClientOriginOffset_Then_RightEdgeIsPreserved()
	{
		// Rounding the width separately from the left edge would move the right edge.
		var result = Win32UiaBoundsMath.ToPhysicalScreenRect(
			(10.3, 20.3, 10.3, 10.3),
			scale: 1.5,
			clientOrigin: (200, 300));

		Assert.AreEqual(215.0, result.Left);
		Assert.AreEqual(330.0, result.Top);
		Assert.AreEqual(16.0, result.Width); // right edge 230.9 -> 231, not round(15.45) = 15
		Assert.AreEqual(16.0, result.Height);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26757")]
	public void When_Scale100_Then_IntegerBoundsAreUnchanged()
	{
		var result = Win32UiaBoundsMath.ToPhysicalScreenRect(
			(12, 34, 56, 78),
			scale: 1.0,
			clientOrigin: (100, 200));

		Assert.AreEqual(112.0, result.Left);
		Assert.AreEqual(234.0, result.Top);
		Assert.AreEqual(56.0, result.Width);
		Assert.AreEqual(78.0, result.Height);
	}
}
