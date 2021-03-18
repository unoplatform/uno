using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno;
using Uno.Disposables;
using Uno.Extensions;
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

		[Preserve]
		public static int DispatchError(string instanceId, string error)
		{
			if (_instances.TryGetValue(instanceId, out var speechRecognizer))
			{
				if (speechRecognizer._currentCompletionSource != null)
				{
					speechRecognizer._currentCompletionSource.SetException(
						new InvalidOperationException($"Speech recognition failed with '{error}'"));
				}
				else
				{
					if (typeof(SpeechRecognizer).Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error))
					{
						typeof(SpeechRecognizer).Log().LogError($"Speech recognition failed with '{error}'");
					}
				}
			}
			return 0;
		}

		[Preserve]
		public static int DispatchHypothesis(string instanceId, string hypothesis)
		{
			if (_instances.TryGetValue(instanceId, out var speechRecognizer))
			{
				speechRecognizer.OnHypothesisGenerated(hypothesis);
			}
			return 0;
		}

		[Preserve]
		public static int DispatchResult(string instanceId, string result, double confidence)
		{
			if (_instances.TryGetValue(instanceId, out var speechRecognizer))
			{
				speechRecognizer.OnStateChanged(SpeechRecognizerState.Idle);
				var recognitionResult = new SpeechRecognitionResult()
				{
					Text = result,
					RawConfidence = confidence
				};
				speechRecognizer?._currentCompletionSource.SetResult(recognitionResult);
			}
			return 0;
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

			var command = $"{JsType}.recognize('{_instanceId}')";
			var recognizeResult = WebAssemblyRuntime.InvokeJS(command);
			if (!bool.TryParse(recognizeResult, out var canRecognize) || !canRecognize)
			{
				throw new InvalidOperationException(
					"Speech recognizer is not available on this device.");
			}

			var result = await _currentCompletionSource.Task;
			_currentCompletionSource = null;
			return result;
		}

		public void Dispose()
		{
			_currentCompletionSource?.SetCanceled();
			var removeInstanceCommand = $"{JsType}.removeInstance('{_instanceId}')";
			WebAssemblyRuntime.InvokeJS(removeInstanceCommand);
		}

		private void InitializeSpeechRecognizer()
		{
			var command = $"{JsType}.initialize('{_instanceId}','{CurrentLanguage.LanguageTag}')";
			WebAssemblyRuntime.InvokeJS(command);
			_instances.GetOrAdd(_instanceId.ToString(), this);
		}
	}
}
