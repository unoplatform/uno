using System;
using System.Drawing;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
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
#if !HAS_RENDER_TARGET_BITMAP
	[Ignore("Cannot take screenshot on this platform.")]
#endif
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
#if !HAS_RENDER_TARGET_BITMAP
	[Ignore("Cannot take screenshot on this platform.")]
#endif
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

	[TestMethod]
#if !HAS_RENDER_TARGET_BITMAP
	[Ignore("Cannot take screenshot on this platform.")]
#endif
	public async Task When_Foreground_Changed_With_ShowAsMonochrome_True()
	{
		var bitmapIcon = new BitmapIcon
		{
			Width = 50,
			Height = 50,
			ShowAsMonochrome = true,
			UriSource = new System.Uri("ms-appx:///Assets/image.png"),
			Foreground = new SolidColorBrush(Colors.Red)
		};

		await UITestHelper.Load(bitmapIcon);
		await TestServices.WindowHelper.WaitForIdle();

		// Change the foreground color - this should update the monochrome tint
		// without causing a full image reload (no flicker).
		bitmapIcon.Foreground = new SolidColorBrush(Colors.Blue);
		await TestServices.WindowHelper.WaitForIdle();

		var sc = await UITestHelper.ScreenShot(bitmapIcon);

		// The image should be visible and rendered in blue (not red, not the original colors).
		ImageAssert.DoesNotHaveColorInRectangle(sc, new Rectangle(0, 0, sc.Width, sc.Height), Colors.Red);
		ImageAssert.HasColorInRectangle(sc, new Rectangle(0, 0, sc.Width, sc.Height), Colors.Blue);
	}

	[TestMethod]
	public async Task When_ShowAsMonochrome_False_Foreground_Change_Does_Not_Reload_Image()
	{
		var bitmapIcon = new BitmapIcon
		{
			Width = 50,
			Height = 50,
			ShowAsMonochrome = false,
			UriSource = new Uri("ms-appx:///Assets/Icons/search.png"),
			Foreground = new SolidColorBrush(Colors.Red)
		};

		await UITestHelper.Load(bitmapIcon);
		await TestServices.WindowHelper.WaitForIdle();

		// Navigate the visual tree to find the internal Image control.
		// BitmapIcon → Grid (_rootGrid) → Image (_image)
		var rootGrid = VisualTreeHelper.GetChild(bitmapIcon, 0);
		var image = VisualTreeHelper.GetChild(rootGrid, 0) as Image;
		Assert.IsNotNull(image, "Expected Image child in BitmapIcon visual tree");

		// Track whether the image gets reloaded after our foreground change.
		int imageOpenedCount = 0;
		image.ImageOpened += (s, e) => imageOpenedCount++;

		// Change the foreground — should NOT trigger image reload when ShowAsMonochrome is false.
		bitmapIcon.Foreground = new SolidColorBrush(Colors.Blue);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(0, imageOpenedCount, "Image should not be reloaded when foreground changes with ShowAsMonochrome=false");
	}
}
