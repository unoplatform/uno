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
using Microsoft.UI.Composition;
using Uno.Disposables;
using Uno.Foundation.Logging;

#if __SKIA__
using System.Runtime.InteropServices;
using SkiaSharp;
using System.Runtime.InteropServices.JavaScript;
#endif

namespace Uno.Helpers;

internal static partial class ImageSourceHelpers
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
	public static async Task<ImageData> ReadFromStreamAsCompositionSurface(Stream imageStream, CancellationToken ct, bool attemptLoadingWithBrowserCanvasApi = true)
	{
		var buffer = new byte[imageStream.Length - imageStream.Position];
		await imageStream.ReadExactlyAsync(buffer, 0, buffer.Length, ct);

		if (OperatingSystem.IsBrowser() && attemptLoadingWithBrowserCanvasApi)
		{
			var decodedBufferObject = await LoadFromArray(buffer);

			if (decodedBufferObject.GetPropertyAsString("error") is { } errorMessage)
			{
				typeof(ImageSourceHelpers).LogError()?.Error($"Failed to load image with the browser Canvas API. Falling back to SKCodec-based loading/decoding: {errorMessage}");
			}
			else
			{
				var width = decodedBufferObject.GetPropertyAsInt32("width");
				var height = decodedBufferObject.GetPropertyAsInt32("height");

				if (width == 0 || height == 0)
				{
					return ImageData.Empty;
				}

				var bytes = decodedBufferObject.GetPropertyAsByteArray("bytes");
				SKImage image;
				unsafe
				{
					var gcHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
					fixed (void* ptr = bytes)
					{
						try
						{
							image = SKImage.FromPixels(new SKPixmap(new SKImageInfo(width, height, SKColorType.Rgba8888), new IntPtr(ptr)), static (_, gcHandle) =>
							{
								((GCHandle)gcHandle).Free();
							}, gcHandle);
							if (image == null)
							{
								throw new InvalidOperationException($"{nameof(SKImage)}.{nameof(SKImage.FromPixels)} returned null.");
							}
							return ImageData.FromCompositionSurface(new SkiaCompositionSurface(image));
						}
						catch (Exception e)
						{
							gcHandle.Free();
							return ImageData.FromError(e);
						}
					}
				}
			}
		}

		var surface = new SkiaCompositionSurface();
		var result = surface.LoadFromStream(new MemoryStream(buffer));

		if (result.success)
		{
			return ImageData.FromCompositionSurface(surface);
		}
		else
		{
			var exception = new InvalidOperationException($"Image load failed ({result.nativeResult})");
			return ImageData.FromError(exception);
		}
	}

	// https://learn.microsoft.com/en-us/dotnet/core/compatibility/aspnet-core/6.0/byte-array-interop#receive-byte-array-in-javascript-from-net-1
	[JSImport($"globalThis.Uno.UI.Runtime.Skia.ImageLoader.loadFromArray")]
	private static partial Task<JSObject> LoadFromArray(byte[] array);
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
	public static async Task<ImageData> GetImageDataFromUriAsCompositionSurface(Uri uri, CancellationToken ct) =>
		await GetImageDataFromUri(uri,
			// add more animation formats here if needed
			(s, token) => ReadFromStreamAsCompositionSurface(s, token, !uri.AbsolutePath.EndsWith(".gif", StringComparison.InvariantCultureIgnoreCase)),
			ct);
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
