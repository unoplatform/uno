using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Helpers;
using Path = Microsoft.UI.Xaml.Shapes.Path;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media;

[TestClass]
[RunsOnUIThread]
public class Given_Geometry_UITest
{
	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWasm)]
	public async Task When_EllipseGeometry()
	{
		var path = new Path
		{
			Fill = new SolidColorBrush(Colors.Red),
			Data = new EllipseGeometry
			{
				Center = new Point(80, 40),
				RadiusX = 80,
				RadiusY = 40
			}
		};

		var border = new Border
		{
			Background = new SolidColorBrush(Colors.Yellow),
			Child = path
		};

		try
		{
			await UITestHelper.Load(border);

			var screenshot = await UITestHelper.ScreenShot(border);
			ImageAssert.HasColorAt(screenshot, (float)(border.ActualWidth / 2), (float)(border.ActualHeight / 2), Colors.Red, tolerance: 5);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWasm)]
	public async Task When_LineGeometry()
	{
		var path = new Path
		{
			Stroke = new SolidColorBrush(Colors.Green),
			StrokeThickness = 20,
			Data = new LineGeometry
			{
				StartPoint = new Point(10, 10),
				EndPoint = new Point(50, 10)
			}
		};

		var border = new Border
		{
			Background = new SolidColorBrush(Colors.Yellow),
			Child = path
		};

		try
		{
			await UITestHelper.Load(border);

			var screenshot = await UITestHelper.ScreenShot(border);
			ImageAssert.HasColorAt(screenshot, (float)(border.ActualWidth / 2), (float)(border.ActualHeight / 2), Colors.Green, tolerance: 5);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}
}
