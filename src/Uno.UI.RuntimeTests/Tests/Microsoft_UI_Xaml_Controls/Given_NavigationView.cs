using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using MUXC = Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Microsoft.UI.Xaml.Markup;
using SamplesApp.UITests;
using Uno.Extensions;
using Uno.Extensions.Specialized;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Helpers;

using static Private.Infrastructure.TestServices;
using Color = Windows.UI.Color;
using Point = Windows.Foundation.Point;
#if HAS_INPUT_INJECTOR
using Windows.UI.Input.Preview.Injection;
using Uno.UI.Toolkit.DevTools.Input;
using Uno.UI.Toolkit.Extensions;
#endif
#if __APPLE_UIKIT__
using UIKit;
#endif

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public partial class Given_NavigationView
	{
		[TestMethod]
		[RequiresFullWindow]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_SelectedItem_Set_Before_Load_And_Theme_Changed()
		{
			var navView = new MUXC.NavigationView()
			{
				MenuItems =
				{
					new MUXC.NavigationViewItem {Content = "Item 1"},
					new MUXC.NavigationViewItem {Content = "Item 2"},
					new MUXC.NavigationViewItem {Content = "Item 3"},
				},
				PaneDisplayMode = MUXC.NavigationViewPaneDisplayMode.LeftMinimal,
				IsBackButtonVisible = MUXC.NavigationViewBackButtonVisible.Collapsed,
			};
			navView.SelectedItem = navView.MenuItems[1];
			var hostGrid = new Grid() { MinWidth = 20, MinHeight = 20 };

			WindowHelper.WindowContent = hostGrid;

			await WindowHelper.WaitForLoaded(hostGrid);

			hostGrid.Children.Add(navView);

			await WindowHelper.WaitForLoaded(navView);

			navView.IsPaneOpen = true;

			await WindowHelper.WaitForIdle();

			var togglePaneButton = navView.FindFirstDescendant<Button>(b => b.Name == "TogglePaneButton");
			// The toggle icon is an AnimatedIcon (AnimatedGlobalNavigationButtonVisualSource). It renders
			// its animated visual rather than the old fallback FontIcon, so there is no fallback TextBlock;
			// assert the themed color on the AnimatedIcon's Foreground brush, which drives the glyph color.
			var icon = togglePaneButton?.FindFirstDescendant<AnimatedIcon>(f => f.Name == "Icon");
			Assert.IsNotNull(icon);

			Color? IconColor() => (icon.Foreground as SolidColorBrush)?.Color;

			// Drive the theme explicitly (rather than relying on the ambient theme) so the test is
			// deterministic regardless of the host's default theme.
			navView.RequestedTheme = ElementTheme.Light;
			await WindowHelper.WaitForIdle();
			ColorAssert.IsDark(IconColor());

			navView.RequestedTheme = ElementTheme.Dark;
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(Colors.White, IconColor());
			ColorAssert.IsLight(IconColor());
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_IsBackButtonVisible_Toggled()
		{
			// unoplatform/uno#19516
			// droid-specific: There is a bug where setting IsBackButtonVisible would "lock" the size of all NVIs
			// preventing resizing on items expansion/collapse.

			var sut = new MUXC.NavigationView()
			{
				Height = 500,
				IsBackButtonVisible = MUXC.NavigationViewBackButtonVisible.Collapsed,
				IsPaneToggleButtonVisible = false,
				PaneDisplayMode = MUXC.NavigationViewPaneDisplayMode.Left,
				CompactModeThresholdWidth = 10,
				ExpandedModeThresholdWidth = 50,
			};
			sut.ItemInvoked += (s, e) =>
			{
				// manual trigger for deepest/inner-most items
				if (e.InvokedItemContainer is MUXC.NavigationViewItem nvi &&
					nvi.MenuItems.Count == 0)
				{
					sut.IsBackButtonVisible = MUXC.NavigationViewBackButtonVisible.Visible;
				}
			};

			var nvis = new Dictionary<string, MUXC.NavigationViewItem>();
			//AddItems(sut.MenuItems, "", count: 4, depth: 1, maxDepth: 2);
			AddItems(sut.MenuItems, "", count: 3, depth: 1, maxDepth: 3);
			void AddItems(IList<object> target, string prefix, int count, int depth, int maxDepth)
			{
				for (int i = 0; i < count; i++)
				{
					var header = prefix + (char)('A' + i);
					var item = new MUXC.NavigationViewItem() { Content = header };

					if (depth < maxDepth) AddItems(item.MenuItems, header, count, depth + 1, maxDepth);

					target.Add(item);
					nvis.Add(header, item);
				}
			}

			// for debugging
			var panel = new StackPanel();
			void AddTestButton(string label, Action action)
			{
				var button = new Button() { Content = label };
				button.Click += (s, e) => action();
				panel.Children.Add(button);
			}
			AddTestButton("InvalidateMeasure", () =>
			{
				foreach (var ir in sut.EnumerateDescendants().OfType<MUXC.ItemsRepeater>())
				{
					ir.InvalidateMeasure();
				}
			});
			AddTestButton("IsBackButtonVisible toggle", () =>
				sut.IsBackButtonVisible = sut.IsBackButtonVisible == MUXC.NavigationViewBackButtonVisible.Collapsed
					? MUXC.NavigationViewBackButtonVisible.Visible
					: MUXC.NavigationViewBackButtonVisible.Collapsed);
			panel.Children.Add(sut);

			await UITestHelper.Load(panel, x => x.IsLoaded);

			var initialHeight = nvis["B"].ActualHeight;

			nvis["B"].IsExpanded = true;
			await UITestHelper.WaitForIdle();
			var partiallyExpandedHeight = nvis["B"].ActualHeight;

			nvis["BB"].IsExpanded = true;
			await UITestHelper.WaitForIdle();
			var fullyExpandedHeight = nvis["B"].ActualHeight;

			// trigger the bug
			await Task.Delay(2000); // necessary
			sut.IsBackButtonVisible = MUXC.NavigationViewBackButtonVisible.Visible;
			await UITestHelper.WaitForIdle();

			nvis["BB"].IsExpanded = false;
			await UITestHelper.WaitForIdle();
			var partiallyCollapsedHeight = nvis["B"].ActualHeight;

			nvis["B"].IsExpanded = false;
			await UITestHelper.WaitForIdle();
			var fullyCollapsedHeight = nvis["B"].ActualHeight;

			// sanity check
			Assert.IsLessThan(partiallyExpandedHeight, initialHeight, $"Expanding 'B' should increase item 'B' height: {initialHeight} -> {partiallyExpandedHeight}");
			Assert.IsLessThan(fullyExpandedHeight, partiallyExpandedHeight, $"Expanding 'BB' should increase item 'B' height: {partiallyExpandedHeight} -> {fullyExpandedHeight}");

			// verifying fix
			Assert.IsGreaterThan(partiallyCollapsedHeight, fullyExpandedHeight, $"Collapsing 'BB' should reduce item 'B' height: {fullyExpandedHeight} -> {partiallyCollapsedHeight}");
			Assert.IsGreaterThan(fullyCollapsedHeight, partiallyCollapsedHeight, $"Collapsing 'B' should reduce item 'B' height: {partiallyCollapsedHeight} -> {fullyCollapsedHeight}");
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/20199")]
		public async Task When_CompactPaneLength_Changed_PaneToggleButton_Updates()
		{
			var navView = new MUXC.NavigationView()
			{
				CompactPaneLength = 200,
				OpenPaneLength = 100,
				IsPaneOpen = true,
				Width = 800,
				Height = 400,
				MenuItems =
				{
					new MUXC.NavigationViewItem { Content = "Menu", Icon = new SymbolIcon(Symbol.AddFriend) },
					new MUXC.NavigationViewItem { Content = "Settings", Icon = new SymbolIcon(Symbol.Setting) },
				},
				Content = new Grid(),
			};

			await UITestHelper.Load(navView);

			var toggleButton = navView.FindFirstDescendant<Button>(b => b.Name == "TogglePaneButton");
			Assert.IsNotNull(toggleButton);

			var templateSettings = navView.TemplateSettings;
			Assert.AreEqual(200, templateSettings.PaneToggleButtonWidth, "Initial PaneToggleButtonWidth");
			Assert.AreEqual(192, templateSettings.SmallerPaneToggleButtonWidth, "Initial SmallerPaneToggleButtonWidth");
			Assert.AreEqual(192, toggleButton.MinWidth, "Initial toggle MinWidth");
			var initialActualWidth = toggleButton.ActualWidth;

			navView.CompactPaneLength += 100;
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(300, templateSettings.PaneToggleButtonWidth, "Updated PaneToggleButtonWidth");
			Assert.AreEqual(292, templateSettings.SmallerPaneToggleButtonWidth, "Updated SmallerPaneToggleButtonWidth");
			Assert.AreEqual(292, toggleButton.MinWidth, "Updated toggle MinWidth");
			Assert.IsTrue(toggleButton.ActualWidth > initialActualWidth + 50, $"Toggle button should grow: {initialActualWidth} -> {toggleButton.ActualWidth}");
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno-private/issues/1091")]
		public async Task When_Theme_Changes_NVItem_Foreground()
		{
			if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap, Uno.UI"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}

			var uc = (UserControl)XamlReader.Load(
				"""
				<UserControl xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
							 xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
							 xmlns:local="using:UITests.Shared.Windows_UI_Xaml_Media.Transform"
							 xmlns:controls="using:Uno.UI.Samples.Controls"
							 xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
							 xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
							 xmlns:muxc="using:Microsoft.UI.Xaml.Controls"
							 mc:Ignorable="d">
				
					<UserControl.Resources>
						<Style x:Key="DefaultNavigationViewItemStyle" TargetType="muxc:NavigationViewItem">
							<Setter Property="FontFamily" Value="XamlAutoFontFamily" />
						</Style>
						<Style x:Key="T1NavigationViewItemStyle"
						       BasedOn="{StaticResource DefaultNavigationViewItemStyle}"
						       TargetType="muxc:NavigationViewItem">
							<Setter Property="Foreground" Value="{ThemeResource TextFillColorPrimaryBrush}" />
						</Style>
					</UserControl.Resources>
					
					<NavigationView PaneDisplayMode="Left" IsPaneOpen="True">
						<NavigationView.MenuItems>
							<NavigationViewItem Background="Red" Content="Ramez" Style="{StaticResource T1NavigationViewItemStyle}" />
						</NavigationView.MenuItems>
					</NavigationView>
				</UserControl>                  
				""");

			await UITestHelper.Load(uc);

			using var _ = ThemeHelper.UseDarkTheme();
			await UITestHelper.WaitForIdle();

			var nvi = uc.FindFirstDescendant<NavigationViewItem>();
			var bitmap = await UITestHelper.ScreenShot(nvi);
			ImageAssert.DoesNotHaveColorInRectangle(bitmap, new Rectangle(new System.Drawing.Point(), bitmap.Size), Color.FromArgb(0xFF, 0x1B, 0, 0));
		}

		[TestMethod]
		[RequiresFullWindow]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/20588")]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Pane_Reopened_After_Close_Expanded_State_Restored()
		{
			// Issue #20588: A hierarchical item's expansion state must survive a pane close/reopen.
			// Pre-fix the pane-close path permanently collapsed items via CollapseMenuItemsInRepeater.

			var child = new MUXC.NavigationViewItem { Content = "Child" };
			var parent = new MUXC.NavigationViewItem
			{
				Content = "Parent",
				MenuItems = { child },
			};
			var leaf = new MUXC.NavigationViewItem { Content = "Leaf" };

			var nv = new MUXC.NavigationView
			{
				PaneDisplayMode = MUXC.NavigationViewPaneDisplayMode.LeftMinimal,
				IsBackButtonVisible = MUXC.NavigationViewBackButtonVisible.Collapsed,
				MenuItems = { parent, leaf },
			};

			await UITestHelper.Load(nv, x => x.IsLoaded);

			// LeftMinimal starts with the pane closed; open it like a user would.
			nv.IsPaneOpen = true;
			await WindowHelper.WaitForIdle();
			Assert.IsTrue(nv.IsPaneOpen, "Pane should be open before expanding");
			await WindowHelper.WaitFor(() => parent.ActualHeight > 0, timeoutMS: 3000, message: "Parent container should be visible in the open pane");

			parent.IsExpanded = true;
			await WindowHelper.WaitFor(() => child.ActualHeight > 0, timeoutMS: 3000, message: "Child container should be realized after expansion");

			nv.IsPaneOpen = false;
			await WindowHelper.WaitForIdle();

			nv.IsPaneOpen = true;
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(parent.IsExpanded, "Parent should still be expanded after pane reopen");
			await WindowHelper.WaitFor(() => child.ActualHeight > 0, timeoutMS: 3000, message: "Child container should be realized again after pane reopen");
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/8584")]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_MenuItems_Removed_While_Selected_Does_Not_Throw()
		{
			// Issue #8584: removing the currently-selected menu item must not throw.
			// Pre-fix the selection-model change handler threw ArgumentOutOfRangeException
			// from InspectingDataSource.GetAtCore via OnSelectionModelSelectionChanged.

			var nv = new MUXC.NavigationView
			{
				IsBackButtonVisible = MUXC.NavigationViewBackButtonVisible.Collapsed,
				IsBackEnabled = false,
				IsSettingsVisible = false,
				PaneDisplayMode = MUXC.NavigationViewPaneDisplayMode.Left,
				Width = 400,
				Height = 500,
				MenuItems =
				{
					new MUXC.NavigationViewItem { Content = "Sale" },
					new MUXC.NavigationViewItemSeparator(),
					new MUXC.NavigationViewItemHeader { Content = "Sales" },
				},
			};

			await UITestHelper.Load(nv);

			for (int i = 0; i < 2; i++)
			{
				var item = new MUXC.NavigationViewItem { Content = "Page", IsSelected = true };
				nv.MenuItems.Add(item);
				nv.SelectedItem = item;
				await WindowHelper.WaitForIdle();
			}

			// The repro: mutate selection + collection synchronously, with no idle wait between operations.
			var last = (MUXC.NavigationViewItem)nv.MenuItems[nv.MenuItems.Count - 1];
			nv.MenuItems.Remove(last);
			var remaining = nv.MenuItems.OfType<MUXC.NavigationViewItem>().Last();
			nv.SelectedItem = remaining;
			nv.MenuItems.Remove(remaining);
			var first = nv.MenuItems.OfType<MUXC.NavigationViewItem>().First();
			nv.SelectedItem = first;

			await WindowHelper.WaitForIdle();

			Assert.AreEqual(3, nv.MenuItems.Count, "Only the original 3 items should remain");
			Assert.AreEqual(nv.MenuItems[0], nv.SelectedItem, "SelectedItem should be the first remaining NavigationViewItem");
			Assert.IsTrue(((MUXC.NavigationViewItem)nv.MenuItems[0]).IsSelected, "First item should report IsSelected");
		}

		[TestMethod]
		[RequiresFullWindow]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/20911")]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_BackButtonVisible_Matches_WinUI_Metrics()
		{
			// Issue #20911: the back button must match shipped WinUI metrics (40x36, 4,2,4,2 margin, rounded corners).
			// Old Uno style was 40x40, zero margin, square corners.

			var nv = new MUXC.NavigationView
			{
				IsBackButtonVisible = MUXC.NavigationViewBackButtonVisible.Visible,
				PaneDisplayMode = MUXC.NavigationViewPaneDisplayMode.Left,
				IsSettingsVisible = false,
				Width = 400,
				Height = 400,
				MenuItems =
				{
					new MUXC.NavigationViewItem { Content = "Home" },
					new MUXC.NavigationViewItem { Content = "Profile" },
				},
			};

			await UITestHelper.Load(nv);

			var backButton = nv.FindFirstDescendant<Button>(b => b.Name == "NavigationViewBackButton");
			Assert.IsNotNull(backButton, "NavigationViewBackButton should be present");

			Assert.AreEqual(new Thickness(4, 2, 4, 2), backButton.Margin, "Back button margin");
			Assert.AreEqual(36d, backButton.ActualHeight, 0.5, $"Back button height should be 36, was {backButton.ActualHeight}");
			Assert.AreEqual(40d, backButton.ActualWidth, 0.5, $"Back button width should be 40, was {backButton.ActualWidth}");

			var rootGrid = backButton.FindFirstDescendant<Grid>(g => g.Name == "RootGrid");
			Assert.IsNotNull(rootGrid, "Back button template RootGrid should be present");
			Assert.AreNotEqual(new CornerRadius(0), rootGrid.CornerRadius, "Back button corners should be rounded, not square");

			// Horizontal position is stable (left-aligned with the 4px margin); vertical offset depends on the
			// title-bar/top-padding and is environment-dependent, so it is intentionally not asserted.
			var origin = backButton.TransformToVisual(nv).TransformPoint(default);
			Assert.AreEqual(4d, origin.X, 0.5, $"Back button left edge should be at x=4, was {origin.X}");
		}

		[TestMethod]
		[RequiresFullWindow]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/20193")]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_TopNav_Resize_SelectedItem_Moves_To_Overflow_Does_Not_Throw()
		{
			// Issue #20193: resizing a Top-mode NavigationView so the selected item moves into the
			// overflow popup must not throw (pre-fix: ArgumentOutOfRangeException during popup measure).

			var nv = new MUXC.NavigationView
			{
				PaneDisplayMode = MUXC.NavigationViewPaneDisplayMode.Top,
				IsBackButtonVisible = MUXC.NavigationViewBackButtonVisible.Collapsed,
				IsSettingsVisible = false,
				Width = 800,
				Height = 80,
			};

			for (int i = 0; i < 10; i++)
			{
				nv.MenuItems.Add(new MUXC.NavigationViewItem { Content = $"Item {i}" });
			}

			await UITestHelper.Load(nv);

			Button overflowButton = null;
			try
			{
				// Select the last item while all items still fit (no overflow at 800px).
				var lastItem = (MUXC.NavigationViewItem)nv.MenuItems[nv.MenuItems.Count - 1];
				Assert.IsTrue(lastItem.ActualWidth > 0, "Sanity: the last item should be visible (not overflowed) at 800px");
				nv.SelectedItem = lastItem;
				await WindowHelper.WaitForIdle();

				nv.Width = 250;
				nv.UpdateLayout();
				await WindowHelper.WaitForIdle();

				overflowButton = nv.FindFirstDescendant<Button>(b => b.Name == "TopNavOverflowButton");
				Assert.IsNotNull(overflowButton, "TopNavOverflowButton should be present once items overflow");

				overflowButton.Flyout.ShowAt(overflowButton);
				await WindowHelper.WaitForIdle();

				nv.Width = 500;
				nv.UpdateLayout();
				await WindowHelper.WaitForIdle();

				nv.Width = 250;
				nv.UpdateLayout();
				await WindowHelper.WaitForIdle();

				Assert.AreEqual(nv.MenuItems[nv.MenuItems.Count - 1], nv.SelectedItem, "Selected item should remain the last item after resizing");
			}
			finally
			{
				overflowButton?.Flyout?.Hide();
				await WindowHelper.WaitForIdle();
			}
		}

		[TestMethod]
		[RequiresFullWindow]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/19482")]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_BackButtonVisible_PaneToggleButton_Not_Clipped()
		{
			// Issue #19482: with a custom content margin and the back button visible, the pane toggle
			// button must not be clipped/collapsed.

			var nv = new MUXC.NavigationView
			{
				PaneDisplayMode = MUXC.NavigationViewPaneDisplayMode.Auto,
				IsBackButtonVisible = MUXC.NavigationViewBackButtonVisible.Visible,
				OpenPaneLength = 200,
				Width = 600,
				Height = 400,
				MenuItems =
				{
					new MUXC.NavigationViewItem { Content = "Home" },
				},
			};
			nv.Resources["NavigationViewContentMargin"] = new Thickness(0, 48, 0, 0);
			nv.Resources["NavigationViewMinimalContentMargin"] = new Thickness(0, 48, 0, 0);

			await UITestHelper.Load(nv);

			var toggle = nv.FindFirstDescendant<Button>(b => b.Name == "TogglePaneButton");
			Assert.IsNotNull(toggle, "TogglePaneButton should be present");
			Assert.AreEqual(40d, toggle.ActualHeight, 1d, $"Toggle button should not be collapsed, height was {toggle.ActualHeight}");

			var layoutRoot = toggle.FindFirstDescendant<Grid>(g => g.Name == "LayoutRoot");
			Assert.IsNotNull(layoutRoot, "Toggle button template LayoutRoot should be present");

			var layoutRootBottom = layoutRoot.TransformToVisual(toggle)
				.TransformBounds(new Rect(0, 0, layoutRoot.ActualWidth, layoutRoot.ActualHeight)).Bottom;
			Assert.IsTrue(layoutRootBottom <= toggle.ActualHeight - 1.5,
				$"Toggle inner content should keep its bottom margin (no clip): layoutRoot bottom {layoutRootBottom} vs toggle height {toggle.ActualHeight}");

			var backButton = nv.FindFirstDescendant<Button>(b => b.Name == "NavigationViewBackButton");
			Assert.IsNotNull(backButton, "NavigationViewBackButton should be present");

			// With the back button visible in an overlay (Minimal) pane, WinUI offsets the toggle
			// button by c_backButtonWidth (40) so the two buttons sit side by side.
			var backBounds = backButton.TransformToVisual(nv)
				.TransformBounds(new Rect(0, 0, backButton.ActualWidth, backButton.ActualHeight));
			var toggleBounds = toggle.TransformToVisual(nv)
				.TransformBounds(new Rect(0, 0, toggle.ActualWidth, toggle.ActualHeight));
			Assert.AreEqual(4d, backBounds.Left, 0.5, $"Back button should sit at x=4: back={backBounds}");
			Assert.AreEqual(40d, toggleBounds.Left, 0.5, $"Toggle button should be offset by the back button width: back={backBounds}, toggle={toggleBounds}");
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/20247")]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		public async Task When_NewTouchPointer_Recovers_Stale_Pressed_State()
		{
#if HAS_INPUT_INJECTOR
			// Issue #20247: a stale (never-released) touch pointer must not prevent a new touch pointer
			// from invoking an item. Pre-fix, all events from the second finger were silently swallowed.

			var itemA = new MUXC.NavigationViewItem { Content = "Item A" };
			var itemB = new MUXC.NavigationViewItem { Content = "Item B" };

			var nv = new MUXC.NavigationView
			{
				PaneDisplayMode = MUXC.NavigationViewPaneDisplayMode.Left,
				IsPaneOpen = true,
				IsBackButtonVisible = MUXC.NavigationViewBackButtonVisible.Collapsed,
				IsPaneToggleButtonVisible = false,
				Width = 400,
				Height = 500,
				MenuItems = { itemA, itemB },
			};

			await UITestHelper.Load(nv);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var finger1 = injector.GetFinger(id: 11);
			using var finger2 = injector.GetFinger(id: 22);

			try
			{
				// Stale pointer: press finger1 on item A and never release it.
				finger1.Press(itemA.GetAbsoluteBounds().GetCenter());
				await WindowHelper.WaitForIdle();

				// New pointer: a full press+release on item A should still invoke/select it.
				finger2.Press(itemA.GetAbsoluteBounds().GetCenter());
				await WindowHelper.WaitForIdle();
				finger2.Release();
				await WindowHelper.WaitForIdle();

				Assert.AreEqual(itemA, nv.SelectedItem, "Item A should be selected by the new touch pointer despite the stale pressed pointer");
			}
			finally
			{
				finger1.Release();
				await WindowHelper.WaitForIdle();
			}
#else
			await Task.CompletedTask;
#endif
		}
	}
}
