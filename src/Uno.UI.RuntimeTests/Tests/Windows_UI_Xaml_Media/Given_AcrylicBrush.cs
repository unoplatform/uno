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
using Private.Infrastructure;
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
		using var filter = SKImageFilter.CreateBlur(30.0f, 30.0f, SKShaderTileMode.Clamp, SKImageFilter.CreateImage(image, new SKSamplingOptions(SKCubicResampler.CatmullRom)));

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
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/21231")]
	public async Task When_Backdrop_DoesNotSampleOutsideElementBounds()
	{
		// The inner border has a white background, and the acrylic brush has a white tint.
		// In WinUI, the acrylic result is pure white because the blur only samples from
		// the element's own backdrop area. In the buggy Skia implementation, the blur
		// samples from outside (picking up the black outer border), causing a grey tint.
		//
		// We screenshot the outerBorder so the entire visual context (black border,
		// white area, acrylic) is composited together. Then we compare a corner pixel
		// of the inner area (most affected by blur bleeding) against the center pixel.
		// With correct blur clamping, the entire inner area should be uniformly white.
		var acrylicBorder = new Border();
		acrylicBorder.Background = new AcrylicBrush() { TintColor = Microsoft.UI.Colors.White };

		var whiteBorder = new Border()
		{
			Background = new SolidColorBrush(Microsoft.UI.Colors.White),
			Width = 80,
			Height = 80,
			Child = acrylicBorder
		};

		var outerBorder = new Border()
		{
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			BorderThickness = new Thickness(50),
			BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Black),
			Child = whiteBorder
		};

		await UITestHelper.Load(outerBorder);
		await TestServices.WindowHelper.WaitForIdle();

		// Screenshot the outerBorder so the black border is part of the captured visual.
		var screenshot = await UITestHelper.ScreenShot(outerBorder);

		// The inner white+acrylic area is at (50,50) to (130,130) in the outer border.
		// Check that the corner of the inner area (closest to the black border) has
		// the same color as the center. Without clamping, the corners are visibly darker.
		var centerColor = screenshot.GetPixel(90, 90);
		var cornerColor = screenshot.GetPixel(52, 52);

		// The center and corner should be nearly identical (within noise tolerance).
		// Without the blur clamp fix, the corner is significantly darker than center
		// because the blur bleeds in the black border color.
		var diffR = Math.Abs(centerColor.R - cornerColor.R);
		var diffG = Math.Abs(centerColor.G - cornerColor.G);
		var diffB = Math.Abs(centerColor.B - cornerColor.B);
		var maxDiff = Math.Max(diffR, Math.Max(diffG, diffB));

		Assert.IsTrue(maxDiff <= 10,
			$"Corner pixel ({cornerColor}) differs too much from center pixel ({centerColor}). " +
			$"Max channel diff: {maxDiff}. This indicates blur is sampling outside the backdrop area.");
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
}
#endif
