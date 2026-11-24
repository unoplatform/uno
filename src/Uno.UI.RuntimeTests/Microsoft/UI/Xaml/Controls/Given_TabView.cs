using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

using Windows.UI.Input.Preview.Injection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;

using Private.Infrastructure;
using Uno.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.DevTools.Input;

using TabView = Microsoft.UI.Xaml.Controls.TabView;
using TabViewItem = Microsoft.UI.Xaml.Controls.TabViewItem;

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

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24560")]
	public async Task When_Arrow_Keys_On_Tabs_Then_Focus_Follows_Tab_Order()
	{
		var firstItem = new TabViewItem { Header = "Tab 1" };
		var secondItem = new TabViewItem { Header = "Tab 2" };
		var SUT = new TabView
		{
			TabItems = { firstItem, secondItem }
		};

		try
		{
			await UITestHelper.Load(SUT);

			var firstCloseButton = (Button)firstItem.FindName("CloseButton");
			Assert.IsNotNull(firstCloseButton);

			firstItem.Focus(FocusState.Keyboard);
			await WindowHelper.WaitForIdle();
			Assert.AreSame(firstItem, FocusManager.GetFocusedElement(WindowHelper.XamlRoot));

			// Focus order is Tab 1 -> Tab 1 close button -> Tab 2 -> ...
			await KeyboardHelper.Right();
			await WindowHelper.WaitForIdle();
			Assert.AreSame(firstCloseButton, FocusManager.GetFocusedElement(WindowHelper.XamlRoot));

			await KeyboardHelper.Right();
			await WindowHelper.WaitForIdle();
			Assert.AreSame(secondItem, FocusManager.GetFocusedElement(WindowHelper.XamlRoot));

			await KeyboardHelper.Left();
			await WindowHelper.WaitForIdle();
			Assert.AreSame(firstCloseButton, FocusManager.GetFocusedElement(WindowHelper.XamlRoot));
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
#if !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is not supported on this platform.")]
#endif
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_Tab_Dragged_Then_Item_Tracks_Drag(bool reapplyTemplate)
	{
		var firstItem = new TabViewItem { Header = "Tab 1" };
		var thirdItem = new TabViewItem { Header = "Tab 3" };
		var SUT = new TabView
		{
			CanDragTabs = true,
			CanReorderTabs = true,
			TabItems = { firstItem, new TabViewItem { Header = "Tab 2" }, thirdItem }
		};

		try
		{
			await UITestHelper.Load(SUT);

			if (reapplyTemplate)
			{
				var template = firstItem.Template;
				var oldCloseButton = firstItem.GetTemplateChild("CloseButton");

				firstItem.Template = null;
				await WindowHelper.WaitForIdle();
				firstItem.Template = template;
				firstItem.ApplyTemplate();
				await WindowHelper.WaitForIdle();

				var newCloseButton = firstItem.GetTemplateChild("CloseButton");
				Assert.IsNotNull(newCloseButton, "The template was not re-applied.");
				Assert.AreNotSame(oldCloseButton, newCloseButton, "The template was not re-applied.");
			}

			var dragStarted = false;
			var dragCompleted = false;
			SUT.TabDragStarting += (_, _) => dragStarted = true;
			SUT.TabDragCompleted += (_, _) => dragCompleted = true;

			var start = firstItem.GetAbsoluteBounds().GetCenter();
			var end = thirdItem.GetAbsoluteBounds().GetCenter();

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			mouse.MoveTo(start);
			await WindowHelper.WaitForIdle();
			mouse.Press();
			await WindowHelper.WaitForIdle();

			const int steps = 12;
			for (var i = 1; i <= steps; i++)
			{
				var t = (double)i / steps;
				mouse.MoveTo(new global::Windows.Foundation.Point(start.X + (end.X - start.X) * t, start.Y + (end.Y - start.Y) * t));
				await WindowHelper.WaitForIdle();
			}

			Assert.IsTrue(dragStarted, "The tab drag never started.");
			Assert.IsTrue(firstItem.IsBeingDragged, "The dragged TabViewItem did not receive TabDragStarting.");

			mouse.Release();
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(dragCompleted, "The tab drag never completed.");
			Assert.IsFalse(firstItem.IsBeingDragged, "The dragged TabViewItem did not receive TabDragCompleted.");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}
#endif

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
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
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
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
	public async Task When_TabViewItem_Removed_Then_Collected()
	{
		var SUT = new TabView();
		SUT.TabItems.Add(new TabViewItem { Header = "Tab 2" });

		try
		{
			await UITestHelper.Load(SUT);

			var itemRef = await AddAndRemoveTab(SUT);
			await LeakTestHelper.CollectAsync([itemRef]);

			Assert.IsFalse(itemRef.IsAlive, "The removed TabViewItem is still alive.");
			Assert.HasCount(1, SUT.TabItems);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_TabItemsSource_Item_Removed_Then_Collected()
	{
		var source = new ObservableCollection<TabLeakData> { new() { Header = "Tab 2" } };
		var SUT = new TabView { TabItemsSource = source };

		try
		{
			await UITestHelper.Load(SUT);

			var dataRef = await AddAndRemoveSourceItem(SUT, source);
			await LeakTestHelper.CollectAsync([dataRef]);

			Assert.IsFalse(dataRef.IsAlive, "The removed data item is still alive.");
			Assert.HasCount(1, source);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
	private static async Task<WeakReference> AddAndRemoveSourceItem(TabView tabView, ObservableCollection<TabLeakData> source)
	{
		var data = new TabLeakData { Header = "Tab 1" };
		source.Insert(0, data);
		await WindowHelper.WaitFor(() => tabView.ContainerFromItem(data) is not null, timeoutMS: 5000, message: "No container was generated for the data item.");

		var container = (TabViewItem)tabView.ContainerFromItem(data);
		await UITestHelper.WaitForLoaded(container);
		container = null;

		source.Remove(data);
		await WindowHelper.WaitForIdle();

		return new WeakReference(data);
	}

	private sealed class TabLeakData
	{
		public string Header { get; set; }

		public override string ToString() => Header;
	}

	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
	private static async Task<WeakReference> AddAndRemoveTab(TabView tabView)
	{
		var item = new TabViewItem { Header = "Tab 1", Content = "Content 1" };
		tabView.TabItems.Insert(0, item);
		await UITestHelper.WaitForLoaded(item);

		tabView.TabItems.Remove(item);
		await WindowHelper.WaitForIdle();

		return new WeakReference(item);
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_TabViewItem_With_Bound_Content_Removed_Then_Collected(bool isSelected)
	{
		var viewModel = new TabLeakViewModel();
		var SUT = new TabView { DataContext = viewModel };
		SUT.TabItems.Add(new TabViewItem { Header = "Tab 2" });

		try
		{
			await UITestHelper.Load(SUT);

			var refs = await AddAndRemoveRichTab(SUT, isSelected);
			await LeakTestHelper.CollectAsync(refs);

			Assert.IsFalse(refs[0].IsAlive, "The removed TabViewItem is still alive.");
			Assert.IsFalse(refs[1].IsAlive, "The removed TabViewItem's Header is still alive.");
			Assert.IsFalse(refs[2].IsAlive, "The removed TabViewItem's Content is still alive.");
			Assert.HasCount(1, SUT.TabItems);
			GC.KeepAlive(viewModel);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
	private static async Task<List<WeakReference>> AddAndRemoveRichTab(TabView tabView, bool isSelected)
	{
		var header = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Children =
			{
				new FontIcon { Glyph = "\uE8A5" },
				new TextBlock(),
			}
		};
		((TextBlock)header.Children[1]).SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath(nameof(TabLeakViewModel.Title)) });

		var textBox = new TextBox();
		textBox.SetBinding(TextBox.TextProperty, new Binding { Path = new PropertyPath(nameof(TabLeakViewModel.Text)), Mode = BindingMode.TwoWay });
		var content = new StackPanel
		{
			Children =
			{
				textBox,
				new Button { Content = "Button" },
				new ListView { ItemsSource = new[] { "A", "B", "C" } },
			}
		};

		var item = new TabViewItem
		{
			Header = header,
			IconSource = new SymbolIconSource { Symbol = Symbol.Document },
			Content = content,
		};
		tabView.TabItems.Insert(0, item);
		await UITestHelper.WaitForLoaded(item);

		if (isSelected)
		{
			tabView.SelectedItem = item;
			await UITestHelper.WaitForLoaded(content);
		}

		tabView.TabItems.Remove(item);
		await WindowHelper.WaitForIdle();

		return [new WeakReference(item), new WeakReference(header), new WeakReference(content)];
	}

	private class TabLeakViewModel : ViewModelBase
	{
		private string _title = "Bound title";
		private string _text = "Bound text";

		public string Title
		{
			get => _title;
			set => SetAndRaiseIfChanged(ref _title, value);
		}

		public string Text
		{
			get => _text;
			set => SetAndRaiseIfChanged(ref _text, value);
		}
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
		((ContentPresenter)closeButton1.GetTemplateChild("ContentPresenter")).FindFirstChild<ImplicitTextBlock>();

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
