using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Uno.UI.Xaml.Media;

namespace Windows.UI.Xaml.Media.Imaging;

partial class SvgImageSource
{
	private protected override bool TryOpenSourceAsync(CancellationToken ct, int? targetWidth, int? targetHeight, out Task<ImageData> asyncImage)
	{
		asyncImage = TryOpenSourceAsync(ct);
		return true;
	}

	private async Task<ImageData> TryOpenSourceAsync(CancellationToken ct)
	{
		try
		{
			if (UriSource != null)
			{
				if (UriSource.Scheme == "http" || UriSource.Scheme == "https")
				{
					var client = new HttpClient();
					var response = await client.GetAsync(UriSource, HttpCompletionOption.ResponseContentRead, ct);
					using var imageStream = await response.Content.ReadAsStreamAsync();
					return await ReadFromStreamAsync(imageStream, ct);
				}
				else if (UriSource.Scheme == "ms-appx")
				{
					var path = UriSource.PathAndQuery;
					path = GetApplicationPath(path);
					using var fileStream = File.OpenRead(path);
					return await ReadFromStreamAsync(fileStream, ct);
				}
				else if (UriSource.Scheme == "ms-appdata")
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
			Path.Combine(Windows.Application­Model.Package.Current.Installed­Location.Path,
				 rawPath.TrimStart('/').Replace('/', global::System.IO.Path.DirectorySeparatorChar)
			);

		return originalLocalPath;
	}
}
