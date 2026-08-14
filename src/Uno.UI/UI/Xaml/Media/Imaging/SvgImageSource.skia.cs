using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Uno.Helpers;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Media;
using Windows.Application­Model;
using Windows.Foundation;
using Microsoft.UI.Composition;
using Uno.UI.Composition.Drawing;

namespace Microsoft.UI.Xaml.Media.Imaging;

partial class SvgImageSource
{
	// The retained parsed SVG (from the registered ISvgRenderer); set when the markup parses.
	private ISvgDocument _svgDocument;

	private protected override bool TryOpenSourceAsync(CancellationToken ct, int? targetWidth, int? targetHeight, out Task<ImageData> asyncImage)
	{
		if (TryOpenSvgImageData(ct, out var imageTask))
		{
			asyncImage = imageTask.ContinueWith(task =>
			{
				var imageData = task.Result;

				// Primary path: render through the managed, backend-neutral SVG engine (no Skia dependency).
				if (_svgDocument is { } document)
				{
					var width = targetWidth is > 0 ? targetWidth.Value
						: RasterizePixelWidth > 0 ? (int)Math.Ceiling(RasterizePixelWidth)
						: (int)Math.Ceiling(document.SourceSize.Width);
					var height = targetHeight is > 0 ? targetHeight.Value
						: RasterizePixelHeight > 0 ? (int)Math.Ceiling(RasterizePixelHeight)
						: (int)Math.Ceiling(document.SourceSize.Height);
					var w = Math.Max(1, width);
					var h = Math.Max(1, height);

					// The engine retains the parsed vector; we rasterize it at the display size here. This layer owns
					// the backend, so RenderOffscreen lives on the caller side of the seam — the engine only draws.
					var svgImage = DrawingFactory.Current.RenderOffscreen(w, h, s => document.Render(s, new Size(w, h)));
					return ImageData.FromCompositionSurface(new CompositionImageSurface(svgImage));
				}

				// Fallback: the optional add-in (e.g. features the managed engine doesn't yet cover). The add-in
				// rasterizes internally and returns a neutral image, so no Skia type crosses this seam.
				if (imageData is { Kind: ImageDataKind.ByteArray, ByteArray: not null } && _svgProvider is { } provider)
				{
					var sourceSize = provider.SourceSize;
					var width = targetWidth is > 0 ? targetWidth.Value
						: RasterizePixelWidth > 0 ? (int)Math.Ceiling(RasterizePixelWidth)
						: (int)Math.Ceiling(sourceSize.Width);
					var height = targetHeight is > 0 ? targetHeight.Value
						: RasterizePixelHeight > 0 ? (int)Math.Ceiling(RasterizePixelHeight)
						: (int)Math.Ceiling(sourceSize.Height);

					if (provider.RenderToImage(Math.Max(1, width), Math.Max(1, height)) is IImage svgImage)
					{
						return ImageData.FromCompositionSurface(new CompositionImageSurface(svgImage));
					}

					if (this.Log().IsEnabled(LogLevel.Warning))
					{
						this.Log().Warn($"SVG provider returned no image for {width}x{height}; rendering empty.");
					}
				}

				return ImageData.Empty;
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
