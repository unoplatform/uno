#if __SKIA__
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using SkiaSharp;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

/// <summary>
/// Image surfaces use the shared compositor rather than creating a Compositor for every decoded image.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_SkiaCompositionSurface
{
	[TestMethod]
	public async Task When_Surface_Created_Then_Uses_Shared_Compositor()
	{
		var onUiThread = new SkiaCompositionSurface();
		var offThread = await Task.Run(() => new SkiaCompositionSurface());

		Assert.AreSame(Compositor.GetSharedCompositor(), onUiThread.Compositor);
		Assert.AreSame(Compositor.GetSharedCompositor(), offThread.Compositor);
	}

	[TestMethod]
	public async Task When_Surface_Created_From_Image_Then_Uses_Shared_Compositor()
	{
		using var bitmap = new SKBitmap(4, 4);
		bitmap.Erase(SKColors.Red);

		var onUiThread = new SkiaCompositionSurface(SKImage.FromBitmap(bitmap));
		var offThread = await Task.Run(() => new SkiaCompositionSurface(SKImage.FromBitmap(bitmap)));

		Assert.AreSame(Compositor.GetSharedCompositor(), onUiThread.Compositor);
		Assert.AreSame(Compositor.GetSharedCompositor(), offThread.Compositor);
	}

	[TestMethod]
	public async Task When_BitmapImage_Decoded_Then_Surface_Uses_Shared_Compositor()
	{
		var bitmap = new BitmapImage();
		var image = new Image { Width = 20, Height = 20, Source = bitmap };
		await UITestHelper.Load(image);

		await bitmap.SetSourceAsync(new MemoryStream(CreatePng()));

		var surface = await WaitForSurface(bitmap);
		Assert.AreSame(Compositor.GetSharedCompositor(), surface.Compositor);
	}

	[TestMethod]
	public async Task When_Fail_When_Not_Implemented_Then_BitmapImage_Opens()
	{
		var wasFailing = FeatureConfiguration.ApiInformation.IsFailWhenNotImplemented;
		FeatureConfiguration.ApiInformation.IsFailWhenNotImplemented = true;
		try
		{
			var bitmap = new BitmapImage();
			var opened = new TaskCompletionSource<string>();
			bitmap.ImageOpened += (_, _) => opened.TrySetResult(null);
			bitmap.ImageFailed += (_, e) => opened.TrySetResult(e.ErrorMessage);

			await bitmap.SetSourceAsync(new MemoryStream(CreatePng()));
			var image = new Image { Width = 20, Height = 20, Source = bitmap };
			await UITestHelper.Load(image);

			var error = await opened.Task.WaitAsync(TimeSpan.FromSeconds(10));
			Assert.IsNull(error, $"The image failed to open: {error}");
		}
		finally
		{
			FeatureConfiguration.ApiInformation.IsFailWhenNotImplemented = wasFailing;
		}
	}

	private static byte[] CreatePng()
	{
		using var bitmap = new SKBitmap(4, 4);
		bitmap.Erase(SKColors.Red);
		using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
		return data.ToArray();
	}

	// The surface a displayed source delivers to its subscribers.
	private static async Task<SkiaCompositionSurface> WaitForSurface(ImageSource source)
	{
		SkiaCompositionSurface surface = null;
		await WindowHelper.WaitFor(() =>
		{
			source.Subscribe(data => surface = data.CompositionSurface).Dispose();
			return surface is not null;
		}, message: "The source delivered no surface");

		return surface;
	}
}
#endif
