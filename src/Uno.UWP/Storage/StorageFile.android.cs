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
using Uno.Logging;
using Uno.UI;
using Windows.Foundation;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.UI.Composition.Interactions;
using Windows.Storage.Helpers;

namespace Windows.Storage
{
	partial class StorageFile
	{
		private static ConcurrentEntryManager _assetGate = new ConcurrentEntryManager();

		private static async Task<StorageFile> GetFileFromApplicationUri(CancellationToken ct, Uri uri)
		{
			if(uri.Scheme != "ms-appx")
			{
				throw new InvalidOperationException("Uri is not using the ms-appx scheme");
			}

			var path = AndroidResourceNameEncoder.EncodeResourcePath(uri.PathAndQuery.TrimStart(new char[] { '/' }));

			// Read the contents of our asset
			var assets = global::Android.App.Application.Context.Assets;
			var outputCachePath = global::System.IO.Path.Combine(Android.App.Application.Context.CacheDir.AbsolutePath, path);

			if (typeof(StorageFile).Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				typeof(StorageFile).Log().Debug($"GetFileFromApplicationUriAsyncTask path:{path} outputCachePath:{outputCachePath}");
			}

			// Ensure only one generation of the cached file can occur at a time.
			// The copy of tha android asset should not be necessary, but requires a significant
			// refactoring of the StorageFile class to make non-local provides opaque.
			// This will need to be revisited once we add support for other providers.
			using var gate = await _assetGate.LockForAsset(ct, path);

			if (!File.Exists(outputCachePath))
			{
				Directory.CreateDirectory(global::System.IO.Path.GetDirectoryName(outputCachePath));

				using var input = assets.Open(path);
				using var output = File.OpenWrite(outputCachePath);

				await input.CopyToAsync(output);
			}

			return await StorageFile.GetFileFromPathAsync(outputCachePath);
		}
	}
}
