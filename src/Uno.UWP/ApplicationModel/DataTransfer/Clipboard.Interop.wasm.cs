using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace __Windows.ApplicationModel.DataTransfer
{
	internal partial class Clipboard
	{
		internal static partial class NativeMethods
		{
			private const string JsType = "globalThis.Uno.Utils.Clipboard";

			[JSImport($"{JsType}.getText")]
			internal static partial Task<string> GetTextAsync();

			[JSImport($"{JsType}.setText")]
			internal static partial void SetText(string text);

			[JSImport($"{JsType}.getHtml")]
			internal static partial Task<string> GetHtmlAsync();

			[JSImport($"{JsType}.setHtml")]
			internal static partial Task SetHtmlAsync(string html, string text);

			[JSImport($"{JsType}.startContentChanged")]
			internal static partial void StartContentChanged();

			[JSImport($"{JsType}.stopContentChanged")]
			internal static partial void StopContentChanged();
		}
	}
}
