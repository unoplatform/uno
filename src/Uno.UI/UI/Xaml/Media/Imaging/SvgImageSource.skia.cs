using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Uno.UI.Xaml.Media;
using Windows.Application­Model;

namespace Microsoft.UI.Xaml.Media.Imaging;

partial class SvgImageSource
{
	private protected override bool TryOpenSourceAsync(CancellationToken ct, int? targetWidth, int? targetHeight, out Task<ImageData> asyncImage) =>
		TryOpenSvgImageData(ct, out asyncImage);

	private async Task<ImageData> GetSvgImageDataAsync(CancellationToken ct)
	{
		try
		{
			if (AbsoluteUri != null && AbsoluteUri.IsAbsoluteUri)
			{
				if (AbsoluteUri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) ||
					AbsoluteUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ||
					AbsoluteUri.IsFile)
				{
					using var imageStream = await OpenStreamFromUriAsync(UriSource, ct);
					return await ReadFromStreamAsync(imageStream, ct);
				}
				else if (AbsoluteUri.Scheme.Equals("ms-appx", StringComparison.OrdinalIgnoreCase))
				{
					var path = AbsoluteUri.PathAndQuery;
					path = GetApplicationPath(path);
					using var fileStream = File.OpenRead(path);
					return await ReadFromStreamAsync(fileStream, ct);
				}
				else if (AbsoluteUri.Scheme.Equals("ms-appdata", StringComparison.OrdinalIgnoreCase))
				{
					using var fileStream = File.OpenRead(FilePath);
					return await ReadFromStreamAsync(fileStream, ct);
				}
			}
			else if (_stream != null)
			{
				return await ReadFromStreamAsync(_stream.AsStream(), ct);
			}
		}
		catch (Exception e)
		{
			return ImageData.FromError(e);
		}

		return default;
	}

	private static string GetApplicationPath(string rawPath)
	{
		var originalLocalPath =
			Path.Combine(Package.Current.Installed­Location.Path,
				 rawPath.TrimStart('/').Replace('/', global::System.IO.Path.DirectorySeparatorChar)
			);

		return originalLocalPath;
	}
}
