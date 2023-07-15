using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media;

[TestClass]
public class Given_GradientBrush
{
	[TestMethod]
	public async Task When_GradientStop_Color_Changes()
	{
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
	}
}
