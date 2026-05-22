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
				// Web Speech reports benign terminations (no-speech, aborted, no-match) and genuine
				// failures (network, not-allowed, …) through the same "error" channel. Mirror UWP and
				// complete with a SpeechRecognitionResult carrying the mapped Status instead of throwing,
				// so every outcome flows back to the caller exactly like a successful recognition.
				speechRecognizer.OnStateChanged(SpeechRecognizerState.Idle);
				var recognitionResult = new SpeechRecognitionResult()
				{
					Text = string.Empty,
					Status = MapErrorToStatus(error)
				};

				// TrySetResult: ignore the report if the source was already completed or cancelled.
				speechRecognizer._currentCompletionSource?.TrySetResult(recognitionResult);
			}
			return 0;
		}

		private static SpeechRecognitionResultStatus MapErrorToStatus(string error) =>
			error switch
			{
				"no-speech" => SpeechRecognitionResultStatus.TimeoutExceeded,
				"no-match" => SpeechRecognitionResultStatus.Success,
				"aborted" => SpeechRecognitionResultStatus.UserCanceled,
				"audio-capture" => SpeechRecognitionResultStatus.MicrophoneUnavailable,
				"not-allowed" => SpeechRecognitionResultStatus.MicrophoneUnavailable,
				"service-not-allowed" => SpeechRecognitionResultStatus.MicrophoneUnavailable,
				"network" => SpeechRecognitionResultStatus.NetworkFailure,
				"language-not-supported" => SpeechRecognitionResultStatus.TopicLanguageNotSupported,
				"bad-grammar" => SpeechRecognitionResultStatus.GrammarCompilationFailure,
				_ => SpeechRecognitionResultStatus.Unknown
			};

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
					RawConfidence = confidence,
					Status = SpeechRecognitionResultStatus.Success
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
				_currentCompletionSource = null;

				// No recognition engine in this browser. Report it through the result/status contract
				// rather than throwing, consistent with the other completion paths.
				return new SpeechRecognitionResult()
				{
					Text = string.Empty,
					Status = SpeechRecognitionResultStatus.Unknown
				};
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
