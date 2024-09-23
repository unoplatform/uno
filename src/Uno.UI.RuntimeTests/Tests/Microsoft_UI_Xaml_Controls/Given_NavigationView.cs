using System.Threading.Tasks;
#if WINAPPSDK
using Uno.UI.Extensions;
#elif __IOS__
using UIKit;
#elif __MACOS__
using AppKit;
#else
#endif
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Private.Infrastructure.TestServices;
using Windows.UI;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Helpers;

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
			var navView = new Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationView()
			{
				MenuItems =
				{
					new Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationViewItem {Content = "Item 1"},
					new Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationViewItem {Content = "Item 2"},
					new Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationViewItem {Content = "Item 3"},
				},
				PaneDisplayMode = Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationViewPaneDisplayMode.LeftMinimal,
				IsBackButtonVisible = Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationViewBackButtonVisible.Collapsed,
			};
			navView.SelectedItem = navView.MenuItems[1];
			var hostGrid = new Grid() { MinWidth = 20, MinHeight = 20 };

			WindowHelper.WindowContent = hostGrid;

			await WindowHelper.WaitForLoaded(hostGrid);

			hostGrid.Children.Add(navView);

			await WindowHelper.WaitForLoaded(navView);

			navView.IsPaneOpen = true;

			await WindowHelper.WaitForIdle();

			var togglePaneButton = navView.FindFirstChild<Button>(b => b.Name == "TogglePaneButton");
			var icon = togglePaneButton?.FindFirstChild<FrameworkElement>(f => f.Name == "Icon");
			var iconTextBlock = icon?.FindFirstChild<TextBlock>(includeCurrent: true);

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
				Assert.AreEqual(false, iconTextBlock.IsLayoutRequested);
				Assert.AreEqual((Android.Graphics.Color)((iconTextBlock.Foreground as SolidColorBrush).Color), iconTextBlock.NativeArrangedColor);
#endif
			}
		}
	}
}
