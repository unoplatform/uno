#nullable enable
using System.Threading.Tasks;
namespace Windows.Storage.Helpers;

public partial class StorageFileHelper
{
	/// <summary>
	/// Determines if a file exists within application package
	/// </summary>
	/// <param name="filename">relative file path</param>
	/// <returns>true if file exists</returns>
	public static async Task<bool> Exists(string filename) => await FileExistsInPackage(filename);
}
