#if __SKIA__
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using SkiaSharp;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Storage;


namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media;

[TestClass]
public class Given_AcrylicBrush
{
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Drawn()
	{
		var img = new Image
		{
			Width = 200,
			Height = 200,
			Stretch = Stretch.UniformToFill,
			Source = ImageSource.TryCreateUriFromString("ms-appx:///Assets/test_image_200_200.png")
		};

		var actual = new Grid
		{
			Width = 200,
			Height = 200,
			Children =
			{
				img,
				new Border() { Width = 200, Height = 200, Background = new AcrylicBrush() }
			}
		};

		var noiseBitmap = new BitmapImage();
		await noiseBitmap.SetSourceAsync(typeof(AcrylicBrush).Assembly.GetManifestResourceStream("Uno.UI.Resources.NoiseAsset256x256.png"));

		var noiseImg = new Image
		{
			Width = 200,
			Height = 200,
			Stretch = Stretch.None,
			Source = noiseBitmap,
			Opacity = 0.02d
		};

		var storageFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/test_image_200_200.png"));
		using var stream = await storageFile.OpenStreamForReadAsync();
		using var image = SKImage.FromEncodedData(SKData.Create(stream));
		using var surface = SKSurface.Create(info: new SKImageInfo(image.Width, image.Height));
		using var canvas = surface.Canvas;
		using var paint = new SKPaint();
		using var filter = SKImageFilter.CreateBlur(30.0f, 30.0f, SKImageFilter.CreateImage(image, new SKSamplingOptions(SKCubicResampler.CatmullRom)));

		paint.IsAntialias = true;
		paint.ImageFilter = filter;
		canvas.DrawPaint(paint);

		using var snapshot = surface.Snapshot();

		var compositor = Compositor.GetSharedCompositor();
		using var brush = compositor.CreateSurfaceBrush(new SkiaCompositionSurface(snapshot));
		using var visual = compositor.CreateSpriteVisual();
		visual.Size = new(200, 200);
		visual.Brush = brush;

		var blurredImg = new Border() { Width = 200, Height = 200 };
		ElementCompositionPreview.SetElementChildVisual(blurredImg, visual);

		var expected = new Grid
		{
			Width = 200,
			Height = 200,
			Children =
			{
				blurredImg,
				noiseImg
			}
		};

		var renderResult = await Render(expected, actual);

		await ImageAssert.AreSimilarAsync(renderResult.actual, renderResult.expected, 0.04);
	}

	private async Task<(RawBitmap expected, RawBitmap actual)> Render(FrameworkElement expected, FrameworkElement actual)
	{
		// making sure the viewport is big enough for elements to fully render (and also accounting for DPI calculations)
		var expectedElm = new Grid { Width = 500, Height = 500, Children = { expected }, Background = new SolidColorBrush(Windows.UI.Colors.White) };
		var actualElm = new Grid { Width = 500, Height = 500, Children = { actual }, Background = new SolidColorBrush(Windows.UI.Colors.White) };
		await UITestHelper.Load(new StackPanel()
		{
			Spacing = 10,
			Orientation = Orientation.Horizontal,
			Children = { actualElm, expectedElm }
		});
		var expectedImg = await UITestHelper.ScreenShot(expectedElm);
		var actualImg = await UITestHelper.ScreenShot(actualElm);

		return (expectedImg, actualImg);
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/20634")]
	public async Task When_Idle()
	{
		var scrollBar = new ScrollBar { Orientation = Orientation.Vertical, Width = 30, Height = 200 };
		var sp = new StackPanel
		{
			Children =
			{
				new MenuFlyoutPresenter(),
				scrollBar
			}
		};

		scrollBar.LayoutUpdated += (_, _) =>
		{
			VisualStateManager.GoToState(scrollBar, "MouseIndicator", true);
			VisualStateManager.GoToState(scrollBar, "Expanded", true);
		};

		await UITestHelper.Load(sp);

		var frameRenderedCount = 0;
		Action OnFrameRendered = () => frameRenderedCount++;
		try
		{
			((CompositionTarget)sp.Visual.CompositionTarget)!.FrameRendered += OnFrameRendered;
			await Task.Delay(TimeSpan.FromSeconds(5));
			frameRenderedCount.Should().BeLessThan(100);
		}
		finally
		{
			((CompositionTarget)sp.Visual.CompositionTarget)!.FrameRendered -= OnFrameRendered;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/21095")]
	public async Task When_Nearby_Dark_Element_Should_Not_Bleed_Through()
	{
		// Create a layout with a dark element next to an acrylic element
		// The dark element should NOT bleed through the acrylic
		var darkElement = new Border
		{
			Width = 50,
			Height = 100,
			Background = new SolidColorBrush(Windows.UI.Colors.Black)
		};

		var acrylicElement = new Border
		{
			Width = 100,
			Height = 100,
			Background = new AcrylicBrush()
			{
				TintColor = Windows.UI.Colors.White,
				TintOpacity = 0.5,
				AlwaysUseFallback = false
			}
		};

		// Place the elements side by side - dark on left, acrylic on right
		var layout = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Background = new SolidColorBrush(Windows.UI.Colors.White),
			Children = { darkElement, acrylicElement }
		};

		await UITestHelper.Load(layout);

		// Take a screenshot of the acrylic element's left edge (area closest to dark element)
		var screenshot = await UITestHelper.ScreenShot(acrylicElement);

		// Sample the left edge of the acrylic (first column of pixels)
		// These should be light-colored (white-ish) because the acrylic is over white background
		// If the dark element bleeds through, these pixels would be darker
		var leftEdgePixel = screenshot.GetPixel(2, 50); // 2 pixels from left, middle height

		// The pixel should be predominantly light (white with some tint)
		// If bleeding occurs, the luminance would be much lower
		var luminance = (leftEdgePixel.R + leftEdgePixel.G + leftEdgePixel.B) / 3.0;
		luminance.Should().BeGreaterThan(150, "The left edge of acrylic should not be darkened by nearby dark element");
	}
}
#endif
