using System;
using System.Threading.Tasks;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation.Metadata;
using Windows.UI;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media;

[TestClass]
public class Given_GradientBrush
{
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_GradientStop_Color_Changes()
	{
		if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
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
}
