using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using static Private.Infrastructure.TestServices;
using Path = Microsoft.UI.Xaml.Shapes.Path;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Shapes;

[TestClass]
[RunsOnUIThread]
[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWasm)]
public class Given_ShapesClearData_UITest
{
	// "D" shaped geometry used by the original UITest sample (Path_ClearData).
	private const string PathData = "M25,25 L25,125 L75,125 C125,125 125,25 75,25 L25,25 Z";

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6846")]
	public async Task When_Data_Set_Path_Renders()
	{
		var border = CreateHost(out var path);
		path.Width = 150;
		path.Height = 150;
		path.Data = (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), PathData);
		path.Fill = new SolidColorBrush(Microsoft.UI.Colors.Green);

		try
		{
			await UITestHelper.Load(border);

			var screenshot = await UITestHelper.ScreenShot(border);
			var center = new Point(border.ActualWidth / 2, border.ActualHeight / 2);
			ImageAssert.HasColorAt(screenshot, center, Microsoft.UI.Colors.Green);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6846")]
	public async Task When_Data_Cleared_On_Loaded_Path_Does_Not_Render()
	{
		var border = CreateHost(out var path);
		path.Width = 100;
		path.Height = 150;
		path.Data = (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), PathData);
		path.Fill = new SolidColorBrush(Microsoft.UI.Colors.Red);
		// Mirrors the sample: clearing Data once the Path has loaded must stop it from rendering.
		path.Loaded += (s, e) => ((Path)s).Data = null;

		try
		{
			await UITestHelper.Load(border);

			var screenshot = await UITestHelper.ScreenShot(border);
			var center = new Point(border.ActualWidth / 2, border.ActualHeight / 2);
			ImageAssert.HasColorAt(screenshot, center, Microsoft.UI.Colors.White);
			ImageAssert.DoesNotHaveColorAt(screenshot, center, Microsoft.UI.Colors.Red);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6846")]
	public async Task When_No_Data_Path_Does_Not_Render()
	{
		var border = CreateHost(out var path);
		path.Width = 100;
		path.Height = 150;
		path.Fill = new SolidColorBrush(Microsoft.UI.Colors.Red);

		try
		{
			await UITestHelper.Load(border);

			var screenshot = await UITestHelper.ScreenShot(border);
			var center = new Point(border.ActualWidth / 2, border.ActualHeight / 2);
			ImageAssert.HasColorAt(screenshot, center, Microsoft.UI.Colors.White);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	private static Border CreateHost(out Path path)
	{
		path = new Path();
		return new Border
		{
			Width = 150,
			Height = 150,
			Background = new SolidColorBrush(Microsoft.UI.Colors.White),
			BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.DeepSkyBlue),
			BorderThickness = new Thickness(3),
			Child = path,
		};
	}
}
