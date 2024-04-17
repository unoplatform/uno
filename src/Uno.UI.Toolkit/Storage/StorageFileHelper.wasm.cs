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

	/// <summary>
	/// Retrieves the paths of assets within the current application based on the specified filter predicate.
	/// </summary>
	/// <param name="predicate">A predicate function determining whether a file should be included in the result.</param>
	/// <returns>Returns an array of strings containing the paths of the filtered assets.</returns>
	private static async Task<string[]> GetFilesInDirectory(Func<string, bool> predicate)
	{
		var assets = await AssetsManager.Assets.Value;

		return assets?.Where(e => predicate(e))
			.Select(e => e.Replace('\\', '/'))
			.ToArray() ?? Array.Empty<string>();
	}
}
