#nullable enable
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
using Uno.Helpers;
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

	private protected override bool TryOpenSourceAsync(CancellationToken ct, int? targetWidth, int? targetHeight, out Task<ImageData> asyncImage) =>
		TryOpenSvgImageData(ct, out asyncImage);

	private async Task<ImageData> GetSvgImageDataAsync(CancellationToken ct)
	{
		try
		{
			if (Stream is not null)
			{
				return await ImageSourceHelpers.ReadFromStreamAsBytesAsync(Stream, ct);
			}

			if (!FilePath.IsNullOrEmpty())
			{
				using var fileStream = File.OpenRead(FilePath);
				return await ImageSourceHelpers.ReadFromStreamAsBytesAsync(fileStream, ct);
			}

			if (AbsoluteUri is not null)
			{
				// The ContactsService returns the contact uri for compatibility with UniversalImageLoader - in order to obtain the corresponding photo we resolve using the service below.
				if (IsContactUri(AbsoluteUri))
				{
					if (ContactsContract.Contacts.OpenContactPhotoInputStream(ContextHelper.Current.ContentResolver, Android.Net.Uri.Parse(AbsoluteUri.OriginalString)) is not { } stream)
					{
						return ImageData.Empty;
					}

					return await ImageSourceHelpers.ReadFromStreamAsBytesAsync(stream, ct);
				}

				if (AbsoluteUri.IsLocalResource())
				{
					var file = await StorageFile.GetFileFromApplicationUriAsync(AbsoluteUri);

					using var fileStream = await file.OpenAsync(FileAccessMode.Read);
					using var ioStream = fileStream.AsStream();
					return await ImageSourceHelpers.ReadFromStreamAsBytesAsync(ioStream, ct);
				}

				if (Downloader is not null)
				{
					var filePath = await Download(ct, AbsoluteUri);

					if (filePath is null)
					{
						return ImageData.Empty;
					}

					using var fileStream = File.OpenRead(filePath.LocalPath);
					return await ImageSourceHelpers.ReadFromStreamAsBytesAsync(fileStream, ct);
				}
				else
				{
					using var imageStream = await ImageSourceHelpers.OpenStreamFromUriAsync(UriSource, ct);
					return await ImageSourceHelpers.ReadFromStreamAsBytesAsync(imageStream, ct);
				}
			}

			return ImageData.Empty;
		}
		catch (Exception ex)
		{
			return ImageData.FromError(ex);
		}

	}
}
