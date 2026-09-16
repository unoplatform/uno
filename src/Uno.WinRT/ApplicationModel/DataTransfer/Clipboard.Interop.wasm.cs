using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace __Windows.ApplicationModel.DataTransfer
{
	internal partial class Clipboard
	{
		internal static partial class NativeMethods
		{
			private const string JsType = "globalThis.Uno.Utils.Clipboard";

			[JSImport($"{JsType}.getSnapshot")]
			internal static partial string GetSnapshot();

			[JSImport($"{JsType}.getContentAsync")]
			internal static partial Task<string> GetContentAsync(double pasteShortcutTime);

			[JSImport($"{JsType}.releaseHandles")]
			internal static partial void ReleaseHandles(string ids);

			[JSImport($"{JsType}.beginWrite")]
			internal static partial void BeginWrite(int generation, string formatsJson);

			[JSImport($"{JsType}.resolveWriteAsync")]
			internal static partial Task ResolveWriteAsync(int generation, string entriesJson, byte[] imageBytes, string imageMimeType);

			[JSImport($"{JsType}.abortWrite")]
			internal static partial void AbortWrite(int generation);

			[JSImport($"{JsType}.clearAsync")]
			internal static partial Task ClearAsync(int generation);

			[JSImport($"{JsType}.startContentChanged")]
			internal static partial void StartContentChanged();

			[JSImport($"{JsType}.stopContentChanged")]
			internal static partial void StopContentChanged();
		}
	}
}
