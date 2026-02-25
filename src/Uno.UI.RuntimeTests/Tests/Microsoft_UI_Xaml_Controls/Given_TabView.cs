using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

using Windows.UI.Input.Preview.Injection;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;

using Private.Infrastructure;
using Uno.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Toolkit.DevTools.Input;

using TabView = Microsoft.UI.Xaml.Controls.TabView;
using TabViewItem = Microsoft.UI.Xaml.Controls.TabViewItem;

using static Uno.UI.Extensions.ViewExtensions;
using static Private.Infrastructure.TestServices;

#if __APPLE_UIKIT__
using UIKit;
#endif

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_TabView
{
#if HAS_UNO
	[TestMethod]
#if !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is not supported on this platform.")]
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

		var secondItem = (TabViewItem)SUT.TabItems[1];

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var finger = injector.GetFinger();

		finger.Press(secondItem.GetAbsoluteBounds().GetCenter());
		finger.Release();

		await WindowHelper.WaitForIdle();

		Assert.IsFalse(((TabViewItem)SUT.TabItems[0]).IsSelected);
		Assert.IsTrue(((TabViewItem)SUT.TabItems[1]).IsSelected);
	}
#endif

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeIOS)]
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

		Assert.HasCount(1, SUT.TabItems);
		Assert.AreEqual(0, SUT.SelectedIndex);
		Assert.AreEqual("Tab 2", ((TabViewItem)SUT.TabItems[0]).Header);
	}

	[TestMethod]
	public async Task When_SelectedItem_Changed()
	{
		var SUT = new TabView
		{
			TabItems =
			{
				new TabViewItem { Header = "Tab 1" },
				new TabViewItem { Header = "Tab 2" }
			}
		};

		await UITestHelper.Load(SUT);

		Assert.AreEqual(0, SUT.SelectedIndex);

		SUT.SelectedItem = SUT.TabItems[1];

		await WindowHelper.WaitForIdle();

		Assert.AreEqual(1, SUT.SelectedIndex);
	}

	[TestMethod]
	public async Task When_DataBinding()
	{
		var vm = new ViewModel();
		var SUT = new TabView();

		SUT.SetBinding(TabView.TabItemsSourceProperty, new Microsoft.UI.Xaml.Data.Binding() { Path = new("TabItems") });
		SUT.SetBinding(TabView.SelectedItemProperty, new Microsoft.UI.Xaml.Data.Binding() { Path = new("SelectedItem"), Mode = Microsoft.UI.Xaml.Data.BindingMode.TwoWay });

		SUT.DataContext = vm;

		await UITestHelper.Load(SUT);

		Assert.HasCount(2, SUT.TabItems);
		Assert.AreEqual(-1, SUT.SelectedIndex);
	}

#if !WINAPPSDK // GetTemplateChild is protected in UWP while public in Uno.
	[TestMethod]
	public async Task When_Items_Should_ShowHeader()
	{
		var SUT = new TabView
		{
			TabItems =
			{
				new TabViewItem { Header = "Tab 1" },
				new TabViewItem { Header = "Tab 2" }
			}
		};

		await UITestHelper.Load(SUT);

		var tabviewItem1 = SUT.ContainerFromIndex(0) as TabViewItem;
		var headerPresenter1 = (ContentPresenter)tabviewItem1.GetTemplateChild("ContentPresenter");
		Assert.IsGreaterThan(0, headerPresenter1.ActualWidth, "TabViewItem header for index  0 should have a non-zero width.");
		Assert.IsGreaterThan(0, headerPresenter1.ActualHeight, "TabViewItem header for index  0 should have a non-zero height.");

		var closeButton1 = (Button)tabviewItem1.GetTemplateChild("CloseButton");

		var buttonLabel1 =
#if __APPLE_UIKIT__
		closeButton1.FindFirstChild<ImplicitTextBlock>();
#else
		((ContentPresenter)closeButton1.GetTemplateChild("ContentPresenter")).FindFirstChild<ImplicitTextBlock>();
#endif

		Assert.IsGreaterThan(0, buttonLabel1.ActualWidth, "TabViewItem Button for index 0 should have a non-zero width.");
		Assert.IsGreaterThan(0, buttonLabel1.ActualHeight, "TabViewItem Button  for index 0 should have a non-zero height.");

		var tabviewItem2 = SUT.ContainerFromIndex(1) as TabViewItem;
		var headerPresenter2 = (ContentPresenter)tabviewItem2.GetTemplateChild("ContentPresenter");
		Assert.IsGreaterThan(0, headerPresenter2.ActualWidth, "TabViewItem header for index  1  should have a non-zero width.");
		Assert.IsGreaterThan(0, headerPresenter2.ActualHeight, "TabViewItem header for index  1  should have a non-zero height.");
	}
#endif

	[TestMethod]
	public async Task When_SelectedItem_Changed_Binding()
	{
		var vm = new ViewModel();
		var SUT = new TabView();

		SUT.SetBinding(TabView.TabItemsSourceProperty, new Microsoft.UI.Xaml.Data.Binding() { Path = new("TabItems") });
		SUT.SetBinding(TabView.SelectedItemProperty, new Microsoft.UI.Xaml.Data.Binding() { Path = new("SelectedItem"), Mode = Microsoft.UI.Xaml.Data.BindingMode.TwoWay });

		SUT.DataContext = vm;

		vm.SelectedItem = vm.TabItems[1];

		await UITestHelper.Load(SUT);

		Assert.AreEqual(1, SUT.SelectedIndex);
	}

	[TestMethod]
	public async Task When_AddingTab_While_Binding()
	{
		var SUT = new TabView();

		SUT.SetBinding(TabView.TabItemsSourceProperty, new Microsoft.UI.Xaml.Data.Binding() { Path = new("TabItems") });
		SUT.SetBinding(TabView.SelectedItemProperty, new Microsoft.UI.Xaml.Data.Binding() { Path = new("SelectedItem"), Mode = Microsoft.UI.Xaml.Data.BindingMode.TwoWay });

		SUT.DataContext = new ViewModel(addTab: true);

		await UITestHelper.Load(SUT);

		// It should select the newly added tab
		Assert.AreEqual(2, SUT.SelectedIndex);

		var tabviewItem = SUT.ContainerFromItem(SUT.SelectedItem) as TabViewItem;
		Assert.IsGreaterThan(0, tabviewItem.ActualWidth, "TabViewItem should have a non-zero width.");
		Assert.IsGreaterThan(0, tabviewItem.ActualHeight, "TabViewItem should have a non-zero height.");
	}
}

public class ViewModel : ViewModelBase
{
	public ViewModel(bool addTab = false)
	{
		if (addTab)
		{
			TabItems.Add(new TabViewItem { Header = "Main Tab", Content = "Main Content" });
			SelectedItem = TabItems[^1];
		}
	}
	private TabViewItem _selectedItem;

	public TabViewItem SelectedItem
	{
		get => _selectedItem;
		set => SetAndRaiseIfChanged(ref _selectedItem, value);
	}

	public ObservableCollection<TabViewItem> TabItems { get; } = new ObservableCollection<TabViewItem>
	{
		new TabViewItem { Header = "Tab 1", Content = "Content 1" },
		new TabViewItem { Header = "Tab 2", Content = "Content 2" }
	};
}
