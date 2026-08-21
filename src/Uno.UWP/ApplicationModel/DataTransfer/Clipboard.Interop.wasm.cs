using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace __Windows.ApplicationModel.DataTransfer
{
	internal partial class Clipboard
	{
		internal static partial class NativeMethods
		{
			private const string JsType = "globalThis.Uno.Utils.Clipboard";

			[JSImport($"{JsType}.getSnapshotFormats")]
			internal static partial string GetSnapshotFormats();

			[JSImport($"{JsType}.getContentAsync")]
			internal static partial Task<string> GetContentAsync(bool fromPaste);

			[JSImport($"{JsType}.setContentAsync")]
			internal static partial Task SetContentAsync(string entriesJson, byte[] imageBytes, string imageMimeType);

			[JSImport($"{JsType}.clearAsync")]
			internal static partial Task ClearAsync();

			[JSImport($"{JsType}.startContentChanged")]
			internal static partial void StartContentChanged();

			[JSImport($"{JsType}.stopContentChanged")]
			internal static partial void StopContentChanged();
		}
	}
}
