using System;
using System.Threading.Tasks;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage;
using System.IO;
using Uno.UITest.Helpers.Queries;

#if __SKIA__
using SkiaSharp;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media;

[TestClass]
public class Given_AcrylicBrush
{
#if __SKIA__ && HAS_UNO
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
		using var filter = SKImageFilter.CreateBlur(30.0f, 30.0f, SKImageFilter.CreateImage(image));

		paint.IsAntialias = true;
		paint.FilterQuality = SKFilterQuality.High;
		paint.ImageFilter = filter;
		canvas.DrawPaint(paint);

		using var snapshot = surface.Snapshot();

		var compositor = Window.Current.Compositor;
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
		await UITestHelper.Load(expectedElm);
		var expectedImg = await UITestHelper.ScreenShot(expectedElm);

		var actualElm = new Grid { Width = 500, Height = 500, Children = { actual }, Background = new SolidColorBrush(Windows.UI.Colors.White) };
		await UITestHelper.Load(actualElm);
		var actualImg = await UITestHelper.ScreenShot(actualElm);

		return (expectedImg, actualImg);
	}
#endif
}
