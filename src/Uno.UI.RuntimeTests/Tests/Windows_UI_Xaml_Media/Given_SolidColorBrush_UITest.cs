using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media;

[TestClass]
[RunsOnUIThread]
public class Given_SolidColorBrush_UITest
{
	// Mutating an existing SolidColorBrush's Color (rather than replacing the brush instance)
	// must still invalidate the render of every owner that references it.
	[TestMethod]
#if !HAS_RENDER_TARGET_BITMAP
	[Ignore("Cannot take screenshot on this platform.")]
#endif
	public async Task When_SolidColorBrush_Color_Changed()
	{
		var borderBrush = new SolidColorBrush(Colors.Green);
		var border = new Border
		{
			Width = 190,
			Height = 70,
			BorderThickness = new Thickness(15),
			CornerRadius = new CornerRadius(5),
			BorderBrush = borderBrush,
		};

		var gridBrush = new SolidColorBrush(Colors.IndianRed);
		var grid = new Grid
		{
			Width = 190,
			Height = 70,
			BorderThickness = new Thickness(15),
			CornerRadius = new CornerRadius(5),
			BorderBrush = gridBrush,
		};

		var strokeBrush = new SolidColorBrush(Colors.Violet);
		var strokeEllipse = new Ellipse
		{
			Width = 190,
			Height = 70,
			StrokeThickness = 15,
			Stroke = strokeBrush,
		};

		var fillBrush = new SolidColorBrush(Colors.DarkGoldenrod);
		var fillEllipse = new Ellipse
		{
			Width = 190,
			Height = 70,
			Stroke = new SolidColorBrush(Colors.Transparent),
			StrokeThickness = 15,
			Fill = fillBrush,
		};

		var root = new StackPanel
		{
			Spacing = 5,
			Children = { border, grid, strokeEllipse, fillEllipse },
		};

		try
		{
			await UITestHelper.Load(root);

			// Sample near the left edge (inside the 15px border/stroke stripe) for the
			// border/stroke owners, and dead-center for the filled ellipse.
			const float edgeOffset = 5f;

			await AssertColorAt(border, edgeOffset, (float)(border.ActualHeight / 2), Colors.Green);
			await AssertColorAt(grid, edgeOffset, (float)(grid.ActualHeight / 2), Colors.IndianRed);
			await AssertColorAt(strokeEllipse, edgeOffset, (float)(strokeEllipse.ActualHeight / 2), Colors.Violet);
			await AssertColorAt(fillEllipse, (float)(fillEllipse.ActualWidth / 2), (float)(fillEllipse.ActualHeight / 2), Colors.DarkGoldenrod);

			borderBrush.Color = Colors.Blue;
			gridBrush.Color = Colors.Blue;
			strokeBrush.Color = Colors.Blue;
			fillBrush.Color = Colors.Blue;

			await AssertColorAt(border, edgeOffset, (float)(border.ActualHeight / 2), Colors.Blue);
			await AssertColorAt(grid, edgeOffset, (float)(grid.ActualHeight / 2), Colors.Blue);
			await AssertColorAt(strokeEllipse, edgeOffset, (float)(strokeEllipse.ActualHeight / 2), Colors.Blue);
			await AssertColorAt(fillEllipse, (float)(fillEllipse.ActualWidth / 2), (float)(fillEllipse.ActualHeight / 2), Colors.Blue);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	private static async Task AssertColorAt(FrameworkElement element, float x, float y, Color expected)
	{
		var bitmap = await UITestHelper.ScreenShot(element);
		ImageAssert.HasColorAt(bitmap, x, y, expected, tolerance: 15);
	}
}
