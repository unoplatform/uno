#if __IOS__
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AVFoundation;
using Foundation;
using Speech;
using Uno.Threading;
using Windows.Foundation;

namespace Windows.Media.SpeechRecognition
{
	public partial class SpeechRecognizer : IDisposable
	{
		private AVAudioEngine audioEngine = new AVAudioEngine();

		private SFSpeechRecognizer speechRecognizer;
		private SFSpeechAudioBufferRecognitionRequest recognitionRequest;
		private SFSpeechRecognitionTask recognitionTask;

		private void InitializeSpeechRecognizer()
		{
			speechRecognizer?.Dispose();
			speechRecognizer = new SFSpeechRecognizer(new NSLocale(CurrentLanguage.LanguageTag));
		}

		public IAsyncOperation<SpeechRecognitionResult> RecognizeAsync()
		{
			// Cancel the previous task if it's running.
			recognitionTask?.Cancel();
			recognitionTask = null;
			recognitionRequest?.Dispose();
			recognitionRequest = null;

			var audioSession = AVAudioSession.SharedInstance();
			NSError err;
			err = audioSession.SetCategory(AVAudioSessionCategory.Record);
			audioSession.SetMode(AVAudioSession.ModeMeasurement, out err);
			err = audioSession.SetActive(true, AVAudioSessionSetActiveOptions.NotifyOthersOnDeactivation);

			// Configure request so that results are returned before audio recording is finished
			recognitionRequest = new SFSpeechAudioBufferRecognitionRequest
			{
				ShouldReportPartialResults = true,
				TaskHint = SFSpeechRecognitionTaskHint.Dictation
			};

			var inputNode = audioEngine.InputNode;
			if (inputNode == null)
			{
				throw new InvalidProgramException("Audio engine has no input node");
			}

			var tcs = new FastTaskCompletionSource<SpeechRecognitionResult>();

			// A recognition task represents a speech recognition session.
			// We keep a reference to the task so that it can be cancelled.
			recognitionTask = speechRecognizer.GetRecognitionTask(recognitionRequest, (result, error) =>
			{
				var isFinal = false;
				var bestMatch = default(SpeechRecognitionResult);

				if (result != null)
				{
					bestMatch = new SpeechRecognitionResult()
					{
						Text = result.BestTranscription.FormattedString,
						Alternates = result.Transcriptions?
							.Select(t => new SpeechRecognitionResult()
							{
								Text = t.FormattedString
							})
							.ToList()
					};
					isFinal = result.Final;

					OnHypothesisGenerated(bestMatch.Text);
				}

				if (error != null || isFinal)
				{
					audioEngine.Stop();

					inputNode.RemoveTapOnBus(0);

					audioSession = AVAudioSession.SharedInstance();
					err = audioSession.SetCategory(AVAudioSessionCategory.Playback);
					audioSession.SetMode(AVAudioSession.ModeDefault, out err);
					err = audioSession.SetActive(false, AVAudioSessionSetActiveOptions.NotifyOthersOnDeactivation);

					recognitionRequest = null;
					recognitionTask = null;
					
					OnStateChanged(SpeechRecognizerState.Idle);

					if (bestMatch != null)
					{
						tcs.TrySetResult(bestMatch);
					}
					else
					{
						tcs.TrySetException(new Exception(error.LocalizedDescription));
					}
				}
			});

			var recordingFormat = new AVAudioFormat(sampleRate: 44100, channels: 1);
			inputNode.InstallTapOnBus(0, 1024, recordingFormat, (buffer, when) => {
				recognitionRequest?.Append(buffer);
			});

			audioEngine.Prepare();
			audioEngine.StartAndReturnError(out err);

			OnStateChanged(SpeechRecognizerState.Capturing);

			return tcs.Task.AsAsyncOperation();
		}

		public IAsyncAction StopRecognitionAsync()
		{
			recognitionRequest?.EndAudio();
			return Task.CompletedTask.AsAsyncAction();
		}

		public void Dispose()
		{
			speechRecognizer?.Dispose();
			audioEngine?.Dispose();
			recognitionRequest?.Dispose();
			recognitionTask?.Dispose();
		}
	}
}
#endif
