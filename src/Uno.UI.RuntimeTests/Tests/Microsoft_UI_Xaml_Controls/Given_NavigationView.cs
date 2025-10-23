using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
			ImageAssert.DoesNotHaveColorInRectangle(bitmap, new Rectangle(new Point(), bitmap.Size), Color.FromArgb(0xFF, 0x1B, 0, 0));
		}
	}
}
