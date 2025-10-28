using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;
using Windows.Foundation;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.Storage.Helpers;

namespace Windows.Storage
{
	partial class StorageFile
	{
		private static async Task<StorageFile> GetFileFromApplicationUri(CancellationToken ct, Uri uri)
		{
			if (uri.Scheme != "ms-appx")
			{
				// ms-appdata is handled by the caller.
				throw new InvalidOperationException("Uri is not using the ms-appx or ms-appdata scheme");
			}

			var path = Uri.UnescapeDataString(uri.PathAndQuery);

			return GetFileFromPath(await AssetsManager.DownloadAsset(ct, path));
		}
	}
}
