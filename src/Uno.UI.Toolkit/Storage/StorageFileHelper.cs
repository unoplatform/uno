#nullable enable
using System.Threading;
using System;
using System.Threading.Tasks;
using Uno;

namespace Uno.UI.Toolkit;

public partial class StorageFileHelper
{
	/// <summary>
	/// Determines if an asset or resource exists within application package
	/// </summary>
	/// <param name="fileName">relative file path</param>
	/// <returns>A task that will complete with a result of true if file exists, otherwise with a result of false.</returns>
	public static async Task<bool> ExistsInPackage(string fileName) => await FileExistsInPackage(fileName);

#if NET461 || __NETSTD_REFERENCE__
	private static Task<bool> FileExistsInPackage(string fileName)
		=> throw new NotImplementedException();
#endif
}
