using System;
using System.Drawing;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Private.Infrastructure;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Microsoft.UI;
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

		await LoadOpened(bitmapIcon);

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

		await LoadOpened(bitmapIcon);

		var sc = await UITestHelper.ScreenShot(bitmapIcon);
		ImageAssert.DoesNotHaveColorInRectangle(sc, new Rectangle(0, 0, sc.Width, sc.Height), Windows.UI.Color.FromArgb(255, 240, 28, 36));
		ImageAssert.DoesNotHaveColorInRectangle(sc, new Rectangle(0, 0, sc.Width, sc.Height), Windows.UI.Color.FromArgb(255, 255, 255, 255));
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24489")]
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

		var image = await LoadOpened(bitmapIcon);

		// Sanity check - the initial red monochrome tint is actually rendered.
		// Catches false positives if the image fails to load.
		var initial = await UITestHelper.ScreenShot(bitmapIcon);
		ImageAssert.HasColorInRectangle(initial, new Rectangle(0, 0, initial.Width, initial.Height), Colors.Red);

		// Track reloads on the internal Image.
		int imageOpenedCount = 0;
		image.ImageOpened += (s, e) => imageOpenedCount++;

		// Change the foreground - the monochrome tint must update,
		// but the image itself must not be reloaded (no flicker).
		bitmapIcon.Foreground = new SolidColorBrush(Colors.Blue);
		await TestServices.WindowHelper.WaitForIdle();

		var after = await UITestHelper.ScreenShot(bitmapIcon);
		ImageAssert.DoesNotHaveColorInRectangle(after, new Rectangle(0, 0, after.Width, after.Height), Colors.Red);
		ImageAssert.HasColorInRectangle(after, new Rectangle(0, 0, after.Width, after.Height), Colors.Blue);
		Assert.AreEqual(0, imageOpenedCount, "Image must not be reloaded when foreground changes with ShowAsMonochrome=true");
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

		var image = await LoadOpened(bitmapIcon);

		// Track whether the image gets reloaded after our foreground change.
		int imageOpenedCount = 0;
		image.ImageOpened += (s, e) => imageOpenedCount++;

		// Change the foreground - should NOT trigger image reload when ShowAsMonochrome is false.
		bitmapIcon.Foreground = new SolidColorBrush(Colors.Blue);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(0, imageOpenedCount, "Image should not be reloaded when foreground changes with ShowAsMonochrome=false");
	}

	[TestMethod]
	// WinUI: the capture after toggling ShowAsMonochrome still shows the previous tint.
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_ShowAsMonochrome_Toggled_True_To_False_Image_Reflects_Change()
	{
		var bitmapIcon = new BitmapIcon
		{
			Width = 50,
			Height = 50,
			ShowAsMonochrome = true,
			UriSource = new Uri("ms-appx:///Assets/image.png"),
			Foreground = new SolidColorBrush(Colors.Red)
		};

		var image = await LoadOpened(bitmapIcon);

		// Sanity check - monochrome tint applied, image is red.
		var initial = await UITestHelper.ScreenShot(bitmapIcon);
		ImageAssert.HasColorInRectangle(initial, new Rectangle(0, 0, initial.Width, initial.Height), Colors.Red);

		int imageOpenedCount = 0;
		image.ImageOpened += (s, e) => imageOpenedCount++;

		// Toggle off - monochrome tint should be removed without reloading the image.
		bitmapIcon.ShowAsMonochrome = false;
		await TestServices.WindowHelper.WaitForIdle();

		var after = await UITestHelper.ScreenShot(bitmapIcon);
		ImageAssert.DoesNotHaveColorInRectangle(after, new Rectangle(0, 0, after.Width, after.Height), Colors.Red);
		Assert.AreEqual(0, imageOpenedCount, "Image must not be reloaded when ShowAsMonochrome toggles from true to false");
	}

	[TestMethod]
	// WinUI: the capture after toggling ShowAsMonochrome still shows the previous tint.
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_ShowAsMonochrome_Toggled_False_To_True_Image_Picks_Up_Foreground()
	{
		var bitmapIcon = new BitmapIcon
		{
			Width = 50,
			Height = 50,
			ShowAsMonochrome = false,
			UriSource = new Uri("ms-appx:///Assets/image.png"),
			Foreground = new SolidColorBrush(Colors.Blue)
		};

		var image = await LoadOpened(bitmapIcon);

		// Sanity check - original image colors (not blue-tinted).
		var initial = await UITestHelper.ScreenShot(bitmapIcon);
		ImageAssert.DoesNotHaveColorInRectangle(initial, new Rectangle(0, 0, initial.Width, initial.Height), Colors.Blue);

		int imageOpenedCount = 0;
		image.ImageOpened += (s, e) => imageOpenedCount++;

		// Toggle on - monochrome tint should apply using current Foreground without reloading.
		bitmapIcon.ShowAsMonochrome = true;
		await TestServices.WindowHelper.WaitForIdle();

		var after = await UITestHelper.ScreenShot(bitmapIcon);
		ImageAssert.HasColorInRectangle(after, new Rectangle(0, 0, after.Width, after.Height), Colors.Blue);
		Assert.AreEqual(0, imageOpenedCount, "Image must not be reloaded when ShowAsMonochrome toggles from false to true");
	}

	// Load only waits for the icon's size, which it has before its image is decoded.
	private static async Task<Image> LoadOpened(BitmapIcon bitmapIcon)
	{
		await UITestHelper.Load(bitmapIcon);

		var image = bitmapIcon.FindFirstDescendantOrThrow<Image>();
		await TestServices.WindowHelper.WaitFor(() => image.Source is BitmapSource { PixelWidth: > 0 }, timeoutMS: 5000, message: "The BitmapIcon's image should open.");
		await TestServices.WindowHelper.WaitForIdle();

		return image;
	}
}
