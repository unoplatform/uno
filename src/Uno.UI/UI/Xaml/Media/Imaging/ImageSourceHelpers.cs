#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno.UI.Xaml.Media;
using Windows.ApplicationModel;
using Windows.Storage;

namespace Uno.Helpers;

internal static class ImageSourceHelpers
{
	private static HttpClient? _httpClient;

	public static async Task<ImageData> ReadFromStreamAsBytesAsync(Stream stream, CancellationToken ct)
	{
		if (stream.CanSeek && stream.Position != 0)
		{
			stream.Position = 0;
		}

		var memoryStream = new MemoryStream();
		await stream.CopyToAsync(memoryStream, 81920, ct);
		var data = memoryStream.ToArray();
		return ImageData.FromBytes(data);
	}

#if __SKIA__
	public static Task<ImageData> ReadFromStreamAsCompositionSurface(Stream imageStream, CancellationToken ct)
	{
		var surface = new Windows.UI.Composition.SkiaCompositionSurface();
		var result = surface.LoadFromStream(imageStream);

		if (result.success)
		{
			return Task.FromResult(ImageData.FromCompositionSurface(surface));
		}
		else
		{
			var exception = new InvalidOperationException($"Image load failed ({result.nativeResult})");
			return Task.FromResult(ImageData.FromError(exception));
		}
	}
#endif

	public static async Task<Stream> OpenStreamFromUriAsync(Uri uri, CancellationToken ct)
	{
		if (uri.IsFile)
		{
			return File.OpenRead(uri.LocalPath);
		}

		_httpClient ??= new HttpClient();
		var response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseContentRead, ct);
		return await response.Content.ReadAsStreamAsync();
	}

	public static async Task<ImageData> GetImageDataFromUriAsBytes(Uri uri, CancellationToken ct)
		=> await GetImageDataFromUri(uri, ReadFromStreamAsBytesAsync, ct);

#if __SKIA__
	public static async Task<ImageData> GetImageDataFromUriAsCompositionSurface(Uri uri, CancellationToken ct)
		=> await GetImageDataFromUri(uri, ReadFromStreamAsCompositionSurface, ct);
#endif

	public static async Task<ImageData> GetImageDataFromUri(Uri uri, Func<Stream, CancellationToken, Task<ImageData>> imageDataCreator, CancellationToken ct)
	{
		if (uri != null && uri.IsAbsoluteUri)
		{
			if (uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) ||
				uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ||
				uri.IsFile)
			{
				using var imageStream = await OpenStreamFromUriAsync(uri, ct);
				return await imageDataCreator(imageStream, ct);
			}
			else if (uri.Scheme.Equals("ms-appx", StringComparison.OrdinalIgnoreCase))
			{
				var file = await StorageFile.GetFileFromApplicationUriAsync(uri);
				using var fileStream = await file.OpenStreamForReadAsync();
				return await imageDataCreator(fileStream, ct);
			}
			else if (uri.Scheme.Equals("ms-appdata", StringComparison.OrdinalIgnoreCase))
			{
				using var fileStream = File.OpenRead(AppDataUriEvaluator.ToPath(uri));
				return await imageDataCreator(fileStream, ct);
			}
		}

		return default;
	}
}
