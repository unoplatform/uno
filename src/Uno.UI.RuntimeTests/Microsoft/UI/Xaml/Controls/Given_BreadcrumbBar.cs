using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Helpers;

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

	[TestMethod]
	public async Task When_Items_Arranged_SizeOfSet_Is_Readable()
	{
		var bar = new BreadcrumbBar { ItemsSource = new[] { "Home", "Library", "Buttons" } };

		await UITestHelper.Load(bar);

		var items = Descendants(bar).OfType<BreadcrumbBarItem>().Where(i => AutomationProperties.GetPositionInSet(i) > 0).ToArray();
		Assert.AreEqual(3, items.Length);
		Assert.IsTrue(items.All(i => AutomationProperties.GetSizeOfSet(i) == 3));
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26081")]
	public async Task When_Last_Item_Content_Is_UIElement_Then_Presented_By_Visible_Presenter()
	{
		var bar = new BreadcrumbBar { Width = 400, ItemsSource = new[] { "Home", "Current" } };

		await UITestHelper.Load(bar);

		var repeater = bar.FindFirstDescendant<ItemsRepeater>("PART_ItemsRepeater");
		var lastItem = (BreadcrumbBarItem)repeater!.TryGetElement(2);
		var content = new Border { Width = 50, Height = 20 };
		lastItem.Content = content;
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual("PART_LastItemContentPresenter", (VisualTreeHelper.GetParent(content) as FrameworkElement)?.Name);
		Assert.IsTrue(content.IsLoaded);
		Assert.AreEqual(50, content.ActualWidth);
	}

	// By reading CContentPresenter::ApplyTemplate, WinUI fails with E_INVALIDARG here as the element is still
	// associated with the other presenter; Uno moves it instead (see UIElement.AddChild).
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26081")]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_Item_Stops_Being_Last_Then_UIElement_Content_Follows_Visible_Presenter()
	{
		var first = new Border { Width = 50, Height = 20 };
		var second = new Border { Width = 60, Height = 20 };
		var added = new Border { Width = 70, Height = 20 };
		var items = new ObservableCollection<UIElement> { first, second };
		var bar = new BreadcrumbBar { Width = 400, ItemsSource = items };

		await UITestHelper.Load(bar);
		Assert.AreEqual("PART_LastItemContentPresenter", (VisualTreeHelper.GetParent(second) as FrameworkElement)?.Name);

		items.Add(added);
		await TestServices.WindowHelper.WaitForIdle();
		Assert.AreEqual("PART_ItemContentPresenter", (VisualTreeHelper.GetParent(second) as FrameworkElement)?.Name);
		Assert.AreEqual("PART_LastItemContentPresenter", (VisualTreeHelper.GetParent(added) as FrameworkElement)?.Name);
		Assert.AreEqual(60, second.ActualWidth);

		items.Remove(added);
		await TestServices.WindowHelper.WaitForIdle();
		Assert.AreEqual("PART_LastItemContentPresenter", (VisualTreeHelper.GetParent(second) as FrameworkElement)?.Name);
		Assert.AreEqual(60, second.ActualWidth);
	}

	private static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
	{
		for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
		{
			var child = VisualTreeHelper.GetChild(parent, i);
			yield return child;
			foreach (var descendant in Descendants(child))
			{
				yield return descendant;
			}
		}
	}
}
