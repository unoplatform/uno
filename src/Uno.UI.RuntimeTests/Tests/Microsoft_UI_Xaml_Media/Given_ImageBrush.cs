using System;
using System.Threading.Tasks;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation.Metadata;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using static Private.Infrastructure.TestServices;
using ImageBrush = Microsoft.UI.Xaml.Media.ImageBrush;


namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_ImageBrush
	{
		[DataRow(Stretch.Fill, false)]
		[DataRow(Stretch.Fill, true)]
#if !__APPLE_UIKIT__
		// See https://github.com/unoplatform/uno/issues/9080
		[DataRow(Stretch.UniformToFill, false)]
#endif
		[DataRow(Stretch.UniformToFill, true)]
#if !__ANDROID__
		// Stretch.None is broken on Android.
		// See https://github.com/unoplatform/uno/pull/7238#issuecomment-937667565
		[DataRow(Stretch.None, false)]
		[DataRow(Stretch.None, true)]
#endif
#if !__APPLE_UIKIT__
		// See https://github.com/unoplatform/uno/issues/9080
		[DataRow(Stretch.Uniform, false)]
#endif
		[DataRow(Stretch.Uniform, true)]
		[TestMethod]
		public async Task When_Stretch(Stretch stretch, bool useRectangle)
		{
			const string Redish = "#FFEB1C24";
			const string Yellowish = "#FFFEF200";
			const string Greenish = "#FF0ED145";
			const string Transparent = "#00000000";

			if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}

			var brush = new ImageBrush
			{
				ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/colored-ellipse.jpg")),
				Stretch = stretch,
			};

			FrameworkElement SUT;
			if (useRectangle)
			{
				SUT = new Rectangle
				{
					Width = 100,
					Height = 100,
					StrokeThickness = 2,
					Stroke = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 0, 0)),
					Fill = brush,
				};
			}
			else
			{
				SUT = new Border
				{
					Width = 100,
					Height = 100,
					BorderThickness = new Thickness(2),
					BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 0, 0)),
					Background = brush,
				};
			}
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			// Retry is required because the brush even is not always raised
			await TestHelper.RetryAssert(async () =>
			{
				float BorderOffset =
#if __APPLE_UIKIT__ || __SKIA__
					6;
#else
					3;
#endif
				float width = (float)SUT.Width, height = (float)SUT.Height;
				float centerX = width / 2, centerY = height / 2;
				var expectations = stretch switch
				{
					// All edges are red-ish
					Stretch.Fill => (Top: Redish, Bottom: Redish, Left: Redish, Right: Redish),
					// Top and bottom are red-ish. Left and right are yellow-ish
					Stretch.UniformToFill => (Top: Redish, Bottom: Redish, Left: Yellowish, Right: Yellowish),
					// Top and bottom are same as background. Left and right are red-ish
					Stretch.Uniform => (Top: Transparent, Bottom: Transparent, Left: Redish, Right: Redish),
					// Everything is green-ish
					Stretch.None => (Top: Greenish, Bottom: Greenish, Left: Greenish, Right: Greenish),

					_ => throw new ArgumentOutOfRangeException($"unexpected stretch: {stretch}"),
				};

				var bitmap = await UITestHelper.ScreenShot(SUT);

				ImageAssert.HasColorAt(bitmap, centerX, BorderOffset, expectations.Top, tolerance: 28);
				ImageAssert.HasColorAt(bitmap, centerX, height - BorderOffset, expectations.Bottom, tolerance: 28);
				ImageAssert.HasColorAt(bitmap, BorderOffset, centerY, expectations.Left, tolerance: 28);
				ImageAssert.HasColorAt(bitmap, width - BorderOffset, centerY, expectations.Right, tolerance: 28);
			}, 10);
		}

#if __SKIA__
		[TestMethod]
		[Ignore("Flaky. https://github.com/unoplatform/uno/issues/9080")]
		public async Task When_DownSampling()
		{
			var border = new Border
			{
				Width = 100,
				Height = 100,
				Background = new ImageBrush
				{
					ImageSource = new Uri("ms-appx:/Assets/LargeWisteria.jpg")
				}
			};

			var image = new Image
			{
				Width = 100,
				Height = 100,
				Source = new Uri("ms-appx:/Assets/ResizedLargeWisteria.png")
			};

			await UITestHelper.Load(new StackPanel
			{
				Children =
				{
					border,
					image
				}
			});
			await Task.Delay(1000); // wait for idle might not be enough here

			var bitmap = await UITestHelper.ScreenShot(border);
			var bitmap2 = await UITestHelper.ScreenShot(image);
			await ImageAssert.AreEqualAsync(bitmap, bitmap2);
		}
#endif
	}
}
