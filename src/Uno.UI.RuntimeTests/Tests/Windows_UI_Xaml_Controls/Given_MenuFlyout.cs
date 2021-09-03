using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.Extensions;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.MUX.Helpers;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.MenuFlyoutPages;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_MenuFlyout
	{
		[TestMethod]
		[RequiresFullWindow]
		public async Task When_Native_AppBarButton_And_Managed_Popups()
		{
			using (StyleHelper.UseNativeFrameNavigation())
			{
				var page = new Native_AppBarButton_Page();

				WindowHelper.WindowContent = page;
				await WindowHelper.WaitForLoaded(page);

				var flyout = page.SUT.Flyout as MenuFlyout;
				try
				{
					await ControlHelper.DoClickUsingAP(page.SUT);
#if !NETFX_CORE
					Assert.AreEqual(false, flyout.UseNativePopup);
#endif
					var flyoutItem = page.FirstFlyoutItem;

					await WindowHelper.WaitForLoaded(flyoutItem);
					var pageBounds = page.GetOnScreenBounds();
					var flyoutItemBounds = flyoutItem.GetOnScreenBounds();
					Assert.AreEqual(pageBounds.Right, flyoutItemBounds.Right, delta: 1);
					NumberAssert.Less(flyoutItemBounds.Top, pageBounds.Height / 4); // Exact command bar height may vary between platforms, but the flyout should at least be in the ~top 1/4th of screen
				}
				finally
				{
					flyout.Hide();
				}
			}
		}

		[TestMethod]
		[RequiresFullWindow]
		public async Task Verify_MenuBarItem_Bounds()
		{
			using (StyleHelper.UseFluentStyles())
			{
				var flyoutItem = new MenuFlyoutItem { Text = "Open..." };
				var menuBarItem = new MenuBarItem
				{
					Title = "File",
					Items =
				{
					flyoutItem,
					new MenuFlyoutItem { Text = "Don't open..."}
				}
				};

				var menuBar = new MenuBar
				{
					Items =
				{
					menuBarItem
				}
				};

				var contentSpacer = new Border { Background = new SolidColorBrush(Colors.Tomato), Margin = new Thickness(20) };
				Grid.SetRow(contentSpacer, 1);

				var hostPanel = new Grid
				{
					Children =
				{
					menuBar,
					contentSpacer
				},
					RowDefinitions =
				{
					new RowDefinition {Height = GridLength.Auto},
					new RowDefinition {Height = new GridLength(1, GridUnitType.Star)}
				}
				};

				WindowHelper.WindowContent = hostPanel;
				await WindowHelper.WaitForLoaded(hostPanel);

				var peer = new MenuBarItemAutomationPeer(menuBarItem);
				try
				{
					peer.Invoke();

					await WindowHelper.WaitForLoaded(flyoutItem);

					var menuBarItemBounds = menuBarItem.GetOnScreenBounds();

					var flyoutItemBounds = flyoutItem.GetOnScreenBounds();

					Assert.AreEqual(40, menuBarItemBounds.Height, 1);

					Assert.AreEqual(1, flyoutItemBounds.X, 2);
					Assert.AreEqual(43, flyoutItemBounds.Y, 2);
				}
				finally
				{
					peer.Collapse();
				} 
			}
		}
	}
}
