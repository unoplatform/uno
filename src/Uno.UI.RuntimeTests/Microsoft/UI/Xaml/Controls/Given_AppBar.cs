using System.Reflection;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_AppBar
{
	// Everything below asserts on members of the port itself - AppBar.XcpRound, Popup.IsSubMenu
	// and the private storyboard fields. Native WinUI exposes none of them, so the whole class
	// only compiles and runs against Uno.
#if HAS_UNO
	[TestMethod]
	public async Task When_Template_Applied_Then_Overlay_Storyboards_Are_Resolved()
	{
		var appBar = new AppBar();

		TestServices.WindowHelper.WindowContent = appBar;
		await TestServices.WindowHelper.WaitForLoaded(appBar);
		await TestServices.WindowHelper.WaitForIdle();

		// The animations are x:Key'd entries of LayoutRoot.Resources rather than named template
		// parts, so only a resource lookup finds them. Read back the fields the port assigns.
		Assert.IsNotNull(GetStoryboard(appBar, "m_overlayOpeningStoryboard"), "OverlayOpeningAnimation was not resolved.");
		Assert.IsNotNull(GetStoryboard(appBar, "m_overlayClosingStoryboard"), "OverlayClosingAnimation was not resolved.");
	}

	[TestMethod]
	public void When_XcpRound_Then_Halves_Round_Up()
	{
		// XcpRound is floor(x + 0.5), so halves always go up - unlike Math.Round, which
		// rounds them to even, and unlike MidpointRounding.AwayFromZero on negatives.
		Assert.AreEqual(1d, AppBar.XcpRound(0.5));
		Assert.AreEqual(2d, AppBar.XcpRound(1.5));
		Assert.AreEqual(3d, AppBar.XcpRound(2.5));
		Assert.AreEqual(0d, AppBar.XcpRound(-0.5));
		Assert.AreEqual(-1d, AppBar.XcpRound(-1.5));
		Assert.AreEqual(-2d, AppBar.XcpRound(-2.5));
	}

	[TestMethod]
	public void When_Popup_Is_SubMenu_Then_It_Counts_As_LightDismiss()
	{
		var popup = new Popup { IsLightDismissEnabled = false };

		Assert.IsFalse(popup.IsSelfOrAncestorLightDismiss());

		popup.IsSubMenu = true;

		// A sub-menu popup is always hosted by a light-dismiss parent menu, so the AppBar
		// must not build a second, competing light-dismiss layer inside it.
		Assert.IsTrue(popup.IsSelfOrAncestorLightDismiss());
	}

	private static Storyboard GetStoryboard(AppBar appBar, string fieldName)
	{
		var field = typeof(AppBar).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
		Assert.IsNotNull(field, $"{fieldName} no longer exists on AppBar.");
		return (Storyboard)field.GetValue(appBar);
	}
#endif
}
