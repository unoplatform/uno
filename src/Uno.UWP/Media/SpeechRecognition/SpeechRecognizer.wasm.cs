using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Uno;
using Uno.Foundation;
using Windows.Foundation;

namespace Windows.Media.SpeechRecognition
{
	public partial class SpeechRecognizer
	{
		private const string JsType = "Windows.Media.SpeechRecognizer";

		private readonly static ConcurrentDictionary<string, SpeechRecognizer> _instances =
			new ConcurrentDictionary<string, SpeechRecognizer>();

		private readonly Guid _instanceId = Guid.NewGuid();

		private TaskCompletionSource<SpeechRecognitionResult> _currentCompletionSource;

		[Preserve]
		public static int DispatchStatus(string instanceId, string state)
		{
			if (_instances.TryGetValue(instanceId, out var speechRecognizer))
			{
				if (Enum.TryParse<SpeechRecognizerState>(state, true, out var stateEnum))
				{
					speechRecognizer.OnStateChanged(stateEnum);
				}
			}
			return 0;
		}

		private void InitializeSpeechRecognizer()
		{
			var command = $"{JsType}.initialize('{_instanceId}')";
			WebAssemblyRuntime.InvokeJS(command);
			_instances.GetOrAdd(_instanceId.ToString(), this);
		}

		public IAsyncOperation<SpeechRecognitionResult> RecognizeAsync() =>
			RecognizeTaskAsync().AsAsyncOperation();

		private async Task<SpeechRecognitionResult> RecognizeTaskAsync()
		{
			var existingTask = _currentCompletionSource?.Task;
			if (existingTask != null)
			{
				return await existingTask;
			}

			_currentCompletionSource = new TaskCompletionSource<SpeechRecognitionResult>();



			var result = await _currentCompletionSource.Task;
			_currentCompletionSource = null;
			return result;
		}



		public void Dispose()
		{
			
		}
	}
}
