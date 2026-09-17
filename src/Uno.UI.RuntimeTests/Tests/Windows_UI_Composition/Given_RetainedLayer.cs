#if __SKIA__
using SkiaSharp;
using Uno.UI.Helpers;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

[TestClass]
public class Given_RetainedLayer
{
	// Damage-region renderers draw into a retained layer and blit it onto a swapchain surface that is
	// recycled, so it still holds an older frame. A system backdrop makes the tree transparent and the
	// layer then carries alpha: the blit has to replace those stale pixels, not blend over them.
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24495")]
	public void When_Layer_Has_Alpha_Then_Present_Replaces_Stale_Swapchain_Pixels()
	{
#if __SKIA__
		var info = new SKImageInfo(4, 4, SKColorType.Rgba8888, SKAlphaType.Premul);

		using var layer = new RetainedLayer();
		var layerSurface = layer.EnsureSurface(null, info.Width, info.Height, SKColors.Transparent);
		using (var paint = new SKPaint { Color = new SKColor(0, 255, 0, 128) })
		{
			layerSurface.Canvas.DrawRect(new SKRect(1, 1, 3, 3), paint);
		}
		layerSurface.Canvas.Flush();

		// A recycled swapchain surface still showing an older, fully opaque frame.
		using var swapchain = SKSurface.Create(info);
		swapchain.Canvas.Clear(SKColors.Red);

		layer.Present(swapchain);

		using var expected = ReadPixels(layerSurface, info);
		using var actual = ReadPixels(swapchain, info);

		for (var y = 0; y < info.Height; y++)
		{
			for (var x = 0; x < info.Width; x++)
			{
				Assert.AreEqual(expected.GetPixel(x, y), actual.GetPixel(x, y), $"Pixel ({x},{y}) still shows the stale swapchain content.");
			}
		}
#endif
	}

#if __SKIA__
	private static SKBitmap ReadPixels(SKSurface surface, SKImageInfo info)
	{
		var bitmap = new SKBitmap(info);
		Assert.IsTrue(surface.ReadPixels(info, bitmap.GetPixels(), bitmap.RowBytes, 0, 0));
		return bitmap;
	}
#endif
}
