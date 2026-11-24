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
		internal static void DispatchStatus(string instanceId, string state)
		{
			if (_instances.TryGetValue(instanceId, out var speechRecognizer))
			{
				if (Enum.TryParse<SpeechRecognizerState>(state, true, out var stateEnum))
				{
					speechRecognizer.OnStateChanged(stateEnum);
				}
			}
		}

		[JSExport]
		internal static Task DispatchStatusAsync(string instanceId, string state)
		{
			DispatchStatus(instanceId, state);

			return Task.CompletedTask;
		}

		[JSExport]
		internal static void DispatchError(string instanceId, string error)
		{
			if (_instances.TryGetValue(instanceId, out var speechRecognizer))
			{
				// Web Speech reports benign terminations (no-speech, aborted, no-match) and genuine
				// failures (network, not-allowed, …) through the same "error" channel. Mirror UWP and
				// complete with a SpeechRecognitionResult carrying the mapped Status instead of throwing.
				var completionSource = speechRecognizer._currentCompletionSource;
				if (completionSource is null)
				{
					LogIgnoredCallback(error);
					return;
				}

				speechRecognizer.OnStateChanged(SpeechRecognizerState.Idle);
				completionSource.TrySetResult(new SpeechRecognitionResult()
				{
					Text = string.Empty,
					Status = WebSpeechErrorMapper.ToStatus(error)
				});
			}
		}

		private static void LogIgnoredCallback(string outcome)
		{
			// Late or duplicate callback (after Dispose, or after the operation already completed).
			if (typeof(SpeechRecognizer).Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				typeof(SpeechRecognizer).Log().LogDebug($"Speech recognition reported '{outcome}' while no operation is active, ignoring.");
			}
		}

		[JSExport]
		internal static Task DispatchErrorAsync(string instanceId, string error)
		{
			DispatchError(instanceId, error);

			return Task.CompletedTask;
		}

		[JSExport]
		internal static void DispatchHypothesis(string instanceId, string hypothesis)
		{
			if (_instances.TryGetValue(instanceId, out var speechRecognizer))
			{
				speechRecognizer.OnHypothesisGenerated(hypothesis);
			}
		}

		[JSExport]
		internal static Task DispatchHypothesisAsync(string instanceId, string hypothesis)
		{
			DispatchHypothesis(instanceId, hypothesis);

			return Task.CompletedTask;
		}

		[JSExport]
		internal static void DispatchResult(string instanceId, string result, double confidence)
		{
			if (_instances.TryGetValue(instanceId, out var speechRecognizer))
			{
				var completionSource = speechRecognizer._currentCompletionSource;
				if (completionSource is null)
				{
					LogIgnoredCallback("result");
					return;
				}

				speechRecognizer.OnStateChanged(SpeechRecognizerState.Idle);
				completionSource.TrySetResult(new SpeechRecognitionResult()
				{
					Text = result,
					RawConfidence = confidence,
					Status = SpeechRecognitionResultStatus.Success
				});
			}
		}

		[JSExport]
		internal static Task DispatchResultAsync(string instanceId, string result, double confidence)
		{
			DispatchResult(instanceId, result, confidence);

			return Task.CompletedTask;
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
			// TrySetCanceled: the source is already finalized when recognition ended via a result or
			// error, and SetCanceled would throw before the instance is released below.
			_currentCompletionSource?.TrySetCanceled();
			_currentCompletionSource = null;

			_instances.TryRemove(_instanceId.ToString(), out _);
			NativeMethods.RemoveInstance(_instanceId.ToString());
		}

		private void InitializeSpeechRecognizer()
		{
			NativeMethods.Initialize(_instanceId.ToString(), CurrentLanguage.LanguageTag);

			_instances.GetOrAdd(_instanceId.ToString(), this);
		}
	}
}
