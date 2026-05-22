using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Uno;
using Uno.Disposables;
using Uno.Extensions;
using Uno.Foundation;
using Uno.Foundation.Logging;
using Windows.Foundation;

using NativeMethods = __Windows.Media.SpeechRecognition.SpeechRecognizer.NativeMethods;

namespace Windows.Media.SpeechRecognition
{
	public partial class SpeechRecognizer
	{
		private readonly static ConcurrentDictionary<string, SpeechRecognizer> _instances =
			new ConcurrentDictionary<string, SpeechRecognizer>();

		private readonly Guid _instanceId = Guid.NewGuid();

		private TaskCompletionSource<SpeechRecognitionResult> _currentCompletionSource;

		[JSExport]
		internal static int DispatchStatus(string instanceId, string state)
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

		[JSExport]
		internal static int DispatchError(string instanceId, string error)
		{
			if (_instances.TryGetValue(instanceId, out var speechRecognizer))
			{
				// TrySetException: the completion source may already be completed (a result arrived,
				// or it was cancelled by Dispose), in which case the failure is only logged.
				if (speechRecognizer._currentCompletionSource?.TrySetException(
						new InvalidOperationException($"Speech recognition failed with '{error}'")) != true)
				{
					if (typeof(SpeechRecognizer).Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Error))
					{
						typeof(SpeechRecognizer).Log().LogError($"Speech recognition failed with '{error}'");
					}
				}
			}
			return 0;
		}

		[JSExport]
		internal static int DispatchHypothesis(string instanceId, string hypothesis)
		{
			if (_instances.TryGetValue(instanceId, out var speechRecognizer))
			{
				speechRecognizer.OnHypothesisGenerated(hypothesis);
			}
			return 0;
		}

		[JSExport]
		internal static int DispatchResult(string instanceId, string result, double confidence)
		{
			if (_instances.TryGetValue(instanceId, out var speechRecognizer))
			{
				speechRecognizer.OnStateChanged(SpeechRecognizerState.Idle);
				var recognitionResult = new SpeechRecognitionResult()
				{
					Text = result,
					RawConfidence = confidence
				};

				// TrySetResult: ignore a late/duplicate result if the source was already completed.
				speechRecognizer._currentCompletionSource?.TrySetResult(recognitionResult);
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

			var recognizeResult = NativeMethods.Recognize(_instanceId.ToString());

			if (!recognizeResult)
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
			// TrySetCanceled (not SetCanceled): once recognition has completed via a result or error
			// the source is already finalized, and SetCanceled would throw
			// InvalidOperationException ("TaskT_TransitionToFinal_AlreadyCompleted") — which would also
			// skip RemoveInstance below and leak the JS recognizer instance.
			_currentCompletionSource?.TrySetCanceled();
			_currentCompletionSource = null;

			NativeMethods.RemoveInstance(_instanceId.ToString());
		}

		private void InitializeSpeechRecognizer()
		{
			NativeMethods.Initialize(_instanceId.ToString(), CurrentLanguage.LanguageTag);

			_instances.GetOrAdd(_instanceId.ToString(), this);
		}
	}
}
