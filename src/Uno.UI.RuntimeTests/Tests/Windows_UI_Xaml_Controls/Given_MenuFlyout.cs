using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using static Private.Infrastructure.TestServices;

#if HAS_UNO && !HAS_UNO_WINUI
using Microsoft.UI.Xaml.Controls;
#endif

using MenuBar = Microsoft.UI.Xaml.Controls.MenuBar;
using MenuBarItem = Microsoft.UI.Xaml.Controls.MenuBarItem;
using MenuBarItemAutomationPeer = Microsoft.UI.Xaml.Automation.Peers.MenuBarItemAutomationPeer;
using RuntimeTests.Windows_UI_Xaml_Controls.Flyout;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_MenuFlyout
	{
		[TestMethod]
		[RequiresFullWindow]
		public async Task When_Toggle_IsEnabled_Via_Binding()
		{
			var page = new Flyout_ToggleMenu_IsEnabled();

			WindowHelper.WindowContent = page;
			await WindowHelper.WaitForLoaded(page);

			var content = page.Content as StackPanel;
			var button = content.Children.First() as Button;
			var buttonPeer = FrameworkElementAutomationPeer.CreatePeerForElement(button) as ButtonAutomationPeer;

			var flyout = button.Flyout as MenuFlyout;

			async Task AssertIsEnabled(bool expected)
			{
				buttonPeer.Invoke();
				await TestServices.WindowHelper.WaitFor(() => VisualTreeHelper.GetOpenPopupsForXamlRoot(TestServices.WindowHelper.XamlRoot).Count > 0);

				var popup = VisualTreeHelper.GetOpenPopupsForXamlRoot(TestServices.WindowHelper.XamlRoot).First();
				foreach (var item in flyout.Items)
				{
					// The toggleItem should be disabled by default
					Assert.AreEqual(expected, item.IsEnabled);
				}

				popup.IsOpen = false;

				await WindowHelper.WaitForIdle();
			}

			await AssertIsEnabled(false);

			// Enable the toggleItem
			page.ViewModel.AreItemsEnabled = true;

			await AssertIsEnabled(true);

			// Disable the toggleItem
			page.ViewModel.AreItemsEnabled = false;

			await AssertIsEnabled(false);
		}

		[TestMethod]
		[RequiresFullWindow]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
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
#if !WINAPPSDK
					Assert.IsFalse(flyout.UseNativePopup);
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

				Assert.IsGreaterThan(0, flyoutItem1Bounds.Height);
				Assert.IsGreaterThan(0, flyoutSeparatorBounds.Height);
				Assert.IsGreaterThan(0, flyoutItem2Bounds.Height);

				Assert.IsLessThan(flyoutSeparatorBounds.Y, flyoutItem1Bounds.Y);
				Assert.IsLessThan(flyoutItem2Bounds.Y, flyoutSeparatorBounds.Y);
			}
			finally
			{
				peer.Collapse();
			}
		}

		[TestMethod]
		[RequiresFullWindow]
		public async Task Verify_MenuBarItem_Bounds()
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

#if __ANDROID__
		[TestMethod]
		[RequiresFullWindow]
		[Ignore("Flaky #9080")]
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

			flyoutItem.Invoke();

			// Force close the flyout as InvokeClick does not do so.
			item.CloseMenuFlyout();

			await WindowHelper.WaitForIdle();

			item.Invoke();

			Assert.IsFalse(flyoutItem.IsEnabled);

			flyoutItem.Invoke();
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

		[TestMethod]
		[RequiresFullWindow]
#if __APPLE_UIKIT__
		[Ignore("https://github.com/unoplatform/uno/issues/13314")]
#endif
		public async Task When_MenuFlyoutSubItem_Should_Have_Correct_Placement()
		{
			if (WindowHelper.IsXamlIsland)
			{
				return;
			}

			var button = new Button()
			{
				HorizontalAlignment = HorizontalAlignment.Right,
				Content = "Open flyout",
				Flyout = new MenuFlyout()
				{
					Items =
					{
						new MenuFlyoutSubItem()
						{
							Text = "Open submenu",
							Items =
							{
								new MenuFlyoutSubItem() { Text = "First item" },
							},
						},
					}
				}
			};

			WindowHelper.WindowContent = button;
			await WindowHelper.WaitForLoaded(button);
			button.AutomationPeerClick();

			var flyout = (MenuFlyout)button.Flyout;
			var subItem = (MenuFlyoutSubItem)flyout.Items.Single();
			await WindowHelper.WaitForLoaded(subItem);
			try
			{
				subItem.Open();

				// The "Open submenu" opens at the very right of the screen.
				// So, the "First item" sub item should open on its left.
				// We assert that the left of "Open sub menu" is almost the same as the right of "First item"

				await WindowHelper.WaitForIdle();
				var subItemBounds = subItem.GetAbsoluteBounds();
				var subSubItemBounds = ((MenuFlyoutSubItem)subItem.Items.Single()).GetAbsoluteBounds();

				var difference = subItemBounds.X - subSubItemBounds.Right;
				Assert.IsLessThanOrEqualTo(5d, Math.Abs(difference));

			}
			finally
			{
				subItem.Close();
				flyout.Close();
			}
		}
#endif

		[TestMethod]
		public async Task When_MenuFlyout_DataContext_Changes_In_Opening()
		{
			var SUT = new When_MenuFlyout_DataContext_Changes_In_Opening();


			MenuFlyout flyout = null;
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);


				var button = SUT.FindFirstChild<Button>();
				flyout = (MenuFlyout)button.Flyout;

				flyout.ShowAt(button);
				await WindowHelper.WaitForIdle();

				Assert.AreEqual("1", (flyout.Items[0] as MenuFlyoutItem)!.Text);
			}
			finally
			{
				if (flyout?.IsOpen == true)
				{
					flyout.Hide();
				}
			}
		}

#if HAS_UNO
		[TestMethod]
		public async Task When_Toggle_Item_HasToggle()
		{
			var toggleItem = new ToggleMenuFlyoutItem();
			Assert.IsTrue(toggleItem.HasToggle());
		}

		[TestMethod]
		public async Task When_Menu_Contains_Toggle()
		{
			var menu = new MenuFlyout();
			menu.Items.Add(new MenuFlyoutItem() { Text = "Text" });

			var trigger = new Button();
			TestServices.WindowHelper.WindowContent = trigger;
			await TestServices.WindowHelper.WaitForLoaded(trigger);

			await ValidateToggleAsync(false);

			var toggleItem = new ToggleMenuFlyoutItem() { Text = "Toggle!" };
			menu.Items.Add(toggleItem);
			await ValidateToggleAsync(true);

			menu.Items.Remove(toggleItem);
			await ValidateToggleAsync(false);

			async Task ValidateToggleAsync(bool expected)
			{
				menu.ShowAt(trigger);
				await TestServices.WindowHelper.WaitForIdle();
				var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(TestServices.WindowHelper.XamlRoot);
				var popup = popups[0];
				Assert.IsInstanceOfType(popup.Child, typeof(MenuFlyoutPresenter));
				var presenter = (MenuFlyoutPresenter)popup.Child;
				Assert.AreEqual(expected, presenter.GetContainsToggleItems());
				popup.IsOpen = false;
				await TestServices.WindowHelper.WaitForIdle();
			}
		}
#endif

		// ---------------------------------------------------------------------------
		// {ThemeResource} inheritance for popup-hosted (Flyout / MenuFlyout) content — kahua #480.
		//
		// Verifies that a {ThemeResource} used inside Flyout / MenuFlyout (popup-hosted) content
		// resolves against the content's own inherited ActualTheme — not the ambient/application theme.
		//
		// A menu's commands reference a {ThemeResource} whose theme-keyed brush is declared in the
		// surrounding view's themed ResourceDictionary (Light/Dark sub-dicts). When the menu is opened
		// from a Light subtree under a Dark application, the menu content's ActualTheme is correctly
		// Light, but the {ThemeResource} can wrongly resolve the Dark sub-dictionary value — a mixed
		// result (correct inherited theme, wrong resolved value) that renders the menu with the wrong
		// styling on open.
		//
		// The OS-vs-app mismatch is reproduced on Uno by pinning the application theme to Dark
		// (ThemeHelper.UseApplicationDarkTheme, #if HAS_UNO — it relies on the Uno-internal
		// SetExplicitRequestedTheme) and placing a host that pins RequestedTheme=Light. On native WinUI
		// the app-theme pin is unavailable, so the test runs as a Light-host baseline confirming the
		// WinUI-correct value (Green); WinUI never exhibits the regression, so it still validates the
		// behavior the Uno fix must match. The host's Resources declare the theme-keyed
		// sentinel brush (Light=Green, Dark=Red). The flyout/menu content references that brush via
		// {ThemeResource}, declared inline in the SAME XAML so it parses inside the host's resource scope
		// (a standalone XamlReader.Load of a {ThemeResource} fragment throws on WinUI). The flyout content
		// is hosted in the PopupRoot, reparented out of the owner's visual scope.
		//
		// WinUI-correct behavior: the menu content's {ThemeResource} resolves against the owner's
		// inherited theme (Light), so the brush evaluates to the Light sentinel (Green) — even though the
		// application theme is Dark. Uno regression: the popup-presented content resolves the
		// {ThemeResource} against the global/application active theme (Dark), evaluating to the Dark
		// sentinel (Red), despite the content's ActualTheme correctly being Light. The expected value
		// (Green) is identical on Skia Desktop and native WinUI; only the app-level mismatch differs
		// (forced Dark on Uno, default on WinUI, as noted above).
		// ---------------------------------------------------------------------------
		private static Color? GetThemeResourceForegroundColor(TextBlock textBlock)
			=> (textBlock.Foreground as SolidColorBrush)?.Color;

		// ------------------------------------------------------------------
		// Scenario A — Flyout. A RequestedTheme=Light host declares the theme-keyed
		// sentinel brush; a Flyout's TextBlock content references it via
		// {ThemeResource}. Under a Dark application, the popup-presented content's
		// {ThemeResource} must still resolve the owner's Light sentinel (Green).
		// ------------------------------------------------------------------
		[TestMethod]
		[RequiresFullWindow]
		[GitHubWorkItem("https://github.com/unoplatform/kahua-private/issues/480")]
		public async Task When_Flyout_Menu_Uses_Owner_Subtree_Theme_Light_Under_Dark_App()
		{
#if HAS_UNO
			using var darkApp = ThemeHelper.UseApplicationDarkTheme();
			await WindowHelper.WaitForIdle();
#endif

			// One XAML document: host pins Light + declares the theme-keyed brush; the Flyout content
			// (a TextBlock) references it via {ThemeResource}, parsed inside the host's resource scope.
			var host = (Border)XamlReader.Load(
				"""
				<Border xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
						xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
						RequestedTheme="Light">
					<Border.Resources>
						<ResourceDictionary>
							<ResourceDictionary.ThemeDictionaries>
								<ResourceDictionary x:Key="Light">
									<SolidColorBrush x:Key="MenuItemBrush" Color="Green" />
								</ResourceDictionary>
								<ResourceDictionary x:Key="Dark">
									<SolidColorBrush x:Key="MenuItemBrush" Color="Red" />
								</ResourceDictionary>
							</ResourceDictionary.ThemeDictionaries>
						</ResourceDictionary>
					</Border.Resources>
					<Button x:Name="Owner" Content="Owner">
						<Button.Flyout>
							<Flyout>
								<TextBlock x:Name="MenuLabel" Text="Menu"
										Foreground="{ThemeResource MenuItemBrush}" />
							</Flyout>
						</Button.Flyout>
					</Button>
				</Border>
				""");

			var owner = (Button)host.FindName("Owner");
			var flyout = (Flyout)owner.Flyout;
			var label = (TextBlock)flyout.Content;

			var root = new Border { Child = host };

			try
			{
				WindowHelper.WindowContent = root;
				await WindowHelper.WaitForLoaded(root);

				flyout.ShowAt(owner);
				await WindowHelper.WaitForIdle();
				await WindowHelper.WaitForLoaded(label);
				await WindowHelper.WaitForIdle();

				Assert.AreEqual(ElementTheme.Light, owner.ActualTheme, "Owner should be in the Light subtree.");
				Assert.AreEqual(ElementTheme.Light, label.ActualTheme, "Flyout content should inherit the owner's Light theme.");

				var foreground = GetThemeResourceForegroundColor(label);
				Assert.IsNotNull(foreground, "Flyout content should have resolved a SolidColorBrush foreground.");

				Assert.AreEqual(Colors.Green, foreground.Value,
					$"Flyout content {{ThemeResource}} should resolve against the content's inherited Light theme " +
					$"(Light sentinel Green), but got {foreground.Value}. If it is Red (the Dark sentinel), the " +
					$"popup-presented content resolved the {{ThemeResource}} against the application/global Dark " +
					$"theme instead of its own inherited ActualTheme.");
			}
			finally
			{
				flyout.Hide();
#if HAS_UNO
				VisualTreeHelper.CloseAllPopups(WindowHelper.XamlRoot);
#endif
			}
		}

		// ------------------------------------------------------------------
		// Scenario B — MenuFlyout (the menu/context-menu control family). Same
		// host/brush setup; a MenuFlyoutItem's foreground references the host brush
		// via {ThemeResource} and must resolve the owner's Light sentinel (Green).
		// ------------------------------------------------------------------
		[TestMethod]
		[RequiresFullWindow]
		[GitHubWorkItem("https://github.com/unoplatform/kahua-private/issues/480")]
		public async Task When_MenuFlyout_Item_Uses_Owner_Subtree_Theme_Light_Under_Dark_App()
		{
#if HAS_UNO
			using var darkApp = ThemeHelper.UseApplicationDarkTheme();
			await WindowHelper.WaitForIdle();
#endif

			var host = (Border)XamlReader.Load(
				"""
				<Border xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
						xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
						RequestedTheme="Light">
					<Border.Resources>
						<ResourceDictionary>
							<ResourceDictionary.ThemeDictionaries>
								<ResourceDictionary x:Key="Light">
									<SolidColorBrush x:Key="MenuItemBrush" Color="Green" />
								</ResourceDictionary>
								<ResourceDictionary x:Key="Dark">
									<SolidColorBrush x:Key="MenuItemBrush" Color="Red" />
								</ResourceDictionary>
							</ResourceDictionary.ThemeDictionaries>
						</ResourceDictionary>
					</Border.Resources>
					<Button x:Name="Owner" Content="Owner">
						<Button.Flyout>
							<MenuFlyout>
								<MenuFlyoutItem x:Name="MenuItem" Text="Item"
										Foreground="{ThemeResource MenuItemBrush}" />
							</MenuFlyout>
						</Button.Flyout>
					</Button>
				</Border>
				""");

			var owner = (Button)host.FindName("Owner");
			var flyout = (MenuFlyout)owner.Flyout;
			var item = (MenuFlyoutItem)host.FindName("MenuItem");

			var root = new Border { Child = host };

			try
			{
				WindowHelper.WindowContent = root;
				await WindowHelper.WaitForLoaded(root);

				flyout.ShowAt(owner);
				await WindowHelper.WaitForIdle();
				await WindowHelper.WaitForLoaded(item);
				await WindowHelper.WaitForIdle();

				Assert.AreEqual(ElementTheme.Light, owner.ActualTheme, "Owner should be in the Light subtree.");
				Assert.AreEqual(ElementTheme.Light, item.ActualTheme, "Menu item should inherit the owner's Light theme.");

				var foreground = (item.Foreground as SolidColorBrush)?.Color;
				Assert.IsNotNull(foreground, "Menu item should have resolved a SolidColorBrush foreground.");

				Assert.AreEqual(Colors.Green, foreground.Value,
					$"Menu item {{ThemeResource}} should resolve against the item's inherited Light theme " +
					$"(Light sentinel Green), but got {foreground.Value}. If it is Red (the Dark sentinel), the " +
					$"popup-presented menu resolved the {{ThemeResource}} against the application/global Dark " +
					$"theme instead of its own inherited ActualTheme.");
			}
			finally
			{
				flyout.Hide();
#if HAS_UNO
				VisualTreeHelper.CloseAllPopups(WindowHelper.XamlRoot);
#endif
			}
		}

		// ------------------------------------------------------------------
		// Scenario C — MenuFlyout first-open flash. Same Light-under-Dark-app
		// setup as Scenario B, but verifies the menu item's Foreground is
		// already correct at the moment the popup enters the visual tree —
		// before the first measure/render — so the user does not see one
		// frame of wrong-theme text on first open. The previous fix (Popup
		// falls back to anchor theme on first open) corrects the final state
		// but only via an async corrective theme walk that fires AFTER the
		// item has rendered once with the parse-time {ThemeResource} value;
		// against a Dark application theme that resolves to the Dark sentinel
		// (Red), producing the visible flash the user reported in #480.
		//
		// Note: the item's parse-time Foreground IS legitimately Red because
		// XAML parses {ThemeResource MenuItemBrush} before any subtree theme
		// context is available, so the lookup falls back to the application's
		// global active theme. That pre-tree state is not user-visible (the
		// item is not in any visual tree yet), so this test does not assert
		// on it; it only asserts that by the time the popup is in the tree
		// — the synchronous return point of ShowAt — the Foreground already
		// holds the Light sentinel (Green), and stays Green afterwards.
		// ------------------------------------------------------------------
		[TestMethod]
		[RequiresFullWindow]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.Native)]
		[GitHubWorkItem("https://github.com/unoplatform/kahua-private/issues/480")]
		public async Task When_MenuFlyout_Opens_First_Time_Foreground_Should_Not_Flash_Wrong_Theme()
		{
#if HAS_UNO
			using var darkApp = ThemeHelper.UseApplicationDarkTheme();
			await WindowHelper.WaitForIdle();
#endif

			var host = (Border)XamlReader.Load(
				"""
				<Border xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
						xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
						RequestedTheme="Light">
					<Border.Resources>
						<ResourceDictionary>
							<ResourceDictionary.ThemeDictionaries>
								<ResourceDictionary x:Key="Light">
									<SolidColorBrush x:Key="MenuItemBrush" Color="Green" />
								</ResourceDictionary>
								<ResourceDictionary x:Key="Dark">
									<SolidColorBrush x:Key="MenuItemBrush" Color="Red" />
								</ResourceDictionary>
							</ResourceDictionary.ThemeDictionaries>
						</ResourceDictionary>
					</Border.Resources>
					<Button x:Name="Owner" Content="Owner">
						<Button.Flyout>
							<MenuFlyout>
								<MenuFlyoutItem x:Name="MenuItem" Text="Item"
										Foreground="{ThemeResource MenuItemBrush}" />
							</MenuFlyout>
						</Button.Flyout>
					</Button>
				</Border>
				""");

			var owner = (Button)host.FindName("Owner");
			var flyout = (MenuFlyout)owner.Flyout;
			var item = (MenuFlyoutItem)host.FindName("MenuItem");

			// Capture every Foreground value the menu item takes from the moment the
			// popup is opened, both via snapshot at each lifecycle checkpoint and via
			// property-change callback for anything in between. Visible checkpoints
			// (from after-show-sync onwards) appearing as the Dark sentinel (Red)
			// would mean the popup rendered one frame with the wrong-theme brush.
			var observed = new List<(string Checkpoint, Color? Color)>();
			void Snapshot(string checkpoint)
				=> observed.Add((checkpoint, (item.Foreground as SolidColorBrush)?.Color));

			var root = new Border { Child = host };
			WindowHelper.WindowContent = root;
			await WindowHelper.WaitForLoaded(root);

			var token = item.RegisterPropertyChangedCallback(
				Control.ForegroundProperty,
				(s, dp) => Snapshot("callback"));

			try
			{
				// IMPORTANT: ShowAt's synchronous tail puts the popup in the visual
				// tree and is the last code path that runs before the first measure /
				// render of the menu. The Foreground at this point is what the user
				// sees on the first frame; if it is Red here, the flash is real.
				flyout.ShowAt(owner);
				Snapshot("after-show-sync");

				await WindowHelper.WaitForIdle();
				Snapshot("after-first-idle");

				await WindowHelper.WaitForLoaded(item);
				Snapshot("after-item-loaded");

				await WindowHelper.WaitForIdle();
				Snapshot("after-second-idle");

				// Final state must be the Light sentinel — confirms the existing fix
				// converges, so any Red below is a transient flash, not a final regression.
				Assert.AreEqual(ElementTheme.Light, item.ActualTheme,
					"Menu item should inherit the owner's Light theme.");
				Assert.AreEqual(Colors.Green, (item.Foreground as SolidColorBrush)?.Color,
					"Final menu item Foreground must be the Light sentinel (Green).");

				// Visible checkpoints only — pre-show observations are not user-visible
				// because the item is not in any rendered tree before ShowAt.
				var visibleCheckpoints = observed.Where(o => o.Checkpoint != "callback").ToList();
				var visibleRed = visibleCheckpoints.Where(o => o.Color == Colors.Red).ToList();
				Assert.AreEqual(0, visibleRed.Count,
					$"Menu item Foreground was the Dark sentinel (Red) at user-visible " +
					$"checkpoint(s) {string.Join(", ", visibleRed.Select(c => c.Checkpoint))}, " +
					$"producing the visible first-open flash before the corrective theme walk " +
					$"converged. All observations (checkpoint → color): " +
					$"[{string.Join(", ", observed.Select(o => $"{o.Checkpoint}={o.Color}"))}]");
			}
			finally
			{
				item.UnregisterPropertyChangedCallback(Control.ForegroundProperty, token);
				flyout.Hide();
#if HAS_UNO
				VisualTreeHelper.CloseAllPopups(WindowHelper.XamlRoot);
#endif
			}
		}
	}
}
