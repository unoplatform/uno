using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace __Windows.Storage.Pickers
{
	internal partial class FolderPicker
	{
		internal static partial class NativeMethods
		{
			private const string JsType = "globalThis.Windows.Storage.Pickers.FolderPicker";

			[JSImport($"{JsType}.isNativeSupported")]
			internal static partial bool IsNativeSupported();

			[JSImport($"{JsType}.pickSingleFolderAsync")]
			internal static partial Task<string> PickSingleFolderAsync(string id, string startIn);
		}
	}
}
