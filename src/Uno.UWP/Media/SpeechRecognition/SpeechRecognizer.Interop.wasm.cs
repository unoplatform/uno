using System.Runtime.InteropServices.JavaScript;

namespace __Windows.Media.SpeechRecognition
{
	internal partial class SpeechRecognizer
	{
		internal static partial class NativeMethods
		{
			private const string JsType = "globalThis.Windows.Media.SpeechRecognizer";

			[JSImport($"{JsType}.initialize")]
			internal static partial void Initialize(string id, string language);

			[JSImport($"{JsType}.recognize")]
			internal static partial bool Recognize(string id);

			[JSImport($"{JsType}.removeInstance")]
			internal static partial void RemoveInstance(string id);
		}
	}
}
