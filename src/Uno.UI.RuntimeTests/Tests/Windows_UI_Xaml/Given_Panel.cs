using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Uno.Extensions;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls;
using static Private.Infrastructure.TestServices;
using Microsoft.UI.Xaml.Markup;
using Uno.UI.Extensions;
using System.Collections.ObjectModel;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

[TestClass]
public class Given_Panel
{
	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_Overriding_Measure_Arrange()
	{
		var grid = (Grid)XamlReader.Load(
			"""
				<Grid
					xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
					xmlns:local="using:Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls"
					xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
					xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
					mc:Ignorable="d"
					Height="800"
					Width="600">
						<ListView ItemsSource="1234">
							<ItemsControl.ItemsPanel>
								<ItemsPanelTemplate>
									<local:BaseLayoutOverrideCallingPanel />
								</ItemsPanelTemplate>
							</ItemsControl.ItemsPanel>
							<ItemsControl.ItemTemplate>
								<DataTemplate>
									<Grid Background="Black" >
										<Image x:Name="linuxImage" Source="ms-appx:///Uno.UI.RuntimeTests/Assets/linux.png" />
									</Grid>
								</DataTemplate>
							</ItemsControl.ItemTemplate>
						</ListView>
				</Grid>
			""");

		var lv = (ListView)grid.Children[0];

		WindowHelper.WindowContent = grid;

		await WindowHelper.WaitForLoaded(lv);

#if __SKIA__
		if (grid.FindName("linuxImage") is Image img
			&& img.Source is BitmapImage bitmapImage)
		{
			await WindowHelper.WaitForOpened(bitmapImage);
		}
		else
		{
			throw new InvalidOperationException("Image [linuxImage] is not found");
		}
#endif

		await WindowHelper.WaitFor(() => lv.Items.Select(item => ((ListViewItem)lv.ContainerFromItem(item)).DesiredSize.Width > 0).All(b => b));
		await WindowHelper.WaitForIdle();

		var parent = (UIElement)VisualTreeHelper.GetParent(lv);
		Assert.IsLessThan(1d, Math.Abs(parent.ActualSize.X - lv.ActualSize.X));
		Assert.IsLessThan(1d, Math.Abs(parent.ActualSize.Y - lv.ActualSize.Y));

		// MeasureOverride should be returning the default Size(0,0)
		Assert.AreEqual(0, lv.DesiredSize.Width);
		Assert.AreEqual(0, lv.DesiredSize.Height);

		for (var i = 0; i < 4; i++)
		{
			var child = (ListViewItem)lv.ContainerFromIndex(i);
			var image = lv.FindFirstChild<Image>();
			var imageSource = ((BitmapImage)image.Source);

			var availableSize = LayoutInformation.GetAvailableSize(child);

			var givenRatio = availableSize.Width / availableSize.Height;
			var imageRatio = (double)imageSource.PixelWidth / imageSource.PixelHeight;
			if (imageRatio < givenRatio)
			{
				Assert.IsLessThan(1d, Math.Abs(child.DesiredSize.Height - child.ActualSize.Y));
			}
			else
			{
				Assert.IsLessThan(1d, Math.Abs(child.DesiredSize.Width - child.ActualSize.X));
			}
		}
	}

	// Repro tests for https://github.com/unoplatform/uno/issues/606
	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/606")]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_Layout_With_Large_Margin_Does_Not_Produce_Negative_Rect()
	{
		// Issue: When AllowNegativeWidthHeight=false (strict UWP behavior), some panels/controls
		// pass negative Width/Height to Rect during Measure/Arrange, causing exceptions.
		// Expected: Layout should clamp negative sizes to 0 instead of creating negative Rects.
		// Note: The original bug was reported primarily on iOS (UIKit native layout path).
		// These tests pass on Skia Desktop — they serve as regression tests for managed layout.
		// The iOS-specific layout path in VirtualizingPanelLayout.UIKit.cs may still be affected.

		var originalValue = Uno.FoundationFeatureConfiguration.Rect.AllowNegativeWidthHeight;
		Uno.FoundationFeatureConfiguration.Rect.AllowNegativeWidthHeight = false;

		try
		{
			// Scenario: Border with Padding larger than its Width/Height
			// This commonly causes the content area to have a negative size.
			var border = new Border
			{
				Width = 30,
				Height = 30,
				Padding = new Thickness(50), // padding (100 total) exceeds size (30)
				Child = new Border
				{
					Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Red)
				}
			};

			WindowHelper.WindowContent = border;
			// Should not throw ArgumentOutOfRangeException about negative width/height
			await WindowHelper.WaitForLoaded(border);
			await WindowHelper.WaitForIdle();
		}
		finally
		{
			Uno.FoundationFeatureConfiguration.Rect.AllowNegativeWidthHeight = originalValue;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/606")]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_Layout_With_Large_Margin_On_Child_Does_Not_Produce_Negative_Rect()
	{
		// Issue: Panels create Rect with negative dimensions when a child has margins
		// that exceed the available space.

		var originalValue = Uno.FoundationFeatureConfiguration.Rect.AllowNegativeWidthHeight;
		Uno.FoundationFeatureConfiguration.Rect.AllowNegativeWidthHeight = false;

		try
		{
			// Scenario: Grid with a small size where child margins exceed available space
			var grid = new Grid
			{
				Width = 20,
				Height = 20,
			};
			grid.Children.Add(new Border
			{
				Margin = new Thickness(30), // 60 total margin exceeds grid 20x20
				Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Blue)
			});

			WindowHelper.WindowContent = grid;
			// Should not throw ArgumentOutOfRangeException about negative width/height
			await WindowHelper.WaitForLoaded(grid);
			await WindowHelper.WaitForIdle();
		}
		finally
		{
			Uno.FoundationFeatureConfiguration.Rect.AllowNegativeWidthHeight = originalValue;
		}
	}
}
