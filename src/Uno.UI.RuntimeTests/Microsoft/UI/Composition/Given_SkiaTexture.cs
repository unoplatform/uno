#nullable enable

#if UNO_DRAWING_SKIA
using System;
using Microsoft.UI.Composition;
using SkiaSharp;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

[TestClass]
[RunsOnUIThread]
public class Given_SkiaTexture
{
	private const int Size = 4;
	private static readonly SKColor Red = new(0xFF, 0, 0);

	// The texture draws the image's own pixels through an SKImage of its own, so that its GPU upload, keyed on that
	// image, goes with the texture.
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25547")]
	public void When_Created_From_A_SkiaImage_Then_It_Wraps_Its_Pixels()
	{
		using var factory = new SkiaDrawingFactory();
		using var image = CreateRedImage();
		using var texture = (SkiaTexture)factory.CreateTexture(image);
		using var imagePixels = image.Image.PeekPixels();
		using var texturePixels = texture.Image.PeekPixels();

		Assert.AreEqual(imagePixels.GetPixels(), texturePixels.GetPixels());
		Assert.AreNotEqual(image.Image.UniqueId, texture.Image.UniqueId);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25547")]
	public void When_Image_Is_Disposed_First_Then_The_Texture_Still_Draws()
	{
		using var factory = new SkiaDrawingFactory();
		var image = CreateRedImage();
		var texture = factory.CreateTexture(image);

		image.Dispose();
		Assert.AreEqual(Red, Draw(factory, s => s.DrawImage(texture, 0, 0)));

		texture.Dispose();
		Assert.AreEqual(IntPtr.Zero, image.Image.Handle, "The pixels should be freed with the last reference.");
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25547")]
	public void When_Texture_Is_Disposed_First_Then_The_Image_Stays_Alive()
	{
		using var factory = new SkiaDrawingFactory();
		var image = CreateRedImage();

		factory.CreateTexture(image).Dispose();
		Assert.AreNotEqual(IntPtr.Zero, image.Image.Handle, "Disposing the texture should leave the image alive.");

		image.Dispose();
		Assert.AreEqual(IntPtr.Zero, image.Image.Handle, "The pixels should be freed with the last reference.");
	}

	// The order a surface takes when it switches frames or devices: the texture goes, the image stays and is textured
	// again.
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25547")]
	public void When_Surface_Textures_Its_Image_Again_Then_The_Image_Still_Draws()
	{
		using var first = new SkiaDrawingFactory();
		using var second = new SkiaDrawingFactory();
		var image = CreateRedImage();
		var surface = new CompositionImageSurface(image);

		Assert.IsNotNull(surface.GetTexture(first));
		var texture = surface.GetTexture(second);
		Assert.AreNotEqual(IntPtr.Zero, image.Image.Handle, "Disposing the first texture should leave the image it shares alive.");
		Assert.IsNotNull(texture);
		Assert.AreEqual(Red, Draw(second, s => s.DrawImage(texture, 0, 0)));

		surface.Dispose();
		Assert.AreEqual(IntPtr.Zero, image.Image.Handle, "The pixels should be freed with the surface.");
	}

	// A frame recorded before the texture and its image went away is still replayed.
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25547")]
	public void When_A_Recording_Outlives_The_Texture_And_Image_Then_It_Still_Draws()
	{
		using var factory = new SkiaDrawingFactory();
		var image = CreateRedImage();
		var texture = factory.CreateTexture(image);
		var recorder = factory.CreateRecording();
		recorder.DrawImage(texture, 0, 0);
		var record = recorder.Finish();

		texture.Dispose();
		image.Dispose();
		Assert.AreNotEqual(IntPtr.Zero, image.Image.Handle, "The recording should keep the pixels alive.");
		Assert.AreEqual(Red, Draw(factory, record.Replay));

		record.Dispose();
		Assert.AreEqual(IntPtr.Zero, image.Image.Handle, "The pixels should be freed with the recording.");
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25547")]
	public void When_CopyPixels_Destination_Is_Too_Short_Then_It_Throws()
	{
		using var image = CreateRedImage();
		var buffer = new byte[Size * Size * 4];

		Assert.ThrowsExactly<ArgumentException>(() => image.CopyPixels(buffer.AsSpan(0, buffer.Length - 4)));
		Assert.AreEqual(0, buffer[^1], "Nothing should be written past the destination.");
	}

	private static SkiaImage CreateRedImage()
	{
		var info = new SKImageInfo(Size, Size, SKColorType.Bgra8888, SKAlphaType.Premul);
		using var bitmap = new SKBitmap(info);
		bitmap.Erase(Red);
		return new SkiaImage(SKImage.FromBitmap(bitmap));
	}

	private static SKColor Draw(SkiaDrawingFactory factory, Action<IDrawingSession> draw)
	{
		using var surface = SKSurface.Create(new SKImageInfo(Size, Size, SKColorType.Bgra8888, SKAlphaType.Premul));
		surface.Canvas.Clear(SKColors.Transparent);
		draw(new SkiaDrawingSession(surface.Canvas, factory));
		using var pixmap = surface.PeekPixels();
		return pixmap.GetPixelColor(Size / 2, Size / 2);
	}
}
#endif
