using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace __Windows.Storage.Helpers
{
	internal partial class AssetsManager
	{
		internal static partial class NativeMethods
		{
			[JSImport("globalThis.Windows.Storage.AssetManager.DownloadAsset")]
			internal static partial Task<string> DownloadAssetAsync(string uri);

			[JSImport("globalThis.Windows.Storage.AssetManager.DownloadAssetsManifest")]
			internal static partial Task<string> DownloadAssetsManifestAsync(string uri);
		}
	}
}
