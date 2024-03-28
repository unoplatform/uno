using System.Threading.Tasks;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.Icons;

[TestClass]
[RunsOnUIThread]
public class Given_PathIcon
{
	[TestMethod]
	public async Task When_Themed()
	{
		var textBlock = new TextBlock() { Text = "test" };
		var ellipseGeometry = new EllipseGeometry()
		{
			RadiusX = 20,
			RadiusY = 20,
		};
		var pathIcon = new PathIcon() { Data = ellipseGeometry };
		var stackPanel = new StackPanel()
		{
			Children =
			{
				textBlock,
				pathIcon
			}
		};
		TestServices.WindowHelper.WindowContent = stackPanel;
		await TestServices.WindowHelper.WaitForLoaded(stackPanel);

		var textBlockBrush = (SolidColorBrush)textBlock.Foreground;
		var pathIconBrush = (SolidColorBrush)pathIcon.Foreground;
		Assert.AreEqual(textBlockBrush.Color, pathIconBrush.Color);

		using (ThemeHelper.UseDarkTheme())
		{
			textBlockBrush = (SolidColorBrush)textBlock.Foreground;
			pathIconBrush = (SolidColorBrush)pathIcon.Foreground;
			Assert.AreEqual(textBlockBrush.Color, pathIconBrush.Color);
		}
	}

	[TestMethod]
	public async Task When_Themed_Fluent()
	{
		using (StyleHelper.UseFluentStyles())
		{
			await When_Themed();
		}
	}

	[TestMethod]
	public async Task When_Themed_Path()
	{
		var textBlock = new TextBlock() { Text = "test" };
		var ellipseGeometry = new EllipseGeometry()
		{
			RadiusX = 20,
			RadiusY = 20,
		};
		var pathIcon = new PathIcon() { Data = ellipseGeometry };
		var stackPanel = new StackPanel()
		{
			Children =
			{
				textBlock,
				pathIcon
			}
		};
		TestServices.WindowHelper.WindowContent = stackPanel;
		await TestServices.WindowHelper.WaitForLoaded(stackPanel);

		var innerPath = VisualTreeUtils.FindVisualChildByType<Path>(pathIcon);

		var textBlockBrush = (SolidColorBrush)textBlock.Foreground;
		var pathIconBrush = (SolidColorBrush)innerPath.Fill;
		Assert.AreEqual(textBlockBrush.Color, pathIconBrush.Color);

		using (ThemeHelper.UseDarkTheme())
		{
			textBlockBrush = (SolidColorBrush)textBlock.Foreground;
			pathIconBrush = (SolidColorBrush)innerPath.Fill;
			Assert.AreEqual(textBlockBrush.Color, pathIconBrush.Color);
		}

		pathIcon.Foreground = new SolidColorBrush(Colors.Red);
		pathIconBrush = (SolidColorBrush)innerPath.Fill;
		Assert.AreEqual(Colors.Red, pathIconBrush.Color);
	}


	[TestMethod]
	public async Task When_Themed_Path_Fluent()
	{
		using (StyleHelper.UseFluentStyles())
		{
			await When_Themed_Path();
		}
	}
}
