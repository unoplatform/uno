using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Uno.Helpers;
using Uno.UI.Xaml.Media;
using Windows.Application­Model;

namespace Windows.UI.Xaml.Media.Imaging;

partial class SvgImageSource
{
	private protected override bool TryOpenSourceAsync(CancellationToken ct, int? targetWidth, int? targetHeight, out Task<ImageData> asyncImage) =>
		TryOpenSvgImageData(ct, out asyncImage);

	private async Task<ImageData> GetSvgImageDataAsync(CancellationToken ct)
	{
		try
		{
			ImageData imageData = await ImageSourceHelpers.GetImageDataFromUriAsBytes(AbsoluteUri, ct);
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
