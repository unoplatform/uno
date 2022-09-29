#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Uno.Extensions;
using Uno.Foundation.Logging;

namespace Uno.UI
{
	public static class AssetsHelper
	{
		private static readonly string _statsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "cache", "assetslist.cache");

		private static Dictionary<string, string> _assets;

		/// <summary>
		/// Finds an asset by its full path, using a case insensitive lookup.
		/// </summary>
		/// <param name="assetPath">A android asset path.</param>
		/// <returns>A case sensitive path from android's asset manager</returns>
		public static string FindAssetFile(string assetPath)
		{
			BuildAssetsMap();

			string actualAssetPath;

			if (_assets.TryGetValue(assetPath, out actualAssetPath))
			{
				return actualAssetPath;
			}

			return null;
		}

		public static IEnumerable<string> AllAssets
		{
			get
			{
				BuildAssetsMap();

				return _assets.Keys;
			}
		}

		/// <summary>
		/// Builds a cache file from the current package's assets.
		/// </summary>
		/// <remarks>
		/// This is required because the enumeration of all assets is particularly
		/// slow, and we can cache it since it does not change for a given package installation. 
		/// </remarks>
		private static void BuildAssetsMap()
		{
			if (_assets == null)
			{
				if (!ReadFromCache())
				{
					_assets = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

					foreach (var asset in InternalEnumerateAssetFiles(""))
					{
						_assets.Add(asset, asset);
					}

					SaveToCache();
				}
			}
		}

		private static void SaveToCache()
		{
			try
			{
				Directory.CreateDirectory(Path.GetDirectoryName(_statsFilePath));
				File.WriteAllLines(_statsFilePath, _assets.Keys);
			}
			catch (Exception e)
			{
				if (typeof(AssetsHelper).Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Error))
				{
					typeof(AssetsHelper).Log().Error($"Failed to write the assets cache file [{_statsFilePath}]", e);
				}
			}
		}

		private static bool ReadFromCache()
		{
			try
			{
				if (File.Exists(_statsFilePath))
				{
					var appInfo = Android.App.Application.Context.PackageManager.GetApplicationInfo(Android.App.Application.Context.PackageName, 0);
					var installedDate = System.IO.File.GetLastWriteTime(appInfo.SourceDir);

					var cacheDate = System.IO.File.GetLastWriteTime(_statsFilePath);

					//Only read from the cache if the app has not been updated since the last cache write time,
					//otherwise, the cache should be invalidated
					if (cacheDate > installedDate)
					{
						_assets = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

						foreach (var asset in File.ReadAllLines(_statsFilePath))
						{
							_assets.Add(asset, asset);
						}

						return true;
					}
				}
			}
			catch(Exception e)
			{
				if (typeof(AssetsHelper).Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Error))
				{
					typeof(AssetsHelper).Log().Error($"Failed to read the assets cache file [{_statsFilePath}]", e);
				}
			}
			
			return false;
		}

		private static IEnumerable<string> InternalEnumerateAssetFiles(string path = "")
		{
			var list = Android.App.Application.Context.Assets.List(path);

			if (list.Length > 0)
			{
				// This is a folder
				foreach (var file in list)
				{
					var subPath = System.IO.Path.Combine(path, file);

					foreach (var child in InternalEnumerateAssetFiles(subPath))
					{
						yield return child;
					}
				}
			}
			else
			{
				yield return path;
			}
		}
	}
}
