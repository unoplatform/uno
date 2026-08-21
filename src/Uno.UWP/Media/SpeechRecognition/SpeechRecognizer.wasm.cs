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
				if (speechRecognizer._currentCompletionSource != null)
				{
					speechRecognizer._currentCompletionSource.SetException(
						new InvalidOperationException($"Speech recognition failed with '{error}'"));
				}
				else
				{
					if (typeof(SpeechRecognizer).Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Error))
					{
						typeof(SpeechRecognizer).Log().LogError($"Speech recognition failed with '{error}'");
					}
				}
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
				speechRecognizer.OnStateChanged(SpeechRecognizerState.Idle);
				var recognitionResult = new SpeechRecognitionResult()
				{
					Text = result,
					RawConfidence = confidence
				};
				speechRecognizer?._currentCompletionSource.SetResult(recognitionResult);
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

			NativeMethods.RemoveInstance(_instanceId.ToString());
		}

		private void InitializeSpeechRecognizer()
		{
			NativeMethods.Initialize(_instanceId.ToString(), CurrentLanguage.LanguageTag);

			_instances.GetOrAdd(_instanceId.ToString(), this);
		}
	}
}
