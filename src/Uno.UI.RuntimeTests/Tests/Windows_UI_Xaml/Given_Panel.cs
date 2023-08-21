using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Uno.Extensions;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

[TestClass]
public class Given_Panel
{
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Overriding_Measure_Arrange()
	{
		var SUT = new BaseLayoutOverrideCallingPanel();
		for (var i = 0; i < 4; i++)
		{
			SUT.Children.Add(new Border { Child = new Image { Source = new BitmapImage(new Uri("ms-appx:///Uno.UI.RuntimeTests/Assets/linux.png")), Name = $"Image{i}"} });
		}

		WindowHelper.WindowContent = SUT;

		await WindowHelper.WaitFor(() => SUT.Children.Select(c => c.DesiredSize.Width > 0).AllTrue());
		await WindowHelper.WaitForLoaded(SUT);
		await WindowHelper.WaitForIdle();

		var parent = (UIElement)VisualTreeHelper.GetParent(SUT);
		Assert.IsTrue(Math.Abs(parent.ActualSize.X - SUT.ActualSize.X) < 1);
		Assert.IsTrue(Math.Abs(parent.ActualSize.Y - SUT.ActualSize.Y) < 1);
		Assert.IsTrue(Math.Abs(SUT.DesiredSize.Width - SUT.ActualSize.X) < 1);
		Assert.IsTrue(Math.Abs(SUT.DesiredSize.Height - SUT.ActualSize.Y) < 1);

		for (var i = 0; i < 4; i++)
		{
			var child = SUT.Children[i];
			var image = (Image)SUT.FindName($"Image{i}");
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
