using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Uno.Extensions;
using Uno.Extensions.Specialized;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Helpers;

using static Private.Infrastructure.TestServices;
using MUXC = Microsoft/* UWP don't rename */.UI.Xaml.Controls;

#if __IOS__
using UIKit;
#elif __MACOS__
using AppKit;
#endif

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public partial class Given_NavigationView
	{
		[TestMethod]
		[RequiresFullWindow]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
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
			var icon = togglePaneButton?.FindFirstDescendant<FrameworkElement>(f => f.Name == "Icon");
			var iconTextBlock = icon as TextBlock ?? icon?.FindFirstDescendant<TextBlock>();

			Assert.IsNotNull(iconTextBlock);

			ColorAssert.IsDark((iconTextBlock.Foreground as SolidColorBrush)?.Color);
			using (ThemeHelper.UseDarkTheme())
			{
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(Colors.White, (iconTextBlock.Foreground as SolidColorBrush)?.Color);
				ColorAssert.IsLight((iconTextBlock.Foreground as SolidColorBrush)?.Color);
#if __ANDROID__
				// This is the meat of the test - we verify that the actual color of the TextBlock matches the managed Color, which will only be the
				// case if it was correctly measured and arranged as requested after the theme changed.
				Assert.IsFalse(iconTextBlock.IsLayoutRequested);
				Assert.AreEqual((Android.Graphics.Color)((iconTextBlock.Foreground as SolidColorBrush).Color), iconTextBlock.NativeArrangedColor);
#endif
			}
		}

		[TestMethod]
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
			Assert.IsTrue(initialHeight < partiallyExpandedHeight, $"Expanding 'B' should increase item 'B' height: {initialHeight} -> {partiallyExpandedHeight}");
			Assert.IsTrue(partiallyExpandedHeight < fullyExpandedHeight, $"Expanding 'BB' should increase item 'B' height: {partiallyExpandedHeight} -> {fullyExpandedHeight}");

			// verifying fix
			Assert.IsTrue(fullyExpandedHeight > partiallyCollapsedHeight, $"Collapsing 'BB' should reduce item 'B' height: {fullyExpandedHeight} -> {partiallyCollapsedHeight}");
			Assert.IsTrue(partiallyCollapsedHeight > fullyCollapsedHeight, $"Collapsing 'B' should reduce item 'B' height: {partiallyCollapsedHeight} -> {fullyCollapsedHeight}");
		}
	}
}
