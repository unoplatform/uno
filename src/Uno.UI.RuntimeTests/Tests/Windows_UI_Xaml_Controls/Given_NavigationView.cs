using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.ListViewPages;
#if NETFX_CORE
using Uno.UI.Extensions;
#elif __IOS__
using UIKit;
#elif __MACOS__
using AppKit;
#else
using Uno.UI;
#endif

#if HAS_UNO_WINUI
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using NavigationView = Windows.UI.Xaml.Controls.NavigationView;
using NavigationViewItem = Windows.UI.Xaml.Controls.NavigationViewItem;
using NavigationViewList = Windows.UI.Xaml.Controls.NavigationViewList;
#else
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
#endif

using static Private.Infrastructure.TestServices;
using Uno.Extensions;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public partial class Given_NavigationView
	{
		[TestMethod]
		[RunsOnUIThread]
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
#if __ANDROID__ || __IOS__ // ItemsStackPanel is just a Xaml facade on Android/iOS, its Children list isn't populated
				list.GetItemsPanelChildren();
#else
				panel.Children;
#endif
			Assert.AreEqual(item2, children.Last());
		}

	}
	public partial class MyNavigationView : NavigationView
	{
		public NavigationViewList MenuItemsHost { get; private set; }
		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			MenuItemsHost = GetTemplateChild("MenuItemsHost") as NavigationViewList;
		}
	}
}
