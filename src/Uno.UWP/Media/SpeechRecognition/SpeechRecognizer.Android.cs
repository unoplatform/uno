using System;
using System.Threading.Tasks;
using Android.Content;
using Android.Speech;
using Windows.Foundation;

namespace Windows.Media.SpeechRecognition
{
	public partial class SpeechRecognizer : IDisposable
	{
		private Android.Speech.SpeechRecognizer _speechRecognizer;

		private void InitializeSpeechRecognizer()
		{
			_speechRecognizer = Android.Speech.SpeechRecognizer.CreateSpeechRecognizer(Android.App.Application.Context);
		}

		public IAsyncOperation<SpeechRecognitionResult> RecognizeAsync()
		{
			var tcs = new TaskCompletionSource<SpeechRecognitionResult>();

			var listener = new SpeechRecognitionListener
			{
				StartOfSpeech = () =>
				{
					OnStateChanged(SpeechRecognizerState.SpeechDetected);
				},
				EndOfSpeech = () =>
				{
					OnStateChanged(SpeechRecognizerState.Capturing);
				},
				Error = error =>
				{
					OnStateChanged(SpeechRecognizerState.Idle);
					tcs.TrySetException(new Exception($"Error during speech recognition: {error.ToString()}"));
				},
				PartialResults = result =>
				{
					OnHypothesisGenerated(result.Text);
				},
				FinalResults = result =>
				{
					OnStateChanged(SpeechRecognizerState.Idle);
					tcs.TrySetResult(result);
				}
			};

			_speechRecognizer.SetRecognitionListener(listener);
			_speechRecognizer.StartListening(this.CreateSpeechIntent());

			OnStateChanged(SpeechRecognizerState.Capturing);

			return tcs.Task.AsAsyncOperation();
		}

		protected virtual Intent CreateSpeechIntent()
		{
			var intent = new Intent(RecognizerIntent.ActionRecognizeSpeech);
			intent.PutExtra(RecognizerIntent.ExtraLanguagePreference, Java.Util.Locale.ForLanguageTag(CurrentLanguage.LanguageTag));
			intent.PutExtra(RecognizerIntent.ExtraLanguage, Java.Util.Locale.ForLanguageTag(CurrentLanguage.LanguageTag));
			intent.PutExtra(RecognizerIntent.ExtraLanguageModel, RecognizerIntent.LanguageModelFreeForm);
			intent.PutExtra(RecognizerIntent.ExtraCallingPackage, Android.App.Application.Context.PackageName);
			intent.PutExtra(RecognizerIntent.ExtraPartialResults, true);
			intent.PutExtra(RecognizerIntent.ExtraSpeechInputCompleteSilenceLengthMillis, Timeouts.EndSilenceTimeout.TotalMilliseconds);
			intent.PutExtra(RecognizerIntent.ExtraSpeechInputPossiblyCompleteSilenceLengthMillis, Timeouts.BabbleTimeout.TotalMilliseconds);
			intent.PutExtra(RecognizerIntent.ExtraSpeechInputMinimumLengthMillis, Timeouts.InitialSilenceTimeout.TotalMilliseconds);

			return intent;
		}

		public IAsyncAction StopRecognitionAsync()
		{
			_speechRecognizer?.StopListening();
			_speechRecognizer?.Destroy();
			return Task.CompletedTask.AsAsyncAction();
		}

		public void Dispose()
		{
			_speechRecognizer?.Dispose();
		}
	}
}
