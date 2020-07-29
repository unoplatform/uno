using System;
using Uno.Foundation;

namespace Windows.Media.SpeechRecognition
{
	public partial class SpeechRecognizer
    {
		private const string JsType = "Windows.Media.SpeechRecognition.SpeechRecognizer";

		private Guid _instanceId = Guid.NewGuid();

		private void InitializeSpeechRecognizer()
		{
			var command = $"{JsType}.initialize('{_instanceId}')";
			WebAssemblyRuntime.InvokeJS(command);
		}
	}
}
