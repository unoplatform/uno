using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
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
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using static Private.Infrastructure.TestServices;
#if NETFX_CORE
// Use the MUX MenuBar on Window for consistency, since Uno is using the MUX styles. (However Uno.UI only defines WUXC.MenuBar, not MUXC.MenuBar)
using MenuBar = Microsoft.UI.Xaml.Controls.MenuBar;
using MenuBarItem = Microsoft.UI.Xaml.Controls.MenuBarItem;
using MenuBarItemAutomationPeer = Microsoft.UI.Xaml.Automation.Peers.MenuBarItemAutomationPeer;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
#if __MACOS__
	[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
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
		public async Task When_Add_MenuFlyoutSeparator_To_MenuBarItem()
		{
			using (StyleHelper.UseFluentStyles())
			{
				var menuBarItem = new MenuBarItem
				{
					Title = "File",
				};

				var flyoutItem1 = new MenuFlyoutItem { Text = "Open..." };
				var flyoutItem2 = new MenuFlyoutItem { Text = "Save..." };
				var flyoutSeparator = new MenuFlyoutSeparator();

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

				menuBarItem.Items.Add(flyoutItem1);
				menuBarItem.Items.Add(flyoutSeparator);
				menuBarItem.Items.Add(flyoutItem2);

				var peer = new MenuBarItemAutomationPeer(menuBarItem);
				try
				{
					peer.Invoke();

					await WindowHelper.WaitForLoaded(flyoutItem1);
					await WindowHelper.WaitForLoaded(flyoutSeparator);
					await WindowHelper.WaitForLoaded(flyoutItem2);

					var flyoutItem1Bounds = flyoutItem1.GetRelativeBounds(menuBarItem);
					var flyoutSeparatorBounds = flyoutSeparator.GetRelativeBounds(menuBarItem);
					var flyoutItem2Bounds = flyoutItem2.GetRelativeBounds(menuBarItem);

					Assert.IsTrue(flyoutItem1Bounds.Height > 0);
					Assert.IsTrue(flyoutSeparatorBounds.Height > 0);
					Assert.IsTrue(flyoutItem2Bounds.Height > 0);

					Assert.IsTrue(flyoutItem1Bounds.Y < flyoutSeparatorBounds.Y);
					Assert.IsTrue(flyoutSeparatorBounds.Y < flyoutItem2Bounds.Y);
				}
				finally
				{
					peer.Collapse();
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

					var menuBarBounds = menuBar.GetOnScreenBounds();

					Assert.AreEqual(32, menuBarItemBounds.Height, 1);

					var expectedY = 39.0;
#if __ANDROID__
					if (!FeatureConfiguration.Popup.UseNativePopup)
					{
						// If using managed popup, the expected offset must be adjusted for the status bar
						expectedY += menuBarBounds.Y;
					}
#endif

					Assert.AreEqual(5, flyoutItemBounds.X, 3);
					Assert.AreEqual(expectedY, flyoutItemBounds.Y, 3);
				}
				finally
				{
					peer.Collapse();
				}
			}
		}

#if __ANDROID__
		[TestMethod]
		[RequiresFullWindow]
		public async Task Verify_MenuBarItem_Bounds_Native_Popups()
		{
			using (FeatureConfigurationHelper.UseNativePopups())
			{
				await Verify_MenuBarItem_Bounds();
			}
		}

		[TestMethod]
		[RequiresFullWindow]
		public async Task Verify_MenuBarItem_Bounds_Managed_Popups()
		{
			await Verify_MenuBarItem_Bounds();
		}
#endif

#if HAS_UNO
		[TestMethod]
		public async Task When_MenuFlyoutItem_CommandChanging()
		{
			var SUT = new MenuBar();
			var item = new MenuBarItem() { Title = "test item" };
			Common.DelegateCommand command1 = null;
			command1 = new(() => command1.CanExecuteEnabled = !command1.CanExecuteEnabled);
			var flyoutItem = new MenuFlyoutItem
			{
				Command = command1,
				Text = "test flyout"
			};

			SUT.Items.Add(item);
			item.Items.Add(flyoutItem);

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			item.Invoke();
			Assert.IsTrue(flyoutItem.IsEnabled);

			await WindowHelper.WaitForLoaded(flyoutItem);

			flyoutItem.InvokeClick();

			// Force close the flyout as InvokeClick does not do so.
			item.CloseMenuFlyout();

			await WindowHelper.WaitForIdle();

			item.Invoke();

			Assert.IsFalse(flyoutItem.IsEnabled);

			flyoutItem.InvokeClick();
			item.CloseMenuFlyout();
		}

		[TestMethod]
		public async Task When_MenuFlyout_Added_In_Opening()
		{
			MenuFlyout flyout = null;
			try
			{
				var button = new Button() { Content = "Test" };
				WindowHelper.WindowContent = button;
				await WindowHelper.WaitForLoaded(button);
				var menuItem = new MenuFlyoutItem
				{
					Icon = new SymbolIcon(Symbol.Home),
					Text = "This menu item should have a home icon",
				};

				flyout = new MenuFlyout
				{
					Placement = FlyoutPlacementMode.Bottom,
				};

				flyout.Opening += (s2, e2) =>
				{
					(s2 as MenuFlyout).Items.Add(menuItem);
				};

				flyout.ShowAt(button);

				await WindowHelper.WaitForLoaded(menuItem);

				var presenter = flyout.GetPresenter() as MenuFlyoutPresenter;
				Assert.IsNotNull(presenter);
				Assert.IsTrue(presenter.GetContainsIconItems());
			}
			finally
			{
				if (flyout?.IsOpen == true)
				{
					flyout.Hide();
				}
			}
		}
#endif
	}
}
