using System.Drawing;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.Icons;

[TestClass]
[RunsOnUIThread]
public class Given_BitmapIcon
{
	[TestMethod]
	public async Task When_Themed()
	{
		var textBlock = new TextBlock()
		{
			Text = "test"
		};
		var bitmapIcon = new BitmapIcon()
		{
			UriSource = new System.Uri("ms-appx:///Assets/Icons/search.png")
		};
		var stackPanel = new StackPanel()
		{
			Children =
			{
				textBlock, bitmapIcon
			}
		};
		TestServices.WindowHelper.WindowContent = stackPanel;
		await TestServices.WindowHelper.WaitForLoaded(stackPanel);

		var textBlockBrush = (SolidColorBrush)textBlock.Foreground;
		var bitmapIconBrush = (SolidColorBrush)bitmapIcon.Foreground;
		Assert.AreEqual(textBlockBrush.Color, bitmapIconBrush.Color);

		using (ThemeHelper.UseDarkTheme())
		{
			textBlockBrush = (SolidColorBrush)textBlock.Foreground;
			bitmapIconBrush = (SolidColorBrush)bitmapIcon.Foreground;
			Assert.AreEqual(textBlockBrush.Color, bitmapIconBrush.Color);
		}
	}

	[TestMethod]
	public async Task When_Themed_Uwp()
	{
		using var _ = StyleHelper.UseUwpStyles();
		await When_Themed();
	}

	[TestMethod]
	public async Task When_Foreground_Set_With_ShowAsMonochrome_False()
	{
		var bitmapIcon = new BitmapIcon()
		{
			Width = 50,
			Height = 50,
			ShowAsMonochrome = false,
			UriSource = new System.Uri("ms-appx:///Assets/Icons/search.png"),
			Foreground = new SolidColorBrush(Colors.Green)
		};

		TestServices.WindowHelper.WindowContent = bitmapIcon;
		await TestServices.WindowHelper.WaitForLoaded(bitmapIcon);
		await TestServices.WindowHelper.WaitForIdle();

		// Foreground is ignored when ShowAsMonochrome is false
		var sc = await UITestHelper.ScreenShot(bitmapIcon);
		ImageAssert.DoesNotHaveColorInRectangle(sc, new Rectangle(0, 0, sc.Width, sc.Height), Colors.Green);
	}

	[TestMethod]
	public async Task When_Foreground_Set_With_ShowAsMonochrome_True()
	{
		var bitmapIcon = new BitmapIcon
		{
			Width = 50,
			Height = 50,
			ShowAsMonochrome = true,
			UriSource = new System.Uri("ms-appx:///Assets/image.png"),
			Foreground = new SolidColorBrush(Colors.Green)
		};

		await UITestHelper.Load(bitmapIcon);

		var sc = await UITestHelper.ScreenShot(bitmapIcon);
		ImageAssert.DoesNotHaveColorInRectangle(sc, new Rectangle(0, 0, sc.Width, sc.Height), Windows.UI.Color.FromArgb(255, 240, 28, 36));
		ImageAssert.DoesNotHaveColorInRectangle(sc, new Rectangle(0, 0, sc.Width, sc.Height), Windows.UI.Color.FromArgb(255, 255, 255, 255));
	}
}
