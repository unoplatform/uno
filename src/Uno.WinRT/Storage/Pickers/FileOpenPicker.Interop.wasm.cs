using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace __Windows.Storage.Pickers
{
	internal partial class FileOpenPicker
	{
		internal static partial class NativeMethods
		{
			private const string JsType = "globalThis.Windows.Storage.Pickers.FileOpenPicker";

			[JSImport($"{JsType}.isNativeSupported")]
			internal static partial bool IsNativeSupported();

			[JSImport($"{JsType}.nativePickFilesAsync")]
			internal static partial Task<string> PickFilesAsync(bool multiple, bool showAll, string fileTypeMap, string id, string startIn);

			[JSImport($"{JsType}.uploadPickFilesAsync")]
			internal static partial Task<string> UploadPickFilesAsync(bool multiple, string targetFolder, string accept);
		}
	}
}
