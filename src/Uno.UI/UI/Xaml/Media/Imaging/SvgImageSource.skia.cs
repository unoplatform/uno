using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Uno.Helpers;
using Uno.UI.Xaml.Media;
using Windows.Application­Model;
using Microsoft.UI.Composition;
using SkiaSharp;
using System.Reflection;
using Uno.UI.Composition.Drawing;

namespace Microsoft.UI.Xaml.Media.Imaging;

partial class SvgImageSource
{
	private static MethodInfo _fromPictureMethod;

	// The SkiaSharp-free managed SVG engine; set when the markup parses (the primary, add-in-free path).
	private ManagedSvg _managedSvg;

	private protected unsafe override bool TryOpenSourceAsync(CancellationToken ct, int? targetWidth, int? targetHeight, out Task<ImageData> asyncImage)
	{
		if (TryOpenSvgImageData(ct, out var imageTask))
		{
			asyncImage = imageTask.ContinueWith(task =>
			{
				var imageData = task.Result;

				// Primary path: render through the managed, backend-neutral SVG engine (no Skia dependency).
				if (_managedSvg is { } managed)
				{
					var width = targetWidth is > 0 ? targetWidth.Value
						: RasterizePixelWidth > 0 ? (int)Math.Ceiling(RasterizePixelWidth)
						: (int)Math.Ceiling(managed.SourceSize.Width);
					var height = targetHeight is > 0 ? targetHeight.Value
						: RasterizePixelHeight > 0 ? (int)Math.Ceiling(RasterizePixelHeight)
						: (int)Math.Ceiling(managed.SourceSize.Height);

					var svgImage = managed.Render(Math.Max(1, width), Math.Max(1, height));
					return ImageData.FromCompositionSurface(new CompositionImageSurface(svgImage));
				}

				// Fallback: the optional Skia-based add-in (e.g. features the managed engine doesn't yet cover).
				if (imageData is { Kind: ImageDataKind.ByteArray, ByteArray: not null } &&
					_svgProvider?.TryGetLoadedDataAsPictureAsync() is SKPicture picture)
				{
					var sourceSize = _svgProvider.SourceSize;

					_fromPictureMethod ??= typeof(SKImage).GetMethod(
						"FromPicture",
						BindingFlags.NonPublic | BindingFlags.Static,
						new[] {
							typeof(SKPicture),
							typeof(SKSizeI),
							typeof(SKMatrix).MakePointerType(),
							typeof(SKPaint),
							typeof(bool),
							typeof(SKColorSpace),
							typeof(SKSurfaceProperties) });

					if (_fromPictureMethod is null)
					{
						throw new InvalidOperationException("Unable to find the 'FromPicture' method on SKImage");
					}

					var matrix = SKMatrix.Identity;

					var skImage = (SKImage)_fromPictureMethod.Invoke(
						null,
						[
							picture,
							new SKSizeI((int)sourceSize.Width, (int)sourceSize.Height),
							Pointer.Box(&matrix, typeof(SKMatrix*)),
							new SKPaint(),
							false,
							SKColorSpace.CreateSrgb(),
							new SKSurfaceProperties(SKPixelGeometry.Unknown)
					]);

					return ImageData.FromCompositionSurface(new(new global::Uno.UI.Composition.Drawing.SkiaImage(skImage)));
				}
				else
				{
					return ImageData.Empty;
				}
			}, ct);
			return true;
		}
		else
		{
			asyncImage = Task.FromResult(ImageData.Empty);
			return false;
		}
	}

	private async Task<ImageData> GetSvgImageDataAsync(CancellationToken ct)
	{
		try
		{
			ImageData imageData = ImageData.Empty;

			if (AbsoluteUri is { } uri)
			{
				imageData = await ImageSourceHelpers.GetImageDataFromUriAsBytes(uri, ct);
			}

			if (!imageData.HasData && _stream is not null)
			{
				imageData = await ImageSourceHelpers.ReadFromStreamAsBytesAsync(_stream.AsStream(), ct);
			}

			return imageData;
		}
		catch (Exception e)
		{
			return ImageData.FromError(e);
		}
	}
}
