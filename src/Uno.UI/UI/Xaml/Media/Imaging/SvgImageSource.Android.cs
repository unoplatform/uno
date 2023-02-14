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
using Microsoft.UI.Xaml.Shapes;
using static Android.Provider.DocumentsContract;

namespace Microsoft.UI.Xaml.Media.Imaging;

partial class SvgImageSource
{
	private protected override bool IsSourceReady => true;

	private protected override bool TryOpenSourceAsync(CancellationToken ct, int? targetWidth, int? targetHeight, out Task<ImageData> asyncImage) =>
		TryOpenSvgImageData(ct, out asyncImage);

	private async Task<ImageData> GetSvgImageDataAsync(CancellationToken ct)
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

			if (!FilePath.IsNullOrEmpty())
			{
				using var fileStream = File.OpenRead(FilePath);
				return await ReadFromStreamAsync(fileStream, ct);
			}

			if (AbsoluteUri != null)
			{
				// The ContactsService returns the contact uri for compatibility with UniversalImageLoader - in order to obtain the corresponding photo we resolve using the service below.
				if (IsContactUri(AbsoluteUri))
				{
					var stream = ContactsContract.Contacts.OpenContactPhotoInputStream(ContextHelper.Current.ContentResolver, Android.Net.Uri.Parse(AbsoluteUri.OriginalString));
					return await ReadFromStreamAsync(stream, ct);
				}

				if (Downloader is not null)
				{
					var filePath = await Download(ct, AbsoluteUri);

					if (filePath == null)
					{
						return ImageData.Empty;
					}

					using var fileStream = File.OpenRead(FilePath);
					return await ReadFromStreamAsync(fileStream, ct);
				}
				else
				{
					using var imageStream = await OpenStreamFromUriAsync(UriSource, ct);
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
