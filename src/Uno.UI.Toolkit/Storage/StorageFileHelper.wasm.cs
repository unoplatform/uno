#nullable enable
using System;
using System.Linq;
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

	private static async Task<string[]> GetFilesInDirectory(Func<string, bool> predicate)
	{
		var assets = await AssetsManager.Assets.Value;

		return assets?.Where(e => predicate(e))
			.Select(e => e.Replace('\\', '/'))
			.ToArray() ?? Array.Empty<string>();
	}
}
