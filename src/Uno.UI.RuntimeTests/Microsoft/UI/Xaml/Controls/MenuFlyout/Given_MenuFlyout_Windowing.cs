#nullable enable

using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

// Also meant to run on WinUI: whether a menu got a window of its own shows in the menu outgrowing its root.
[TestClass]
[RunsOnUIThread]
#if HAS_UNO
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
#endif
public class Given_MenuFlyout_Windowing
{
	private static async Task<double> OpenTallMenu(bool withPoint)
	{
		var target = new Button { Content = "Target" };
		await UITestHelper.Load(target);

		var flyout = new MenuFlyout { ShouldConstrainToRootBounds = true };
		for (var i = 0; i < 100; i++)
		{
			flyout.Items.Add(new MenuFlyoutItem { Text = "Item" });
		}

		var opened = false;
		flyout.Opened += (_, _) => opened = true;
		if (withPoint)
		{
			flyout.ShowAt(target, new Point(5, 5));
		}
		else
		{
			flyout.ShowAt(target);
		}

		await TestServices.WindowHelper.WaitFor(() => opened);
		await TestServices.WindowHelper.WaitForIdle();

		var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(target.XamlRoot);
		var height = ((FrameworkElement)popups[0].Child).ActualHeight;

		flyout.Hide();
		await TestServices.WindowHelper.WaitForIdle();

		return height;
	}

	[TestMethod]
	public async Task When_Constrained_And_ShowAt_Without_Options_Then_Stays_In_Root()
	{
		var height = await OpenTallMenu(withPoint: false);
		var root = TestServices.WindowHelper.XamlRoot.Size.Height;

		Assert.IsTrue(height <= root, $"The menu is {height} tall in a root of {root}.");
	}

	[TestMethod]
	public async Task When_Constrained_And_ShowAt_With_Point_Then_Outgrows_Root()
	{
		var height = await OpenTallMenu(withPoint: true);
		var root = TestServices.WindowHelper.XamlRoot.Size.Height;

		Assert.IsTrue(height > root, $"The menu is {height} tall in a root of {root}.");
	}
}
