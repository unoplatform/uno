using Microsoft.UI.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Runtime.MacOS;

namespace Uno.UI.Tests.Windows_UI_Input;

/// <summary>
/// Tests for <see cref="MacOSTitleBarMath"/>, the pure part of the macOS title bar support
/// (the traffic-light bounds and the hit-test callback themselves need a live NSWindow).
/// </summary>
[UITestClass]
public class Given_MacOSTitleBarMath
{
	// Standard traffic lights: close at x = 7pt, zoom ending at x = 59pt.
	private const double ButtonsMinX = 7;
	private const double ButtonsMaxX = 59;

	[TestMethod]
	public void When_Buttons_On_Left_LeftInset_Mirrors_Leading_Spacing()
	{
		var (left, right) = MacOSTitleBarMath.GetInsets(ButtonsMinX, ButtonsMaxX, windowWidth: 800, scale: 1);

		Assert.AreEqual(66, left);
		Assert.AreEqual(0, right);
	}

	[TestMethod]
	public void When_Retina_Insets_Are_Physical_Pixels()
	{
		// WinUI reports the insets in physical pixels, TitleBar uses them as-is.
		var (left, right) = MacOSTitleBarMath.GetInsets(ButtonsMinX, ButtonsMaxX, windowWidth: 800, scale: 2);

		Assert.AreEqual(132, left);
		Assert.AreEqual(0, right);
	}

	[TestMethod]
	public void When_Fractional_Scale_Inset_Is_Rounded_Up()
	{
		var (left, _) = MacOSTitleBarMath.GetInsets(ButtonsMinX, ButtonsMaxX, windowWidth: 800, scale: 1.5);

		Assert.AreEqual(99, left);

		(left, _) = MacOSTitleBarMath.GetInsets(7.25, 59, windowWidth: 800, scale: 1.5);

		Assert.AreEqual(100, left);
	}

	[TestMethod]
	public void When_Buttons_On_Right_RightInset_Mirrors_Trailing_Spacing()
	{
		// Right-to-left system layout: close at 793pt, the group starts at 741pt.
		var (left, right) = MacOSTitleBarMath.GetInsets(800 - ButtonsMaxX, 800 - ButtonsMinX, windowWidth: 800, scale: 2);

		Assert.AreEqual(0, left);
		Assert.AreEqual(132, right);
	}

	[TestMethod]
	public void When_No_Buttons_No_Insets()
	{
		Assert.AreEqual((0, 0), MacOSTitleBarMath.GetInsets(0, 0, windowWidth: 800, scale: 2));
	}

	[TestMethod]
	[DataRow(NonClientRegionKind.Caption, true)]
	[DataRow(NonClientRegionKind.Icon, true)]
	[DataRow(NonClientRegionKind.Passthrough, false)]
	[DataRow(NonClientRegionKind.Close, false)]
	[DataRow(NonClientRegionKind.Maximize, false)]
	[DataRow(NonClientRegionKind.Minimize, false)]
	[DataRow(NonClientRegionKind.TopBorder, false)]
	public void When_Region_Hit_Maps_To_Pointer_Action(NonClientRegionKind region, bool expectDrag)
	{
		var expected = expectDrag ? MacOSNonClientPointerAction.Drag : MacOSNonClientPointerAction.Client;
		Assert.AreEqual(expected, MacOSTitleBarMath.GetPointerAction(region));
	}

	[TestMethod]
	public void When_No_Region_Hit_Pointer_Goes_To_Content()
	{
		Assert.AreEqual(MacOSNonClientPointerAction.Client, MacOSTitleBarMath.GetPointerAction(null));
	}
}
