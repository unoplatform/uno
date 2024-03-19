#nullable enable

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
using Uno.Foundation.Logging;
using Uno.UI;
using Windows.Foundation;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.Storage.Helpers;
using Uno.Helpers;
using Windows.ApplicationModel.Background;
using System.Diagnostics.CodeAnalysis;

namespace Windows.Storage;

partial class StorageFile
{
	private static ConcurrentEntryManager _assetGate = new ConcurrentEntryManager();
	private static string? _currentAppID;

	private static async Task<StorageFile> GetFileFromApplicationUri(CancellationToken ct, Uri uri)
	{
		if (uri.Scheme != "ms-appx")
		{
			// ms-appdata is handled by the caller.
			throw new InvalidOperationException("Uri is not using the ms-appx or ms-appdata scheme");
		}

		var originalPath = Uri.UnescapeDataString(uri.PathAndQuery).TrimStart('/');

		var path = AndroidResourceNameEncoder.EncodeResourcePath(originalPath);

		EnsureAppID();

		// Read the contents of our asset
		var outputCachePath = global::System.IO.Path.Combine(Android.App.Application.Context.CacheDir!.AbsolutePath, _currentAppID, path);

		if (typeof(StorageFile).Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
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
			Directory.CreateDirectory(global::System.IO.Path.GetDirectoryName(outputCachePath)!);

			Stream? GetAsset()
			{
				var assets = global::Android.App.Application.Context.Assets;
				if (assets.TryOpen(path, out var stream))
				{
					return stream;
				}
				else if (DrawableHelper.FindResourceIdFromPath(path) is { } resourceId)
				{
					return ContextHelper.Current.Resources!.OpenRawResource(resourceId);
				}

				return null;
			}

			var stream = GetAsset();

			if (stream is null)
			{
				throw new FileNotFoundException($"The file [{originalPath}] cannot be found in the package directory");
			}

			using (stream)
			{
				using var output = File.OpenWrite(outputCachePath);
				await stream.CopyToAsync(output);
			}
		}

		return await StorageFile.GetFileFromPathAsync(outputCachePath);
	}

	[MemberNotNull(nameof(_currentAppID))]
	private static void EnsureAppID()
	{
		if (_currentAppID is null)
		{
			var packageVersion = ApplicationModel.Package.Current.Id.Version;

			_currentAppID =
				ContextHelper.Current.GetType().Assembly.GetModules().First().ModuleVersionId.ToString()
				+ $"_{packageVersion.Major}.{packageVersion.Minor}.{packageVersion.Build}.{packageVersion.Revision}";
		}
	}
}
