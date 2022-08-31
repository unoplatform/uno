using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Provider;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.Dispatching;
using Uno.UI.Xaml.Media;
using Uno.UI.Xaml.Media.Imaging.Svg;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Shapes;
using static Android.Provider.DocumentsContract;

namespace Windows.UI.Xaml.Media.Imaging;

partial class SvgImageSource
{
	private protected override bool IsSourceReady => true;

	private protected override bool TryOpenSourceAsync(CancellationToken ct, int? targetWidth, int? targetHeight, out Task<ImageData> asyncImage)
	{
		asyncImage = TryOpenSourceAsync(ct);
		return true;
	}

	private async Task<ImageData> TryOpenSourceAsync(CancellationToken ct)
	{
		var imageData = await TryReadImageDataAsync(ct);
		if (imageData.Kind == ImageDataKind.ByteArray)
		{
			if (_svgProvider is null)
			{
				this.Log().LogWarning("Uno.UI.Svg package needs to be installed to use SVG on this platform.");
			}
			_svgProvider?.NotifySourceOpened(imageData.ByteArray);
		}
		return imageData;
	}

	private async Task<ImageData> TryReadImageDataAsync(CancellationToken ct)
	{
		try
		{
			if (ResourceId.HasValue)
			{
				return await FetchSvgResource(ct, ResourceId.Value);
			}

			if (ResourceString is not null)
			{
				return await FetchSvgAssetAsync(ct, ResourceString);
			}

			if (Stream != null)
			{
				return await ReadFromStreamAsync(Stream, ct);
			}

			if (FilePath.HasValue())
			{
				using var fileStream = File.OpenRead(FilePath);
				return await ReadFromStreamAsync(fileStream, ct);
			}

			if (WebUri != null)
			{
				// The ContactsService returns the contact uri for compatibility with UniversalImageLoader - in order to obtain the corresponding photo we resolve using the service below.
				if (IsContactUri(WebUri))
				{
					var stream = ContactsContract.Contacts.OpenContactPhotoInputStream(ContextHelper.Current.ContentResolver, Android.Net.Uri.Parse(WebUri.OriginalString));
					return await ReadFromStreamAsync(stream, ct);
				}

				if (Downloader is not null)
				{
					var filePath = await Download(ct, WebUri);

					if (filePath == null)
					{
						return ImageData.Empty;
					}

					using var fileStream = File.OpenRead(FilePath);
					return await ReadFromStreamAsync(fileStream, ct);
				}
				else
				{
					var client = new HttpClient();
					var response = await client.GetAsync(UriSource, HttpCompletionOption.ResponseContentRead, ct);
					using var imageStream = await response.Content.ReadAsStreamAsync();
					return await ReadFromStreamAsync(imageStream, ct);
				}
			}

			return ImageData.Empty;
		}
		catch (Exception ex)
		{
			return ImageData.FromError(ex);
		}
	}

	private async Task<ImageData> FetchSvgResource(CancellationToken ct, int resourceId)
	{
		var rawResourceStream = ContextHelper.Current.Resources.OpenRawResource(resourceId);
		return await ReadFromStreamAsync(rawResourceStream, ct);
	}

	private async Task<ImageData> FetchSvgAssetAsync(CancellationToken ct, string assetPath)
	{
		AssetManager assets = ContextHelper.Current.Assets;
		using var stream = assets.Open(assetPath);
		return await ReadFromStreamAsync(stream, ct);
	}
}
