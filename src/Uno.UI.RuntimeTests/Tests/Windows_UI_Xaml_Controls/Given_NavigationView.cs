using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Input.Preview.Injection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.ListViewPages;
using Uno.UI.Toolkit.DevTools.Input;
#if __APPLE_UIKIT__
using UIKit;
#else
using Uno.UI;
#endif

#if HAS_UNO_WINUI || WINAPPSDK
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
#else
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
#endif

using static Private.Infrastructure.TestServices;
using Uno.Extensions;
using Uno.UI.RuntimeTests.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.Tests.Uno_UI_Xaml_Core;
using Uno.UI.Toolkit.Extensions;
using Microsoft.UI.Xaml.Media;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public partial class Given_NavigationView
	{
#if HAS_UNO && !HAS_UNO_WINUI
		[TestMethod]
		public async Task When_NavView()
		{
			var SUT = new MyNavigationView() { IsSettingsVisible = false };
			SUT.MenuItems.Add(new NavigationViewItem { DataContext = this, Content = "Item 1" });

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForLoaded(SUT);

			var list = SUT.MenuItemsHost;
			var panel = list.ItemsPanelRoot;

			Assert.IsNotNull(panel);

			NavigationViewItem item2 = null;
			await SUT.Dispatcher.RunIdleAsync(a => SUT.MenuItems.Add(new NavigationViewItem() { DataContext = this, Content = "Item 2" }));
			await SUT.Dispatcher.RunIdleAsync(a => SUT.MenuItems.RemoveAt(1));
			await SUT.Dispatcher.RunIdleAsync(a => SUT.MenuItems.Add(item2 = new NavigationViewItem() { DataContext = this, Content = "Item 2" }));

			await WindowHelper.WaitForLoaded(item2);

			var children =
#if __ANDROID__ || __APPLE_UIKIT__ // ItemsStackPanel is just a Xaml facade on Android/iOS, its Children list isn't populated
				list.GetItemsPanelChildren();
#else
				panel.Children;
#endif
			Assert.AreEqual(item2, children.Last());
		}
#endif

		[TestMethod]
		[RequiresFullWindow]
		[Ignore("Failing on CI due to animations")]
		public async Task MUX_When_MinimalHierarchicalAndSelectItem_Then_RemoveOverState()
		{
			var items = Enumerable
				.Range(0, 10)
				.Select(g =>
				{
					var item = new Microsoft.UI.Xaml.Controls.NavigationViewItem { Content = $"Group {g}" };
					item.MenuItems.AddRange(Enumerable
						.Range(0, 30)
						.Select(i => new Microsoft.UI.Xaml.Controls.NavigationViewItem { Content = $"Group {g} - Item {i}" } as object));

					return item;
				})
				.ToList();

			var SUT = new Microsoft.UI.Xaml.Controls.NavigationView
			{
				PaneDisplayMode = Microsoft.UI.Xaml.Controls.NavigationViewPaneDisplayMode.LeftMinimal,
				IsPaneToggleButtonVisible = true,
				IsBackButtonVisible = Microsoft.UI.Xaml.Controls.NavigationViewBackButtonVisible.Collapsed,
				IsPaneOpen = false
			};
			SUT.MenuItems.AddRange(items);

			await UITestHelper.Load(SUT);

			using var finger = InputInjector.TryCreate()?.GetFinger() ?? throw new InvalidOperationException("Failed to create finger");

			// This might not fail for each item, try to repro on mutliple items
			var item9 = await Select(0, 9);
			var item7 = await Select(0, 7);
			var item5 = await Select(0, 5);

			// Open the pane and expend the group 0 for screenshot
			await Expend(0);

			var screenShot = await UITestHelper.ScreenShot(SUT);

			ImageAssert.HasColorAt(screenShot, item9.GetLocation().OffsetLinear(5), "#E6E6E6");
			ImageAssert.HasColorAt(screenShot, item7.GetLocation().OffsetLinear(5), "#E6E6E6");
			ImageAssert.HasColorAt(screenShot, item5.GetLocation().OffsetLinear(5), "#E6E6E6");

			async Task OpenPane()
			{
				SUT.IsPaneOpen = true;
				await WindowHelper.WaitForIdle();
			}

			async Task Expend(int group)
			{
				await OpenPane();

				items[group].IsExpanded = true;
				await WindowHelper.WaitForIdle();
			}


			async Task<Rect> Select(int group, int item)
			{
				await Expend(group);

				var itemBounds = ((Microsoft.UI.Xaml.Controls.NavigationViewItem)items[group].MenuItems[item]).GetAbsoluteBounds();
				finger.Press(itemBounds.GetCenter());
				await WindowHelper.WaitForIdle();
				await Task.Delay(250 + 100); // Close animation

				return itemBounds;
			}
		}

#if HAS_UNO_WINUI || WINAPPSDK
		[TestMethod]
		public async Task When_NavigationViewItem_MenuSource_VectorChanged()
		{
			var nvi1 = new NavigationViewItem
			{
				Content = "Level 1",
				Icon = new SymbolIcon(Symbol.Home)
			};
			var source = new ObservableCollection<NavigationViewItem>
			{
				nvi1
			};

			var nv = new NavigationView
			{
				PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
				MenuItemsSource = source
			};

			await UITestHelper.Load(nv);

			var nvi2 = new NavigationViewItem { Content = "Level 1 item 1", Name = "RuntimeTestNVI" };
			source[0].MenuItems.Add(nvi2);
			await WindowHelper.WaitForIdle();

			nvi1.IsExpanded = true;
			await WindowHelper.WaitForIdle();

#if __ANDROID__ || __APPLE_UIKIT__
			var descendant = nv.EnumerateDescendants().SingleOrDefault(d => d is NavigationViewItem { Name: "RuntimeTestNVI" });
			Assert.AreEqual(nvi2, descendant);
#else
			Assert.AreEqual(nvi2, nv.FindVisualChildByName("RuntimeTestNVI"));
#endif
		}
#endif

		[TestMethod]
		[RequiresFullWindow]
		public async Task When_NavigationView_MenuItems_Clear()
		{
			void AddItems(NavigationView nv, int count)
			{
				for (var i = 0; i < count; i++)
				{
					nv.MenuItems.Add(new NavigationViewItem { Content = $"Item {i}" });
				}
			}

			var nv = new NavigationView
			{
				PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact,
				IsSettingsVisible = false
			};

			await UITestHelper.Load(nv);
			await WindowHelper.WaitForIdle();

			for (int i = 0; i < 5; i++)
			{
				AddItems(nv, 10);
				Assert.AreEqual(10, nv.MenuItems.Count, "Initial count of MenuItems should be 10.");
				nv.SelectedItem = nv.MenuItems[0];
				await WindowHelper.WaitForIdle();
				Action act = () => nv.MenuItems.Clear();
				act.Should().NotThrow("Clearing MenuItems should not throw an exception.");
				Assert.IsEmpty(nv.MenuItems, "MenuItems should be empty after clearing.");
			}
		}
	}

#if HAS_UNO && !HAS_UNO_WINUI
	public partial class MyNavigationView : NavigationView
	{
		public NavigationViewList MenuItemsHost { get; private set; }
		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			MenuItemsHost = GetTemplateChild("MenuItemsHost") as NavigationViewList;
		}
	}
#endif
}
