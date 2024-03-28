using System;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation.Metadata;
using Windows.UI;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media;

#if HAS_UNO && !HAS_UNO_WINUI
using Microsoft/* UWP don't rename */.UI.Xaml.Media;
#endif

[TestClass]
public class Given_GradientBrush
{
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_GradientStop_Color_Changes()
	{
		if (!ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
		{
			Assert.Inconclusive(); // "System.NotImplementedException: RenderTargetBitmap is not supported on this platform.";
		}

		var rect = new Rectangle();
		rect.Width = 100;
		rect.Height = 100;
		var gradientStop1 = new GradientStop() { Color = Colors.Red, Offset = 0 };
		var gradientStop2 = new GradientStop() { Color = Colors.Yellow, Offset = 1 };
		rect.Fill = new LinearGradientBrush()
		{
			GradientStops = { gradientStop1, gradientStop2 }
		};

		WindowHelper.WindowContent = rect;
		await WindowHelper.WaitForLoaded(rect);

		gradientStop1.Color = Colors.Blue;

		var renderer = new RenderTargetBitmap();
		await WindowHelper.WaitForIdle();
		await renderer.RenderAsync(rect);

		var bitmap = await RawBitmap.From(renderer, rect);
#if __IOS__
		ImageAssert.HasColorAt(bitmap, 0, 0, Colors.Blue, tolerance: 55);
#else
		ImageAssert.HasColorAt(bitmap, 0, 0, Colors.Blue, tolerance: 5);
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
#if __ANDROID__ || __IOS__
	[Ignore("Fails on Android and iOS")]
#endif
	public async Task When_RadialGradientBrush_Ellipse_With_Non_Equal_Center_And_Origin()
	{
		if (!ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
		{
			Assert.Inconclusive(); // "System.NotImplementedException: RenderTargetBitmap is not supported on this platform.";
		}

		var rect = new Rectangle();
		rect.Width = 200;
		rect.Height = 200;
		var gradientStop1 = new GradientStop() { Color = Colors.Red, Offset = 0 };
		var gradientStop2 = new GradientStop() { Color = Colors.Blue, Offset = 0.5 };
		var gradientStop3 = new GradientStop() { Color = Colors.Yellow, Offset = 1 };
		rect.Fill = new RadialGradientBrush()
		{
			Center = new(0.4, 0.6),
			GradientOrigin = new(0.2, 0.3),
			RadiusX = 0.7,
			RadiusY = 0.3,
			GradientStops = { gradientStop1, gradientStop2, gradientStop3 }
		};

		await UITestHelper.Load(rect);
		await WindowHelper.WaitForIdle();
		var actualBitmap = await UITestHelper.ScreenShot(rect);

		var expected = new Image
		{
			Height = 200,
			Width = 200,
			Stretch = Stretch.None,
			Source = new BitmapImage(new Uri("ms-appx:///Assets/When_RadialGradientBrush_Ellipse_With_Non_Equal_Center_And_Origin_Expected.png"))
		};

		// NOTE: At the time of writing, the test works correctly in Uno without waiting for ImageOpened.
		// However, it fails in WinUI if it didn't wait for ImageOpened.
		var imageOpened = false;
		expected.ImageOpened += (_, _) => imageOpened = true;

		await UITestHelper.Load(expected);
		await WindowHelper.WaitFor(() => imageOpened);
		var expectedBitmap = await UITestHelper.ScreenShot(expected);
		await ImageAssert.AreSimilarAsync(actualBitmap, expectedBitmap);
	}

	[TestMethod]
	[RunsOnUIThread]
#if __ANDROID__ || __IOS__
	[Ignore("Fails on Android and iOS")]
#endif
	public async Task When_RadialGradientBrush_Ellipse_With_Equal_Center_And_Origin()
	{
		if (!ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
		{
			Assert.Inconclusive(); // "System.NotImplementedException: RenderTargetBitmap is not supported on this platform.";
		}

		var rect = new Rectangle();
		rect.Width = 200;
		rect.Height = 200;
		var gradientStop1 = new GradientStop() { Color = Colors.Red, Offset = 0 };
		var gradientStop2 = new GradientStop() { Color = Colors.Blue, Offset = 0.5 };
		var gradientStop3 = new GradientStop() { Color = Colors.Yellow, Offset = 1 };
		rect.Fill = new RadialGradientBrush()
		{
			Center = new(0.4, 0.6),
			GradientOrigin = new(0.4, 0.6),
			RadiusX = 0.7,
			RadiusY = 0.3,
			GradientStops = { gradientStop1, gradientStop2, gradientStop3 }
		};

		await UITestHelper.Load(rect);
		await WindowHelper.WaitForIdle();
		var actualBitmap = await UITestHelper.ScreenShot(rect);

		var expected = new Image
		{
			Height = 200,
			Width = 200,
			Stretch = Stretch.None,
			Source = new BitmapImage(new Uri("ms-appx:///Assets/When_RadialGradientBrush_Ellipse_With_Equal_Center_And_Origin_Expected.png"))
		};

		// NOTE: At the time of writing, the test works correctly in Uno without waiting for ImageOpened.
		// However, it fails in WinUI if it didn't wait for ImageOpened.
		var imageOpened = false;
		expected.ImageOpened += (_, _) => imageOpened = true;

		await UITestHelper.Load(expected);
		await WindowHelper.WaitFor(() => imageOpened);
		var expectedBitmap = await UITestHelper.ScreenShot(expected);
		await ImageAssert.AreSimilarAsync(actualBitmap, expectedBitmap);
	}

	[TestMethod]
	[RunsOnUIThread]
#if __ANDROID__ || __IOS__
	[Ignore("Fails on Android and iOS")]
#endif
	public async Task When_RadialGradientBrush_Circle_With_Non_Equal_Center_And_Origin()
	{
		if (!ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
		{
			Assert.Inconclusive(); // "System.NotImplementedException: RenderTargetBitmap is not supported on this platform.";
		}

		var rect = new Rectangle();
		rect.Width = 200;
		rect.Height = 200;
		var gradientStop1 = new GradientStop() { Color = Colors.Red, Offset = 0 };
		var gradientStop2 = new GradientStop() { Color = Colors.Blue, Offset = 0.5 };
		var gradientStop3 = new GradientStop() { Color = Colors.Yellow, Offset = 1 };
		rect.Fill = new RadialGradientBrush()
		{
			Center = new(0.4, 0.6),
			GradientOrigin = new(0.2, 0.3),
			RadiusX = 0.3,
			RadiusY = 0.3,
			GradientStops = { gradientStop1, gradientStop2, gradientStop3 }
		};

		await UITestHelper.Load(rect);
		await WindowHelper.WaitForIdle();
		var actualBitmap = await UITestHelper.ScreenShot(rect);

		var expected = new Image
		{
			Height = 200,
			Width = 200,
			Stretch = Stretch.None,
			Source = new BitmapImage(new Uri("ms-appx:///Assets/When_RadialGradientBrush_Circle_With_Non_Equal_Center_And_Origin_Expected.png"))
		};

		// NOTE: At the time of writing, the test works correctly in Uno without waiting for ImageOpened.
		// However, it fails in WinUI if it didn't wait for ImageOpened.
		var imageOpened = false;
		expected.ImageOpened += (_, _) => imageOpened = true;

		await UITestHelper.Load(expected);
		await WindowHelper.WaitFor(() => imageOpened);
		var expectedBitmap = await UITestHelper.ScreenShot(expected);
		await ImageAssert.AreSimilarAsync(actualBitmap, expectedBitmap);
	}

	[TestMethod]
	[RunsOnUIThread]
#if __ANDROID__ || __IOS__
	[Ignore("Fails on Android and iOS")]
#endif
	public async Task When_RadialGradientBrush_Circle_With_Equal_Center_And_Origin()
	{
		if (!ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
		{
			Assert.Inconclusive(); // "System.NotImplementedException: RenderTargetBitmap is not supported on this platform.";
		}

		var rect = new Rectangle();
		rect.Width = 200;
		rect.Height = 200;
		var gradientStop1 = new GradientStop() { Color = Colors.Red, Offset = 0 };
		var gradientStop2 = new GradientStop() { Color = Colors.Blue, Offset = 0.5 };
		var gradientStop3 = new GradientStop() { Color = Colors.Yellow, Offset = 1 };
		rect.Fill = new RadialGradientBrush()
		{
			Center = new(0.4, 0.6),
			GradientOrigin = new(0.4, 0.6),
			RadiusX = 0.3,
			RadiusY = 0.3,
			GradientStops = { gradientStop1, gradientStop2, gradientStop3 }
		};

		await UITestHelper.Load(rect);
		await WindowHelper.WaitForIdle();
		var actualBitmap = await UITestHelper.ScreenShot(rect);
		var expected = new Image
		{
			Height = 200,
			Width = 200,
			Stretch = Stretch.None,
			Source = new BitmapImage(new Uri("ms-appx:///Assets/When_RadialGradientBrush_Circle_With_Equal_Center_And_Origin_Expected.png"))
		};

		// NOTE: At the time of writing, the test works correctly in Uno without waiting for ImageOpened.
		// However, it fails in WinUI if it didn't wait for ImageOpened.
		var imageOpened = false;
		expected.ImageOpened += (_, _) => imageOpened = true;

		await UITestHelper.Load(expected);
		await WindowHelper.WaitFor(() => imageOpened);
		var expectedBitmap = await UITestHelper.ScreenShot(expected);
		await ImageAssert.AreSimilarAsync(actualBitmap, expectedBitmap);
	}
}
