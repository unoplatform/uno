#nullable enable
using System.Threading;
using System.Threading.Tasks;

namespace Windows.Storage.Helpers;

partial class StorageFileHelper
{
	private static async Task<bool> FileExistsInPackage(string fileName)
	{
		var assets = await AssetsManager.GetAssets(CancellationToken.None);
		return assets?.Contains(fileName) ?? false;
	}
}
