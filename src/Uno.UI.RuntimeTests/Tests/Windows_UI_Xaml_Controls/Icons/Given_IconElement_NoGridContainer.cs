#if HAS_UNO
using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using Windows.UI;
using Point = Windows.Foundation.Point;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.Icons;

[TestClass]
[RunsOnUIThread]
public class Given_IconElement_NoGridContainer
{
	private static readonly Uri SearchIcon = new("ms-appx:///Assets/Icons/search.png");
	private static readonly Uri ColoredImage = new("ms-appx:///Assets/image.png");

	private const string Glyph = "\uE890";

	[TestMethod]
	public void When_Disabled_FontIcon_Uses_Grid()
	{
		using var _ = FeatureConfigurationHelper.UseIconElementNoGridContainer(false);

		var fontIcon = new FontIcon { Glyph = Glyph };

		Assert.AreEqual(1, VisualTreeHelper.GetChildrenCount(fontIcon));
		var grid = VisualTreeHelper.GetChild(fontIcon, 0) as Grid;
		Assert.IsNotNull(grid);
		Assert.AreEqual(0, ((SolidColorBrush)grid.Background).Color.A);
		Assert.IsInstanceOfType(VisualTreeHelper.GetChild(grid, 0), typeof(TextBlock));
	}

	[TestMethod]
	public void When_Disabled_BitmapIcon_Uses_Grid()
	{
		using var _ = FeatureConfigurationHelper.UseIconElementNoGridContainer(false);

		var bitmapIcon = new BitmapIcon { UriSource = SearchIcon };

		Assert.AreEqual(1, VisualTreeHelper.GetChildrenCount(bitmapIcon));
		var grid = VisualTreeHelper.GetChild(bitmapIcon, 0) as Grid;
		Assert.IsNotNull(grid);
		Assert.AreEqual(0, ((SolidColorBrush)grid.Background).Color.A);
		Assert.IsInstanceOfType(VisualTreeHelper.GetChild(grid, 0), typeof(Image));
	}

	[TestMethod]
	public void When_Enabled_FontIcon_Hosts_TextBlock_Directly()
	{
		using var _ = FeatureConfigurationHelper.UseIconElementNoGridContainer(true);

		var fontIcon = new FontIcon { Glyph = Glyph };

		Assert.AreEqual(1, VisualTreeHelper.GetChildrenCount(fontIcon));
		var textBlock = VisualTreeHelper.GetChild(fontIcon, 0) as TextBlock;
		Assert.IsNotNull(textBlock);
		Assert.AreEqual(Glyph, textBlock.Text);
		Assert.AreEqual(AccessibilityView.Raw, textBlock.GetValue(AutomationProperties.AccessibilityViewProperty));
	}

	[TestMethod]
	public void When_Enabled_BitmapIcon_Hosts_Image_Directly()
	{
		using var _ = FeatureConfigurationHelper.UseIconElementNoGridContainer(true);

		var bitmapIcon = new BitmapIcon { UriSource = SearchIcon };

		Assert.AreEqual(1, VisualTreeHelper.GetChildrenCount(bitmapIcon));
		Assert.IsInstanceOfType(VisualTreeHelper.GetChild(bitmapIcon, 0), typeof(Image));
	}

	[TestMethod]
	public void When_Enabled_Other_Icons_Keep_Grid()
	{
		using var _ = FeatureConfigurationHelper.UseIconElementNoGridContainer(true);

		Assert.IsInstanceOfType(VisualTreeHelper.GetChild(new SymbolIcon(Symbol.Accept), 0), typeof(Grid));
		Assert.IsInstanceOfType(VisualTreeHelper.GetChild(new PathIcon(), 0), typeof(Grid));
	}

	[TestMethod]
	public async Task When_Enabled_FontIcon_Layout_Matches_Grid_Layout()
	{
		var legacy = CreateFontIcon(noGrid: false);
		var optimized = CreateFontIcon(noGrid: true);

		var panel = new StackPanel { Children = { legacy, optimized } };
		await UITestHelper.Load(panel);

		Assert.AreEqual(legacy.DesiredSize, optimized.DesiredSize);
		Assert.AreEqual(legacy.ActualWidth, optimized.ActualWidth);
		Assert.AreEqual(legacy.ActualHeight, optimized.ActualHeight);

		var legacyText = legacy.FindFirstDescendantOrThrow<TextBlock>();
		var optimizedText = optimized.FindFirstDescendantOrThrow<TextBlock>();

		Assert.AreEqual(legacyText.ActualWidth, optimizedText.ActualWidth);
		Assert.AreEqual(legacyText.ActualHeight, optimizedText.ActualHeight);
		Assert.AreEqual(
			legacyText.TransformToVisual(legacy).TransformPoint(default),
			optimizedText.TransformToVisual(optimized).TransformPoint(default));
	}

	[TestMethod]
	public async Task When_Enabled_BitmapIcon_Layout_Matches_Grid_Layout()
	{
		var legacy = CreateBitmapIcon(noGrid: false);
		var optimized = CreateBitmapIcon(noGrid: true);

		var panel = new StackPanel { Children = { legacy, optimized } };
		await UITestHelper.Load(panel);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(legacy.DesiredSize, optimized.DesiredSize);
		Assert.AreEqual(legacy.ActualWidth, optimized.ActualWidth);
		Assert.AreEqual(legacy.ActualHeight, optimized.ActualHeight);

		var legacyImage = legacy.FindFirstDescendantOrThrow<Image>();
		var optimizedImage = optimized.FindFirstDescendantOrThrow<Image>();

		Assert.AreEqual(legacyImage.ActualWidth, optimizedImage.ActualWidth);
		Assert.AreEqual(legacyImage.ActualHeight, optimizedImage.ActualHeight);
	}

	[TestMethod]
	[DataRow(true)]
	[DataRow(false)]
	public async Task When_Foreground_Reaches_Icon_Child(bool noGrid)
	{
		using var _ = FeatureConfigurationHelper.UseIconElementNoGridContainer(noGrid);

		var fontIcon = new FontIcon { Glyph = Glyph, Foreground = new SolidColorBrush(Colors.Red) };
		var bitmapIcon = new BitmapIcon { UriSource = SearchIcon, Foreground = new SolidColorBrush(Colors.Red) };
		var panel = new StackPanel { Children = { fontIcon, bitmapIcon } };

		await UITestHelper.Load(panel);

		var textBlock = fontIcon.FindFirstDescendantOrThrow<TextBlock>();
		var image = bitmapIcon.FindFirstDescendantOrThrow<Image>();

		Assert.AreEqual(Colors.Red, ((SolidColorBrush)textBlock.Foreground).Color);
		Assert.AreEqual(Colors.Red, image.MonochromeColor);

		fontIcon.Foreground = new SolidColorBrush(Colors.Blue);
		bitmapIcon.Foreground = new SolidColorBrush(Colors.Blue);

		Assert.AreEqual(Colors.Blue, ((SolidColorBrush)textBlock.Foreground).Color);
		Assert.AreEqual(Colors.Blue, image.MonochromeColor);

		bitmapIcon.ShowAsMonochrome = false;
		Assert.IsNull(image.MonochromeColor);
	}

	[TestMethod]
	[DataRow(true)]
	[DataRow(false)]
	public async Task When_Theme_Changes_Icon_Child_Follows(bool noGrid)
	{
		using var _ = FeatureConfigurationHelper.UseIconElementNoGridContainer(noGrid);

		var reference = new TextBlock { Text = "test" };
		var fontIcon = new FontIcon { Glyph = Glyph };
		var panel = new StackPanel { Children = { reference, fontIcon } };

		await UITestHelper.Load(panel);

		var textBlock = fontIcon.FindFirstDescendantOrThrow<TextBlock>();
		Assert.AreEqual(((SolidColorBrush)reference.Foreground).Color, ((SolidColorBrush)textBlock.Foreground).Color);

		using (ThemeHelper.UseDarkTheme())
		{
			Assert.AreEqual(((SolidColorBrush)reference.Foreground).Color, ((SolidColorBrush)textBlock.Foreground).Color);
		}
	}

	[TestMethod]
	[DataRow(true)]
	[DataRow(false)]
	public async Task When_UriSource_Changes_Image_Source_Follows(bool noGrid)
	{
		using var _ = FeatureConfigurationHelper.UseIconElementNoGridContainer(noGrid);

		var bitmapIcon = new BitmapIcon { Width = 50, Height = 50, UriSource = SearchIcon };
		await UITestHelper.Load(bitmapIcon);

		var image = bitmapIcon.FindFirstDescendantOrThrow<Image>();
		Assert.IsInstanceOfType(image.Source, typeof(BitmapImage));
		Assert.AreEqual(SearchIcon, ((BitmapImage)image.Source).UriSource);

		bitmapIcon.UriSource = ColoredImage;
		await TestServices.WindowHelper.WaitForIdle();
		Assert.AreEqual(ColoredImage, ((BitmapImage)image.Source).UriSource);

		bitmapIcon.UriSource = null;
		await TestServices.WindowHelper.WaitForIdle();
		Assert.IsNull(image.Source);
	}

	[TestMethod]
	[DataRow(true)]
	[DataRow(false)]
	public async Task When_RightToLeft_FontIcon_Is_Mirrored(bool noGrid)
	{
		using var _ = FeatureConfigurationHelper.UseIconElementNoGridContainer(noGrid);

		var fontIcon = new FontIcon
		{
			Glyph = Glyph,
			FontSize = 30,
			FlowDirection = FlowDirection.RightToLeft,
			MirroredWhenRightToLeft = true,
		};

		await UITestHelper.Load(fontIcon);

		var scaleTransform = fontIcon.RenderTransform as ScaleTransform;
		Assert.IsNotNull(scaleTransform);
		Assert.AreEqual(-1d, scaleTransform.ScaleX);
		Assert.AreEqual(new Point(0.5, 0.5), fontIcon.RenderTransformOrigin);

		var textBlock = fontIcon.FindFirstDescendantOrThrow<TextBlock>();
		Assert.AreEqual(Glyph, textBlock.Text);
		Assert.IsTrue(fontIcon.ActualWidth > 0);
	}

	[TestMethod]
	public async Task When_Enabled_Icon_Surface_Stays_HitTestable()
	{
		using var _ = FeatureConfigurationHelper.UseIconElementNoGridContainer(true);

		var fontIcon = new FontIcon { Glyph = Glyph, Width = 80, Height = 60 };
		var host = new Border { Width = 80, Height = 60, Child = fontIcon };

		await UITestHelper.Load(host);

		var center = fontIcon.TransformToVisual(null)
			.TransformPoint(new Point(fontIcon.ActualWidth / 2, fontIcon.ActualHeight / 2));
		var hits = VisualTreeHelper.FindElementsInHostCoordinates(center, host).ToArray();

		CollectionAssert.Contains(hits, fontIcon);
	}

	[TestMethod]
	public async Task When_Enabled_IconSourceElement_Still_Replaces_Child()
	{
		using var _ = FeatureConfigurationHelper.UseIconElementNoGridContainer(true);

		var iconSourceElement = new IconSourceElement { IconSource = new SymbolIconSource { Symbol = Symbol.Accept } };
		await UITestHelper.Load(iconSourceElement);

		Assert.IsInstanceOfType(VisualTreeHelper.GetChild(iconSourceElement, 0), typeof(Grid));
		Assert.IsNotNull(VisualTreeUtils.FindVisualChildByType<TextBlock>(iconSourceElement));

		iconSourceElement.IconSource = new BitmapIconSource { UriSource = SearchIcon };
		await TestServices.WindowHelper.WaitForIdle();

		Image image = null;
		await TestServices.WindowHelper.WaitFor(
			() => (image = VisualTreeUtils.FindVisualChildByType<Image>(iconSourceElement)) is not null);

		Assert.IsNull(VisualTreeUtils.FindVisualChildByType<TextBlock>(iconSourceElement));
		Assert.IsInstanceOfType(image.Source, typeof(BitmapImage));
	}

	[TestMethod]
#if !HAS_RENDER_TARGET_BITMAP
	[Ignore("Cannot take screenshot on this platform.")]
#endif
	public async Task When_Enabled_FontIcon_Renders_Like_Grid_Version()
	{
		var legacy = CreateFontIcon(noGrid: false);
		var optimized = CreateFontIcon(noGrid: true);

		var panel = new StackPanel { Children = { legacy, optimized } };
		await UITestHelper.Load(panel);
		await TestServices.WindowHelper.WaitForIdle();

		var legacyShot = await UITestHelper.ScreenShot(legacy);
		var optimizedShot = await UITestHelper.ScreenShot(optimized);

		await ImageAssert.AreSimilarAsync(optimizedShot, legacyShot);
	}

	[TestMethod]
#if !HAS_RENDER_TARGET_BITMAP
	[Ignore("Cannot take screenshot on this platform.")]
#endif
	public async Task When_Enabled_BitmapIcon_Renders_Monochrome()
	{
		using var _ = FeatureConfigurationHelper.UseIconElementNoGridContainer(true);

		var bitmapIcon = new BitmapIcon
		{
			Width = 50,
			Height = 50,
			ShowAsMonochrome = true,
			UriSource = ColoredImage,
			Foreground = new SolidColorBrush(Colors.Green),
		};

		await UITestHelper.Load(bitmapIcon);
		await TestServices.WindowHelper.WaitForIdle();

		var screenshot = await UITestHelper.ScreenShot(bitmapIcon);
		ImageAssert.HasColorInRectangle(
			screenshot,
			new Rectangle(0, 0, screenshot.Width, screenshot.Height),
			Colors.Green);
	}

	private static FontIcon CreateFontIcon(bool noGrid)
	{
		using var _ = FeatureConfigurationHelper.UseIconElementNoGridContainer(noGrid);

		return new FontIcon
		{
			Glyph = Glyph,
			FontSize = 24,
			Width = 60,
			Height = 40,
			Foreground = new SolidColorBrush(Colors.Red),
		};
	}

	private static BitmapIcon CreateBitmapIcon(bool noGrid)
	{
		using var _ = FeatureConfigurationHelper.UseIconElementNoGridContainer(noGrid);

		return new BitmapIcon
		{
			Width = 50,
			Height = 50,
			UriSource = SearchIcon,
		};
	}
}
#endif
