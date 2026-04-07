using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_Page
{
	// Repro tests for https://github.com/unoplatform/uno/issues/1479
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/1479")]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_Page_BottomAppBar_Is_Visible()
	{
		// Issue: Page.BottomAppBar is not visible when set in XAML.
		// The property is marked [NotImplemented] on BottomAppBar, meaning it stores the value
		// but does not actually render the AppBar at the bottom of the page.
		// Expected: The BottomAppBar should be visible and at the bottom of the page.

		var commandBar = new CommandBar
		{
			ClosedDisplayMode = AppBarClosedDisplayMode.Compact,
		};
		commandBar.PrimaryCommands.Add(new AppBarButton { Label = "Refresh" });
		commandBar.PrimaryCommands.Add(new AppBarButton { Label = "Settings" });

		var page = new Page
		{
			BottomAppBar = commandBar,
			Content = new TextBlock { Text = "Page content" }
		};

		await UITestHelper.Load(page);
		await UITestHelper.WaitForIdle();

		// The BottomAppBar should be in the visual tree
		var parent = VisualTreeHelper.GetParent(commandBar);
		Assert.IsNotNull(parent,
			"Page.BottomAppBar CommandBar should be in the visual tree (have a parent), " +
			"but BottomAppBar is [NotImplemented] and is not rendered.");

		// The CommandBar should have non-zero actual height
		Assert.IsTrue(commandBar.ActualHeight > 0,
			$"Expected BottomAppBar CommandBar to have ActualHeight > 0, but got {commandBar.ActualHeight}. " +
			$"Page.BottomAppBar is marked [NotImplemented].");
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/1479")]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_Page_TopAppBar_Is_Visible()
	{
		// Issue: Page.TopAppBar / Page.BottomAppBar not supported.
		// TopAppBar property currently lacks [NotImplemented] on the property getter/setter,
		// but there is no rendering implementation.
		// Expected: The TopAppBar should render at the top of the page.

		var commandBar = new CommandBar
		{
			ClosedDisplayMode = AppBarClosedDisplayMode.Compact,
		};
		commandBar.PrimaryCommands.Add(new AppBarButton { Label = "Back" });

		var page = new Page
		{
			TopAppBar = commandBar,
			Content = new TextBlock { Text = "Page content" }
		};

		await UITestHelper.Load(page);
		await UITestHelper.WaitForIdle();

		// The TopAppBar should be in the visual tree
		var parent = VisualTreeHelper.GetParent(commandBar);
		Assert.IsNotNull(parent,
			"Page.TopAppBar CommandBar should be in the visual tree (have a parent), " +
			"but it appears not to be rendered.");
	}
}
