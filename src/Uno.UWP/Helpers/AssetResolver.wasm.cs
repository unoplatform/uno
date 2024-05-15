using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage.Helpers;

using NativeMethods = __Windows.Storage.Helpers.AssetsManager.NativeMethods;

namespace Uno.Helpers;

internal static partial class AssetResolver
{
	private static readonly Lazy<Task<HashSet<string>>> _assets = new Lazy<Task<HashSet<string>>>(GetAssets);

	// POTENTIAL BUG HERE: if the "fetch" failed, the application
	// will never retry to fetch it again.
	public static Task<HashSet<string>> Assets => _assets.Value;

	private static async Task<HashSet<string>> GetAssets()
	{
		var assetsUri = AssetsPathBuilder.BuildAssetUri("uno-assets.txt");

		var assets = await NativeMethods.DownloadAssetsManifestAsync(assetsUri);

		return new HashSet<string>(LineMatch().Split(assets));
	}

	[GeneratedRegex("\r\n|\r|\n")]
	private static partial Regex LineMatch();
}
