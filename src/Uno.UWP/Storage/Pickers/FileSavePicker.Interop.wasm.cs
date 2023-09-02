using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace __Windows.Storage.Pickers
{
	internal partial class FileSavePicker
	{
		internal static partial class NativeMethods
		{
			private const string JsType = "globalThis.Windows.Storage.Pickers.FileSavePicker";

			[JSImport($"{JsType}.isNativeSupported")]
			internal static partial bool IsNativeSupported();

			[JSImport($"{JsType}.nativePickSaveFileAsync")]
			internal static partial Task<string> PickSaveFileAsync(bool showAll, string fileTypeMap, string suggestedFileName, string id, string startIn);

			[JSImport($"{JsType}.SaveAs")]
			internal static partial void SaveAs(string fileName, nint pData, int length);
		}
	}
}
