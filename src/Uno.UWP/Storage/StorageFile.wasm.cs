#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

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
			if(uri.Scheme != "ms-appx")
			{
				throw new InvalidOperationException("Uri is not using the ms-appx scheme");
			}

			var path = uri.PathAndQuery;

			return await StorageFile.GetFileFromPathAsync(await AssetsManager.DownloadAsset(ct, path));
		}
	}
}
