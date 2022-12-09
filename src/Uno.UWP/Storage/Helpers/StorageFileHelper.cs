#nullable enable
using System.Threading.Tasks;

namespace Windows.Storage.Helpers;

public partial class StorageFileHelper
{
	/// <summary>
	/// Determines if a file exists within application package
	/// </summary>
	/// <param name="fileName">relative file path</param>
	/// <returns>A task that will complete with a result of true if file exists, otherwise with a result of false.</returns>
	public static async Task<bool> Exists(string fileName) => await FileExistsInPackage(fileName);
}
