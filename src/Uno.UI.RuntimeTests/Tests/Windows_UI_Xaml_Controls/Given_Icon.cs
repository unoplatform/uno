using System;
using System.Threading.Tasks;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation.Metadata;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	public class Given_Icon
	{
		[TestMethod]
		[RunsOnUIThread]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282! epic")]
#endif
		public async Task When_BitmapIcon_Is_Monochromatic()
		{
			if (!ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}

			var red = Colors.Red;

			var parent = new Border()
			{
				Width = 100,
				Height = 150,
				Background = new SolidColorBrush(Colors.Blue)
			};

			var SUT = new BitmapIcon()
			{
				Width = 100,
				Height = 150,
				UriSource = new Uri("ms-appx:///Assets/test_image_100_150.png"),
				Foreground = Colors.Red
			};

			parent.Child = SUT;

			TestServices.WindowHelper.WindowContent = parent;
			await WindowHelper.WaitForLoaded(parent);

			var snapshot = await TakeScreenshot(parent);

			var sample = SUT.GetRelativeCoords(parent);
			var centerX = sample.X + sample.Width / 2;
			var centerY = sample.Y + sample.Height / 2;

			ImageAssert.HasPixels(
				snapshot,
				ExpectedPixels
					.At(centerX, centerY)
					.Named("center")
					.WithPixelTolerance(1, 1)
					.Pixel(red),
				ExpectedPixels
					.At(sample.X, sample.Y)
					.Named("top-left")
					.WithPixelTolerance(1, 1)
					.Pixel(red),
				ExpectedPixels
					.At(sample.X + sample.Width - 1f, sample.Y)
					.Named("top-right")
					.WithPixelTolerance(1, 1)
					.Pixel(red),
				ExpectedPixels
					.At(sample.X, sample.Y + sample.Height - 1f)
					.Named("bottom-left")
					.WithPixelTolerance(1, 1)
					.Pixel(red),
				ExpectedPixels
					.At(sample.X + sample.Width - 1f, sample.Y + sample.Height - 1f)
					.Named("bottom-right")
					.WithPixelTolerance(1, 1)
					.Pixel(red));
		}

		private async Task<RawBitmap> TakeScreenshot(FrameworkElement SUT)
		{
			var renderer = new RenderTargetBitmap();
			await WindowHelper.WaitForIdle();
			await renderer.RenderAsync(SUT);
			var result = await RawBitmap.From(renderer, SUT);
			await WindowHelper.WaitForIdle();
			return result;
		}
	}
}
