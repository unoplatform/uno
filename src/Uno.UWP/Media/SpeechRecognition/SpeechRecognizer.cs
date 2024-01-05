#if __ANDROID__ || __IOS__ || __WASM__
using System;
using System.Globalization;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Globalization;

namespace Windows.Media.SpeechRecognition
{
	public partial class SpeechRecognizer
	{
		public SpeechRecognizerState State { get; private set; } = SpeechRecognizerState.Idle;

		public SpeechRecognizerUIOptions UIOptions { get; private set; } = new SpeechRecognizerUIOptions();

		public SpeechRecognizerTimeouts Timeouts { get; private set; } = new SpeechRecognizerTimeouts();

		public Language CurrentLanguage { get; private set; }

		public event TypedEventHandler<SpeechRecognizer, SpeechRecognitionHypothesisGeneratedEventArgs>? HypothesisGenerated;

		public event TypedEventHandler<SpeechRecognizer, SpeechRecognizerStateChangedEventArgs>? StateChanged;

		public SpeechRecognizer()
			: this(new Language(CultureInfo.CurrentCulture.Name))
		{
		}

		public SpeechRecognizer(Language language)
		{
			CurrentLanguage = language;
			InitializeSpeechRecognizer();
		}

		public IAsyncOperation<SpeechRecognitionCompilationResult> CompileConstraintsAsync()
		{
			return AsyncOperation.FromTask(ct =>
			{
				return Task.FromResult(new SpeechRecognitionCompilationResult()
				{
					Status = SpeechRecognitionResultStatus.Success
				});
			});
		}

		private void OnHypothesisGenerated(string hypothesis)
		{
			HypothesisGenerated?.Invoke(
				this,
				new SpeechRecognitionHypothesisGeneratedEventArgs()
				{
					Hypothesis = new SpeechRecognitionHypothesis()
					{
						Text = hypothesis
					}
				});
		}

		private void OnStateChanged(SpeechRecognizerState state)
		{
			StateChanged?.Invoke(
				this,
				new SpeechRecognizerStateChangedEventArgs()
				{
					State = state
				});
		}
	}
}
#endif
