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
				throw new InvalidOperationException("Uri is not using the ms-appx scheme");
			}

			var path = Uri.UnescapeDataString(uri.PathAndQuery);

			if (uri.Host is { Length: > 0 } host)
			{
				path = host + "/" + path.TrimStart("/");
			}

			return await StorageFile.GetFileFromPathAsync(await AssetsManager.DownloadAsset(ct, path));
		}
	}
}
