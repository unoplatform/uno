using System;
using System.Drawing;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.Extensions;
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
#elif !__SKIA__
	[Ignore("BitmapIcon flicker fix is Skia-specific - Image.skia.cs updates the surface brush color filter in place without reloading.")]
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

		// Sanity check - the initial red monochrome tint is actually rendered.
		// Catches false positives if the image fails to load.
		var initial = await UITestHelper.ScreenShot(bitmapIcon);
		ImageAssert.HasColorInRectangle(initial, new Rectangle(0, 0, initial.Width, initial.Height), Colors.Red);

		// Track reloads on the internal Image.
		var image = bitmapIcon.FindFirstDescendantOrThrow<Image>();
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

		await UITestHelper.Load(bitmapIcon);
		await TestServices.WindowHelper.WaitForIdle();

		var image = bitmapIcon.FindFirstDescendantOrThrow<Image>();

		// Track whether the image gets reloaded after our foreground change.
		int imageOpenedCount = 0;
		image.ImageOpened += (s, e) => imageOpenedCount++;

		// Change the foreground - should NOT trigger image reload when ShowAsMonochrome is false.
		bitmapIcon.Foreground = new SolidColorBrush(Colors.Blue);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(0, imageOpenedCount, "Image should not be reloaded when foreground changes with ShowAsMonochrome=false");
	}

	[TestMethod]
#if !HAS_RENDER_TARGET_BITMAP
	[Ignore("Cannot take screenshot on this platform.")]
#elif !__SKIA__
	[Ignore("BitmapIcon flicker fix is Skia-specific - Image.skia.cs updates the surface brush color filter in place without reloading.")]
#endif
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

		await UITestHelper.Load(bitmapIcon);
		await TestServices.WindowHelper.WaitForIdle();

		// Sanity check - monochrome tint applied, image is red.
		var initial = await UITestHelper.ScreenShot(bitmapIcon);
		ImageAssert.HasColorInRectangle(initial, new Rectangle(0, 0, initial.Width, initial.Height), Colors.Red);

		var image = bitmapIcon.FindFirstDescendantOrThrow<Image>();
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
#if !HAS_RENDER_TARGET_BITMAP
	[Ignore("Cannot take screenshot on this platform.")]
#elif !__SKIA__
	[Ignore("BitmapIcon flicker fix is Skia-specific - Image.skia.cs updates the surface brush color filter in place without reloading.")]
#endif
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

		await UITestHelper.Load(bitmapIcon);
		await TestServices.WindowHelper.WaitForIdle();

		// Sanity check - original image colors (not blue-tinted).
		var initial = await UITestHelper.ScreenShot(bitmapIcon);
		ImageAssert.DoesNotHaveColorInRectangle(initial, new Rectangle(0, 0, initial.Width, initial.Height), Colors.Blue);

		var image = bitmapIcon.FindFirstDescendantOrThrow<Image>();
		int imageOpenedCount = 0;
		image.ImageOpened += (s, e) => imageOpenedCount++;

		// Toggle on - monochrome tint should apply using current Foreground without reloading.
		bitmapIcon.ShowAsMonochrome = true;
		await TestServices.WindowHelper.WaitForIdle();

		var after = await UITestHelper.ScreenShot(bitmapIcon);
		ImageAssert.HasColorInRectangle(after, new Rectangle(0, 0, after.Width, after.Height), Colors.Blue);
		Assert.AreEqual(0, imageOpenedCount, "Image must not be reloaded when ShowAsMonochrome toggles from false to true");
	}
}
