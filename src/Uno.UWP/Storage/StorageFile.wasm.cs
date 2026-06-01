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
		/// <summary>
		/// Registers a runtime asset so that subsequent calls to
		/// <see cref="GetFileFromApplicationUriAsync(Uri)"/> succeed for
		/// <c>ms-appx:///</c> paths that are absent from the compile-time
		/// <c>uno-assets.txt</c> manifest.
		/// </summary>
		/// <remarks>
		/// On WASM, <c>ms-appx:///</c> resolution is gated by a static manifest baked at
		/// build time. This method lets runtime code (e.g. plugin hosts or hot-reload
		/// tooling) inject asset content without requiring it to be listed in the manifest.
		/// </remarks>
		/// <param name="assetPath">
		/// The asset path as it would appear in a <c>ms-appx:///</c> URI
		/// (e.g. <c>Fonts/Inter-Regular.ttf</c> or <c>/Fonts/Inter-Regular.ttf</c>).
		/// A leading <c>/</c> is trimmed automatically.
		/// </param>
		/// <param name="content">
		/// Stream containing the asset bytes. The stream is read from its current position;
		/// the caller retains ownership and is responsible for disposal.
		/// </param>
		/// <param name="ct">Cancellation token.</param>
		public static Task RegisterApplicationUriAssetAsync(string assetPath, Stream content, CancellationToken ct = default)
			=> AssetsManager.RegisterAssetAsync(assetPath, content, ct);

		private static async Task<StorageFile> GetFileFromApplicationUri(CancellationToken ct, Uri uri)
		{
			if (uri.Scheme != "ms-appx")
			{
				// ms-appdata is handled by the caller.
				throw new InvalidOperationException("Uri is not using the ms-appx or ms-appdata scheme");
			}

			var path = Uri.UnescapeDataString(uri.PathAndQuery);

			return await StorageFile.GetFileFromPathAsync(await AssetsManager.DownloadAsset(ct, path));
		}
	}
}
