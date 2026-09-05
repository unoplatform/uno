using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using Windows.UI;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media;

[TestClass]
public class Given_LinearGradientBrush_UITest
{
	[TestMethod]
	[RunsOnUIThread]
#if !HAS_RENDER_TARGET_BITMAP
	[Ignore("Cannot take screenshot on this platform.")]
#endif
	public async Task When_GradientStops_Changed()
	{
		// Colors mirror the LinearGradientBrush_Change_Stops sample's UnoGreen/UnoBlue/UnoPurple/UnoRed resources.
		var brush = new LinearGradientBrush
		{
			StartPoint = new Point(0, 0),
			EndPoint = new Point(1, 1),
			GradientStops =
			{
				new GradientStop { Offset = 0.0, Color = Color.FromArgb(0xFF, 0x6C, 0xE5, 0xAE) },
				new GradientStop { Offset = 0.1, Color = Color.FromArgb(0xFF, 0x6C, 0xE5, 0xAE) },
				new GradientStop { Offset = 0.40, Color = Color.FromArgb(0xFF, 0x22, 0x9D, 0xFC) },
				new GradientStop { Offset = 0.60, Color = Color.FromArgb(0xFF, 0x7A, 0x69, 0xF5) },
				new GradientStop { Offset = 0.9, Color = Color.FromArgb(0xFF, 0xF6, 0x56, 0x78) },
				new GradientStop { Offset = 1.0, Color = Color.FromArgb(0xFF, 0xF6, 0x56, 0x78) },
			}
		};

		var rectangle = new Rectangle
		{
			Width = 150,
			Height = 100,
			Fill = brush,
		};

		try
		{
			await UITestHelper.Load(rectangle);

			var before = await UITestHelper.ScreenShot(rectangle);

			// Mirrors the sample's ChangeBrushButton_Click, which drops the two middle stops.
			brush.GradientStops.RemoveAt(2);
			brush.GradientStops.RemoveAt(2);

			await UITestHelper.WaitForIdle();
			var after = await UITestHelper.ScreenShot(rectangle);

			await ImageAssert.AreNotEqualAsync(after, before);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
#if !HAS_RENDER_TARGET_BITMAP
	[Ignore("Cannot take screenshot on this platform.")]
#endif
	public async Task When_Opacity_Is_Specified()
	{
		var grid = new Grid
		{
			Width = 200,
			Height = 200,
			Background = new LinearGradientBrush
			{
				Opacity = 0.5,
				StartPoint = new Point(0, 0),
				EndPoint = new Point(1, 1),
				GradientStops =
				{
					// Single color, to easily test the Opacity property.
					new GradientStop { Offset = 0.0, Color = Colors.Red },
					new GradientStop { Offset = 1.0, Color = Colors.Red },
				}
			}
		};

		try
		{
			await UITestHelper.Load(grid);

			// opaque:true composites over an opaque white background, like the page background in the original sample.
			var screenshot = await UITestHelper.ScreenShot(grid, opaque: true);

			ImageAssert.HasColorAt(screenshot, 100, 100, Color.FromArgb(255, 255, 128, 128), tolerance: 20);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}
}
