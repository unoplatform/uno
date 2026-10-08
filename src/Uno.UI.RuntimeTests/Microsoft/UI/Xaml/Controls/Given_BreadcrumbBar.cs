using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.Extensions;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_BreadcrumbBar
{
	[TestCleanup]
	public void Cleanup()
	{
		TestServices.WindowHelper.WindowContent = null;
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25019")]
	public async Task When_Ellipsis_Flyout_Opened_Then_Background_Is_Acrylic()
	{
		var SUT = new BreadcrumbBar { ItemsSource = new List<string> { "Node 1", "Node 2", "Node 3" } };
		var host = new StackPanel { Width = 60 };
		host.Children.Add(SUT);

		TestServices.WindowHelper.WindowContent = host;
		await TestServices.WindowHelper.WaitForLoaded(SUT);
		await TestServices.WindowHelper.WaitForIdle();

		try
		{
			var repeater = SUT.FindFirstDescendant<ItemsRepeater>("PART_ItemsRepeater");
			var ellipsisItem = (BreadcrumbBarItem)repeater!.TryGetElement(0);
			var ellipsisButton = ellipsisItem.FindFirstDescendant<Button>("PART_ItemButton");
			((IInvokeProvider)new ButtonAutomationPeer(ellipsisButton).GetPattern(PatternInterface.Invoke)).Invoke();
			await TestServices.WindowHelper.WaitForIdle();

			var presenter = VisualTreeHelper.GetOpenPopupsForXamlRoot(SUT.XamlRoot)
				.Select(p => p.Child as FlyoutPresenter ?? p.Child.FindFirstDescendant<FlyoutPresenter>())
				.FirstOrDefault(p => p is not null);

			Assert.IsNotNull(presenter);
			// BreadcrumbBarEllipsisFlyoutPresenterBackground aliases AcrylicBackgroundFillColorDefaultBrush,
			// which only XamlControlsResources defines.
			Assert.IsInstanceOfType(presenter.Background, typeof(AcrylicBrush));
		}
		finally
		{
#if HAS_UNO
			VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
#endif
		}
	}
}
