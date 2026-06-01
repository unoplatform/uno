#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Uno.Disposables;
using Uno.Extensions;
using Uno.Foundation;
using Uno.Threading;
using Windows.Devices.AllJoyn;
using Windows.Foundation;
using Windows.Media.Streaming.Adaptive;
using Windows.Security.Cryptography.Core;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.UI.WebUI;

using NativeMethods = __Windows.Storage.Helpers.AssetsManager.NativeMethods;

namespace Windows.Storage.Helpers
{
	internal partial class AssetsManager
	{
		internal static Lazy<Task<HashSet<string>>> Assets { get; } = new Lazy<Task<HashSet<string>>>(() => GetAssets(CancellationToken.None));
		private static readonly ConcurrentEntryManager _assetsGate = new ConcurrentEntryManager();

		private static async Task<HashSet<string>> GetAssets(CancellationToken ct)
		{
			var assetsUri = AssetsPathBuilder.BuildAssetUri("uno-assets.txt");

			var assets = await NativeMethods.DownloadAssetsManifestAsync(assetsUri);

			return new HashSet<string>(SplitMatch().Split(assets), StringComparer.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Registers a runtime asset so that subsequent <c>ms-appx:///</c> resolution
		/// succeeds for paths absent from the compile-time <c>uno-assets.txt</c> manifest.
		/// </summary>
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
		internal static async Task RegisterAssetAsync(string assetPath, Stream content, CancellationToken ct)
		{
			var updatedPath = assetPath.TrimStart('/');

			var localPath = Path.Combine(
				ApplicationData.Current.LocalFolder.Path,
				".assetsCache",
				AssetsPathBuilder.UNO_BOOTSTRAP_APP_BASE,
				updatedPath);

			// Use the same per-path gate as DownloadAsset to serialize concurrent
			// registrations for the same path and avoid torn writes.
			using var assetLock = await _assetsGate.LockForAsset(ct, updatedPath);

			if (Path.GetDirectoryName(localPath) is { Length: > 0 } dir)
			{
				Directory.CreateDirectory(dir);
			}

			await using var outStream = File.Create(localPath);
			await content.CopyToAsync(outStream, ct);

			// Register in the in-memory manifest after the file is fully written so that
			// a concurrent DownloadAsset call cannot observe the path without its content.
			var assetSet = await Assets.Value;
			assetSet.Add(updatedPath);
		}

		public static async Task<string> DownloadAsset(CancellationToken ct, string assetPath)
		{
			var updatedPath = assetPath.TrimStart("/");
			var assetSet = await Assets.Value;

			if (assetSet.Contains(updatedPath))
			{
				var localPath = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, ".assetsCache", AssetsPathBuilder.UNO_BOOTSTRAP_APP_BASE, updatedPath);

				using var assetLock = await _assetsGate.LockForAsset(ct, updatedPath);

				if (!File.Exists(localPath))
				{
					var assetUri = AssetsPathBuilder.BuildAssetUri(updatedPath);
					var assetInfo = await NativeMethods.DownloadAssetAsync(assetUri);

					var parts = assetInfo.Split(';');
					if (parts.Length == 2)
					{
						var ptr = (IntPtr)int.Parse(parts[0], CultureInfo.InvariantCulture);
						var length = int.Parse(parts[1], CultureInfo.InvariantCulture);

						try
						{
							var buffer = new byte[length];
							Marshal.Copy(ptr, buffer, 0, length);

							if (Path.GetDirectoryName(localPath) is { } path)
							{
								Directory.CreateDirectory(path);
							}

							File.WriteAllBytes(localPath, buffer);
						}
						finally
						{
							Marshal.FreeHGlobal(ptr);
						}
					}
					else
					{
						throw new InvalidOperationException($"Invalid Windows.Storage.AssetManager.DownloadAsset return value");
					}
				}

				return localPath;
			}
			else
			{
				throw new FileNotFoundException($"The file [{assetPath}] cannot be found");
			}
		}

		[GeneratedRegex("\r\n|\r|\n")]
		private static partial Regex SplitMatch();
	}
}
