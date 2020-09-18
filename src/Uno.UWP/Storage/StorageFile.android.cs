#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Android.Content.Res;
using Uno;
using Uno.Extensions;
using Uno.UI;
using Windows.Foundation;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.UI.Composition.Interactions;

namespace Windows.Storage
{
	public partial class StorageFile : StorageItem, IStorageFile
	{
		private static async Task<StorageFile> GetFileFromApplicationUriAsyncTask(CancellationToken ct, Uri uri)
		{
			if(uri.Scheme != "ms-appx")
			{
				throw new InvalidOperationException("Uri is not using the ms-appx scheme");
			}

			var path = AndroidResourceNameEncoder.EncodeResourcePath(uri.PathAndQuery.TrimStart(new char[] { '/' }));

			// Read the contents of our asset
			var assets = global::Android.App.Application.Context.Assets;
			var outputCachePath = global::System.IO.Path.Combine(Android.App.Application.Context.CacheDir.AbsolutePath, path);

			using var input = assets.Open(path);

			using var output = File.OpenWrite(outputCachePath);
			await input.CopyToAsync(output);

			return await StorageFile.GetFileFromPathAsync(outputCachePath);
		}
	}
}
