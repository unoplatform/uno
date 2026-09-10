using System.Reflection;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_AppBar
{
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

	private static Storyboard GetStoryboard(AppBar appBar, string fieldName)
	{
		var field = typeof(AppBar).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
		Assert.IsNotNull(field, $"{fieldName} no longer exists on AppBar.");
		return (Storyboard)field.GetValue(appBar);
	}
}
