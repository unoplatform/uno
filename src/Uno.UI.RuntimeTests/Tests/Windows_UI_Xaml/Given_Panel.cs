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
		Assert.IsTrue(Math.Abs(parent.ActualSize.X - lv.ActualSize.X) < 1);
		Assert.IsTrue(Math.Abs(parent.ActualSize.Y - lv.ActualSize.Y) < 1);

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
				Assert.IsTrue(Math.Abs(child.DesiredSize.Height - child.ActualSize.Y) < 1);
			}
			else
			{
				Assert.IsTrue(Math.Abs(child.DesiredSize.Width - child.ActualSize.X) < 1);
			}
		}
	}
}
