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

namespace Windows.Storage
{
	internal partial class AssetsManager
	{
		private static readonly string UNO_BOOTSTRAP_APP_BASE = Environment.GetEnvironmentVariable(nameof(UNO_BOOTSTRAP_APP_BASE));

		private static readonly Lazy<Task<HashSet<string>>> _assets = new Lazy<Task<HashSet<string>>>(() => GetAssets(CancellationToken.None));
		private static readonly Dictionary<string, DownloadEntry> _assetsGate = new Dictionary<string, DownloadEntry>();

		private static async Task<HashSet<string>> GetAssets(CancellationToken ct)
		{
			var assetsUri = !string.IsNullOrEmpty(UNO_BOOTSTRAP_APP_BASE) ? $"{UNO_BOOTSTRAP_APP_BASE}/uno-assets.txt" : "uno-assets.txt";

			var assets = await WebAssemblyRuntime.InvokeAsync($"Windows.Storage.AssetManager.DownloadAssetsManifest(\'{assetsUri}\')");

			return new HashSet<string>(Regex.Split(assets, "\r\n|\r|\n"), StringComparer.OrdinalIgnoreCase);
		}

		public static async Task<string> DownloadAsset(CancellationToken ct, string assetPath)
		{
			var updatedPath = assetPath.TrimStart("/");
			var assetSet = await _assets.Value;

			if (assetSet.Contains(updatedPath))
			{
				var localPath = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, ".assetsCache", UNO_BOOTSTRAP_APP_BASE, updatedPath);

				using var assetLock = await LockForAsset(ct, updatedPath);

				if (!File.Exists(localPath))
				{
					var assetUri = !string.IsNullOrEmpty(UNO_BOOTSTRAP_APP_BASE) ? $"{UNO_BOOTSTRAP_APP_BASE}/{updatedPath}" : updatedPath;
					var assetInfo = await WebAssemblyRuntime.InvokeAsync($"Windows.Storage.AssetManager.DownloadAsset(\'{assetUri}\')");

					var parts = assetInfo.Split(';');
					if (parts.Length == 2)
					{
						var ptr = (IntPtr)int.Parse(parts[0], CultureInfo.InvariantCulture);
						var length = int.Parse(parts[1], CultureInfo.InvariantCulture);

						try
						{
							var buffer = new byte[length];
							Marshal.Copy(ptr, buffer, 0, length);

							Directory.CreateDirectory(Path.GetDirectoryName(localPath));
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

		private static async Task<IDisposable> LockForAsset(CancellationToken ct, string updatedPath)
		{
			DownloadEntry GetEntry()
			{
				lock (_assetsGate)
				{
					if (!_assetsGate.TryGetValue(updatedPath, out var entry))
					{
						_assetsGate[updatedPath] = entry = new DownloadEntry();
					}

					entry.ReferenceCount++;

					return entry;
				}
			}

			var entry = GetEntry();

			var disposable = await entry.Gate.LockAsync(ct);

			void ReleaseEntry()
			{
				lock (_assetsGate)
				{
					disposable.Dispose();

					if(--entry.ReferenceCount == 0)
					{
						_assetsGate.Remove(updatedPath);
					}
				}
			}

			return Disposable.Create(ReleaseEntry);
		}

		private class DownloadEntry
		{
			public int ReferenceCount;
			public AsyncLock Gate { get; } = new AsyncLock();
		}
	}
}
