#if __SKIA__ || WINAPPSDK
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
		ImageAssert.HasColorAt(screenshot, 20, 20, Colors.Red, tolerance: 20);
		ImageAssert.HasColorAt(screenshot, 20, 130, Colors.White, tolerance: 20);
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

		ImageAssert.HasColorAt(screenshot, 20, 20, Colors.White, tolerance: 20);
		ImageAssert.HasColorAt(screenshot, 20, 130, Colors.Red, tolerance: 20);
	}
}
#endif
