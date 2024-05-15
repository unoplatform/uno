#nullable enable
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Helpers;

namespace Uno.UI.Toolkit;

partial class StorageFileHelper
{
	private static async Task<bool> FileExistsInPackage(string fileName)
	{
		var assets = await AssetsManager.Assets.Value;
		return assets?.Contains(fileName) ?? false;
	}
}
