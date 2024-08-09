using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

using Windows.UI.Input.Preview.Injection;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;

using Private.Infrastructure;
using Uno.Extensions;
using Uno.UI.RuntimeTests.Helpers;

using TabView = Microsoft/* UWP don't rename */.UI.Xaml.Controls.TabView;
using TabViewItem = Microsoft/* UWP don't rename */.UI.Xaml.Controls.TabViewItem;

using static Uno.UI.Extensions.ViewExtensions;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_TabView
{
#if HAS_UNO
	[TestMethod]
#if !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is only supported on skia")]
#endif
	public async Task When_First_Item_Unselected()
	{
		var SUT = new TabView
		{
			TabItems =
			{
				new TabViewItem
				{
					Header = "Tab1"
				},
				new TabViewItem
				{
					Header = "Tab2"
				}
			}
		};

		await UITestHelper.Load(SUT);

		Assert.IsTrue(((TabViewItem)SUT.TabItems[0]).IsSelected);
		Assert.IsFalse(((TabViewItem)SUT.TabItems[1]).IsSelected);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var finger = injector.GetFinger();

		finger.Press(SUT.GetAbsoluteBounds().GetCenter());
		finger.Release();

		await WindowHelper.WaitForIdle();

		Assert.IsFalse(((TabViewItem)SUT.TabItems[0]).IsSelected);
		Assert.IsTrue(((TabViewItem)SUT.TabItems[1]).IsSelected);
	}
#endif

	[TestMethod]
	public async Task When_Leading_Item_Removed()
	{
		var source = new ObservableCollection<int>(Enumerable.Range(0, 100));
		var setup = new TabView { Width = 400, Height = 200, TabItemsSource = source };

		await UITestHelper.Load(setup);
		await Task.Delay(1000);

		var presenter = setup.FindFirstDescendant<ContentPresenter>("TabContentPresenter");
		setup.TabCloseRequested += (s, e) => source.Remove((int)e.Item);

		setup.SelectedItem = 5;
		await WindowHelper.WaitForIdle();

		// Remove a nearby materialized item that is before the selected item.
		// On android, this will cause a ContainerFromItem bug to manifest
		// where the items are no longer in sync with the materialized containers.
		// This is guaranteed to repro if both removed & selected items are currently materialized.
		source.Remove(4);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(setup.SelectedItem, presenter.DataContext, "TabView content was changed.");
	}

	[TestMethod]
	public async Task When_Tab_Removed()
	{
		var SUT = new TabView
		{
			TabItems =
			{
				new TabViewItem { Header = "Tab 1" },
				new TabViewItem { Header = "Tab 2" }
			}
		};
		SUT.TabCloseRequested += (sender, args) => sender.TabItems.Remove(args.Tab);

		await UITestHelper.Load(SUT);

		SUT.SelectedIndex = 1;
		await WindowHelper.WaitForIdle();

		var closeButton = (Button)((TabViewItem)SUT.TabItems[0]).FindName("CloseButton");
		new ButtonAutomationPeer(closeButton).Invoke();
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(1, SUT.TabItems.Count);
		Assert.AreEqual(0, SUT.SelectedIndex);
		Assert.AreEqual("Tab 2", ((TabViewItem)SUT.TabItems[0]).Header);
	}
}
