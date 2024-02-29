using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace __Windows.ApplicationModel.DataTransfer
{
	internal partial class DataTransferManager
	{
		internal static partial class NativeMethods
		{
			private const string JsType = "globalThis.Windows.ApplicationModel.DataTransfer.DataTransferManager";

			[JSImport($"{JsType}.isSupported")]
			internal static partial bool IsSupported();

			[JSImport($"{JsType}.showShareUI")]
			internal static partial Task<string> ShowShareUIAsync(string title, string text, string? uri);
		}
	}
}
