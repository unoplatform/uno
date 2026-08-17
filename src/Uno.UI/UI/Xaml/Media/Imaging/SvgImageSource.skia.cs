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
				_ = task.Result;

				// The registered ISvgRenderer retains the parsed vector; hand it to a live composition surface that
				// replays it each frame at the display size — resolution-independent (crisp at any scale), no
				// intermediate rasterization. Consumed like any other image surface (e.g. by Image's surface brush).
				if (_svgDocument is { } document)
				{
					return ImageData.FromCompositionSurface(new CompositionSvgSurface(document));
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
