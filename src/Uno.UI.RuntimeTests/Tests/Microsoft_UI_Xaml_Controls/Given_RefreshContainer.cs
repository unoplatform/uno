using System;
using System.Threading.Tasks;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	[RequiresFullWindow]
	public partial class Given_RefreshContainer
	{
		[TestMethod]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task When_Stretch_Child()
		{
			var grid = new Grid();
			var refreshContainer = new Microsoft/* UWP don't rename */.UI.Xaml.Controls.RefreshContainer();
			var child = new Border()
			{
				Background = new SolidColorBrush(Colors.Red),
				HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch,
				VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Stretch,
			};
			var grandChild = new Border()
			{
				Background = new SolidColorBrush(Colors.Blue),
				Width = 10,
				Height = 10,
			};

			child.Child = grandChild;
			refreshContainer.Content = child;
			grid.Children.Add(refreshContainer);

			WindowHelper.WindowContent = grid;

			await WindowHelper.WaitForLoaded(grandChild);
			await WindowHelper.WaitForLoaded(refreshContainer);

			await WindowHelper.WaitForIdle();

			Assert.IsTrue(child.ActualWidth > 50);
			Assert.IsTrue(child.ActualHeight > 50);
		}

		[TestMethod]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task When_Child_Empty_List()
		{
			if (!ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}

			var grid = new Grid() { Background = new SolidColorBrush(Colors.Red) };
			grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
			grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
			grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
			var leftStrip = new Border()
			{
				Background = new SolidColorBrush(Colors.Red),
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment = VerticalAlignment.Stretch
			};
			var rightStrip = new Border()
			{
				Background = new SolidColorBrush(Colors.Red),
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment = VerticalAlignment.Stretch
			};
			var refreshContainer = new Microsoft/* UWP don't rename */.UI.Xaml.Controls.RefreshContainer();
			var listView = new ListView();
			refreshContainer.Content = listView;
			refreshContainer.RefreshRequested += OnRefreshRequested;
			grid.Children.Add(refreshContainer);
			grid.Children.Add(leftStrip);
			grid.Children.Add(rightStrip);
			Grid.SetColumnSpan(refreshContainer, 3);
			Grid.SetColumn(leftStrip, 0);
			Grid.SetColumn(rightStrip, 2);

			// The left and right strip are in place to ensure they cover
			// the sides of refresh container, where the refresh indicator
			// would potentially show up in case it was not positioned
			// correctly in the middle.

			WindowHelper.WindowContent = grid;

			await WindowHelper.WaitForLoaded(grid);

			await WindowHelper.WaitForIdle();

			var screenshotBefore = await TakeScreenshot(grid);

			await WindowHelper.WaitForIdle();
			var screenshotBeforeVerify = await TakeScreenshot(grid);
			await ImageAssert.AreEqualAsync(screenshotBefore, screenshotBeforeVerify);

			Deferral deferral = null;
			refreshContainer.RequestRefresh();

			await Task.Delay(200); // Artificial delay to allow the indicator to animate in
			var screenshotAfter = await TakeScreenshot(grid);
			await ImageAssert.AreNotEqualAsync(screenshotBefore, screenshotAfter);
			deferral.Complete();

			void OnRefreshRequested(
				Microsoft/* UWP don't rename */.UI.Xaml.Controls.RefreshContainer sender,
				Microsoft/* UWP don't rename */.UI.Xaml.Controls.RefreshRequestedEventArgs args)
			{
				deferral = args.GetDeferral(); // Keep refreshing
			}
		}

		private Task<RawBitmap> TakeScreenshot(FrameworkElement SUT)
			=> UITestHelper.ScreenShot(SUT);
	}
}
