#if __SKIA__ || WINAPPSDK
using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Shapes;

[TestClass]
[RunsOnUIThread]
public class Given_Ellipse
{
	// An Ellipse with UniformToFill whose Width exceeds its container becomes a circle as large
	// as that width. Its desired height must therefore equal the width (300, not the container's
	// 150) so there is vertical overflow for VerticalAlignment to act on. The lower hemisphere of
	// the circle is then visible: wide near the top of the window, narrowing toward bottom-center.
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/18265")]
	public async Task When_UniformToFill_Wider_Than_Container_Bottom_Aligned()
	{
		var ellipse = new Ellipse
		{
			Width = 300,
			Stretch = Stretch.UniformToFill,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Bottom,
			Fill = new SolidColorBrush(Colors.Red),
		};
		var grid = new Grid
		{
			Width = 150,
			Height = 150,
			Background = new SolidColorBrush(Colors.White),
			Children = { ellipse },
		};

		await UITestHelper.Load(grid);
		var screenshot = await UITestHelper.ScreenShot(grid);

		// Top-left is inside the visible lower hemisphere, bottom-left is outside it.
		ImageAssert.HasColorAt(screenshot, 20, 20, Colors.Red, tolerance: 5);
		ImageAssert.HasColorAt(screenshot, 20, 130, Colors.White, tolerance: 5);
	}

	// Mirror of the scenario above: with VerticalAlignment=Top the upper hemisphere is visible
	// (narrow near the top of the window, wide toward bottom-center). Guards that the shape stays
	// a circle and alignment is honored in the opposite direction.
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/18265")]
	public async Task When_UniformToFill_Wider_Than_Container_Top_Aligned()
	{
		var ellipse = new Ellipse
		{
			Width = 300,
			Stretch = Stretch.UniformToFill,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Fill = new SolidColorBrush(Colors.Red),
		};
		var grid = new Grid
		{
			Width = 150,
			Height = 150,
			Background = new SolidColorBrush(Colors.White),
			Children = { ellipse },
		};

		await UITestHelper.Load(grid);
		var screenshot = await UITestHelper.ScreenShot(grid);

		ImageAssert.HasColorAt(screenshot, 20, 20, Colors.White, tolerance: 5);
		ImageAssert.HasColorAt(screenshot, 20, 130, Colors.Red, tolerance: 5);
	}
	// The fill's antialiasing ring must be one DEVICE pixel wide whatever the scale. A ring sized when the shape was
	// recorded, in DIPs, is four pixels wide at 4x and reads as a blurry edge.
	[TestMethod]
	[RequiresScaling(4f)]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_Fill_Edge_Is_One_Device_Pixel_At_4x()
	{
		var grid = new Grid
		{
			Width = 200,
			Height = 200,
			Background = new SolidColorBrush(Colors.White),
			Children = { new Ellipse { Width = 200, Height = 200, Fill = new SolidColorBrush(Colors.LimeGreen) } },
		};

		await UITestHelper.Load(grid);
		var screenshot = await UITestHelper.ScreenShot(grid);

		// Along the row through the centre the edge is vertical, so walking device pixels from the left crosses it
		// perpendicularly: white, at most one pixel between 5% and 95% covered, then solid green. A four-pixel ring
		// puts three or four pixels in that band.
		var bitmap = screenshot.Bitmap;
		var pixels = (await bitmap.GetPixelsAsync()).ToArray();
		int w = bitmap.PixelWidth, row = bitmap.PixelHeight / 2, partial = 0;
		for (var x = 0; x < w / 2; x++)
		{
			var r = pixels[(row * w + x) * 4 + 2];   // BGRA
			if (r > 60 && r < 245) { partial++; }
			else if (r <= 60) { break; }             // solid green reached
		}
		Assert.IsTrue(partial <= 1, $"{partial} partially covered pixels across the edge at 4x; the ring must be one device pixel wide");
	}
}
#endif
