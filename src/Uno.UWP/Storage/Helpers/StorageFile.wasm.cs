#nullable enable
using System.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Uno.Extensions;
using Uno.Foundation;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Windows.Storage.Helpers;

partial class StorageFileHelper
{
	private static string assets { get; set; } = string.Empty;
	private static async Task<bool> FileExistsInPackage(string fileName)
	{
		if (string.IsNullOrEmpty(assets))
		{
			var assetsUri = AssetsPathBuilder.BuildAssetUri("uno-assets.txt");
			assets = await WebAssemblyRuntime.InvokeAsync($"Windows.Storage.AssetManager.DownloadAssetsManifest(\'{assetsUri}\')");
		}

		var assetsHash = new HashSet<string>(Regex.Split(assets, "\r\n|\r|\n"), StringComparer.OrdinalIgnoreCase);

		return assetsHash?.Contains(fileName) ?? false;
	}
}
