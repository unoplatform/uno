using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media;

[TestClass]
public class Given_FontFamily
{
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Non_Existing_Font_Path_Does_Not_Break_Layout()
	{
		try
		{
			var outerStack = new Microsoft.UI.Xaml.Controls.StackPanel() { Spacing = 20 };
			var fontFamilyPath = new Uri("ms-appx:///Assets/Data/Fonts/SomeDefinitelyNotRealFont.ttf#IDontExist");
			var fontFamily = new Microsoft.UI.Xaml.Media.FontFamily(fontFamilyPath.AbsoluteUri);
			var textBlock = new Microsoft.UI.Xaml.Controls.TextBlock
			{
				Text = "Hello World",
				FontFamily = fontFamily,
				TextAlignment = Microsoft.UI.Xaml.TextAlignment.Center,
				VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center,
				HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center
			};
			var rootGrid = new Microsoft.UI.Xaml.Controls.Grid();
			rootGrid.Children.Add(textBlock);
			outerStack.Children.Add(rootGrid);

			TestServices.WindowHelper.WindowContent = outerStack;

			// This previously never loaded and caused layout exception.
			await TestServices.WindowHelper.WaitForLoaded(outerStack);

#if HAS_RENDER_TARGET_BITMAP
			var textBlockDuplicate = new Microsoft.UI.Xaml.Controls.TextBlock
			{
				Text = "Hello World",
				TextAlignment = Microsoft.UI.Xaml.TextAlignment.Center,
				VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center,
				HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center
			};
			var rootGridDuplicate = new Microsoft.UI.Xaml.Controls.Grid();
			rootGridDuplicate.Children.Add(textBlockDuplicate);

			outerStack.Children.Add(rootGridDuplicate);
			await TestServices.WindowHelper.WaitForLoaded(rootGridDuplicate);

			rootGrid.Width = 100;
			rootGrid.Height = 100;
			rootGridDuplicate.Width = 100;
			rootGridDuplicate.Height = 100;

			await TestServices.WindowHelper.WaitForIdle();

			// Compare screenshots
			var screenshotRootGrid = await UITestHelper.ScreenShot(rootGrid);
			var screenshotRootGridDuplicate = await UITestHelper.ScreenShot(rootGridDuplicate);
			await ImageAssert.AreSimilarAsync(screenshotRootGrid, screenshotRootGridDuplicate);
#endif
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}
}
