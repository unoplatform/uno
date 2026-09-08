using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace __Windows.Storage
{
	internal partial class StorageFolder
	{
		internal static partial class NativeMethods
		{
			private const string JsType = "globalThis.Uno.Storage.NativeStorageFolder";

			[JSImport("globalThis.Windows.Storage.StorageFolder.makePersistent")]
			internal static partial Task MakePersistentAsync(string[] folders);

			[JSImport($"{JsType}.createFileAsync")]
			internal static partial Task<string> CreateFileAsync(string id, string name);

			[JSImport($"{JsType}.createFolderAsync")]
			internal static partial Task<string> CreateFolderAsync(string id, string name);

			[JSImport($"{JsType}.deleteItemAsync")]
			internal static partial Task<string> DeleteItemAsync(string id, string name);

			[JSImport($"{JsType}.getFilesAsync")]
			internal static partial Task<string> GetFilesAsync(string id);

			[JSImport($"{JsType}.getFoldersAsync")]
			internal static partial Task<string> GetFoldersAsync(string id);

			[JSImport($"{JsType}.getItemsAsync")]
			internal static partial Task<string> GetItemsAsync(string id);

			[JSImport($"{JsType}.getPrivateRootAsync")]
			internal static partial Task<string> GetPrivateRootAsync();

			[JSImport($"{JsType}.tryGetFileAsync")]
			internal static partial Task<string> TryGetFileAsync(string id, string name);

			[JSImport($"{JsType}.tryGetFolderAsync")]
			internal static partial Task<string> TryGetFolderAsync(string id, string name);
		}
	}
}
