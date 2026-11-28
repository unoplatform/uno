#if __SKIA__
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Uno.UI.Composition.Drawing;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

/// <summary>
/// Image surfaces use the shared compositor rather than creating a Compositor for every decoded image.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_CompositionImageSurface
{
	[TestMethod]
	public async Task When_Surface_Created_Then_Uses_Shared_Compositor()
	{
		var onUiThread = new CompositionImageSurface();
		var offThread = await Task.Run(() => new CompositionImageSurface());

		Assert.AreSame(Compositor.GetSharedCompositor(), onUiThread.Compositor);
		Assert.AreSame(Compositor.GetSharedCompositor(), offThread.Compositor);
	}

	[TestMethod]
	public async Task When_Surface_Created_From_Image_Then_Uses_Shared_Compositor()
	{
		var pixels = new byte[4 * 4 * 4];

		using var onUiThread = new CompositionImageSurface(ImageEncoderDecoder.Current.CreateImage(4, 4, pixels));
		using var offThread = await Task.Run(() => new CompositionImageSurface(ImageEncoderDecoder.Current.CreateImage(4, 4, pixels)));

		Assert.AreSame(Compositor.GetSharedCompositor(), onUiThread.Compositor);
		Assert.AreSame(Compositor.GetSharedCompositor(), offThread.Compositor);
	}

	[TestMethod]
	public async Task When_BitmapImage_Decoded_Then_Surface_Uses_Shared_Compositor()
	{
		var bitmap = new BitmapImage();
		var image = new Image { Width = 20, Height = 20, Source = bitmap };
		await UITestHelper.Load(image);

		await bitmap.SetSourceAsync(new MemoryStream(TestPngEncoder.CreateSolidPng(4, 4, Colors.Red)));

		var surface = await WaitForSurface(bitmap);
		Assert.AreSame(Compositor.GetSharedCompositor(), surface.Compositor);
	}

	[TestMethod]
	public async Task When_SvgImageSource_Loaded_Then_Surface_Uses_Shared_Compositor()
	{
		const string svg = "<svg xmlns='http://www.w3.org/2000/svg' width='20' height='20'><rect width='20' height='20' fill='red'/></svg>";
		var source = new SvgImageSource();
		var image = new Image { Width = 20, Height = 20, Source = source };
		await UITestHelper.Load(image);

		using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(svg)).AsRandomAccessStream())
		{
			Assert.AreEqual(SvgImageSourceLoadStatus.Success, await source.SetSourceAsync(stream));
		}

		var surface = await WaitForSurface(source);
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

			await bitmap.SetSourceAsync(new MemoryStream(TestPngEncoder.CreateSolidPng(4, 4, Colors.Red)));
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

	// The surface a displayed source delivers to its subscribers.
	private static async Task<CompositionObject> WaitForSurface(ImageSource source)
	{
		CompositionObject surface = null;
		await WindowHelper.WaitFor(() =>
		{
			source.Subscribe(data => surface = data.CompositionSurface as CompositionObject).Dispose();
			return surface is not null;
		}, message: "The source delivered no surface");

		return surface;
	}
}
#endif
