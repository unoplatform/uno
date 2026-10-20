using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace __Windows.Storage
{
	internal partial class StorageFile
	{
		internal static partial class NativeMethods
		{
			private const string JsType = "globalThis.Uno.Storage.NativeStorageFile";

			[JSImport($"{JsType}.getBasicPropertiesAsync")]
			internal static partial Task<string> GetBasicPropertiesAsync(string id);
		}
	}
}
