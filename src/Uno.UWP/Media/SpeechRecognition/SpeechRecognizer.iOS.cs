using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using AVFoundation;
using Foundation;
using Speech;
using Windows.Foundation;

namespace Windows.Media.SpeechRecognition;

public partial class SpeechRecognizer : IDisposable
{
	private AVAudioEngine _audioEngine = new AVAudioEngine();

	private SFSpeechRecognizer _speechRecognizer;
	private SFSpeechAudioBufferRecognitionRequest? _recognitionRequest;
	private SFSpeechRecognitionTask? _recognitionTask;
	private Timer? _endSilenceTimeout;
	private Timer? _initialSilenceTimeout;

	[MemberNotNull(nameof(_speechRecognizer))]
	private void InitializeSpeechRecognizer()
	{
		_speechRecognizer?.Dispose();
		_speechRecognizer = new SFSpeechRecognizer(new NSLocale(CurrentLanguage.LanguageTag));
	}

	public IAsyncOperation<SpeechRecognitionResult> RecognizeAsync()
	{
		_initialSilenceTimeout = new Timer();
		_initialSilenceTimeout.Interval = Math.Max(Timeouts.InitialSilenceTimeout.TotalMilliseconds, 5000);
		_initialSilenceTimeout.Elapsed += OnTimeout;

		_endSilenceTimeout = new Timer();
		_endSilenceTimeout.Interval = Math.Max(Timeouts.EndSilenceTimeout.TotalMilliseconds, 150);
		_endSilenceTimeout.Elapsed += OnTimeout;

		// Cancel the previous task if it's running.
		_recognitionTask?.Cancel();
		_recognitionTask = null;

		var audioSession = AVAudioSession.SharedInstance();
		NSError? err;
		err = audioSession.SetCategory(AVAudioSessionCategory.Record);
#if NET8_0_OR_GREATER
		audioSession.SetMode(AVAudioSessionMode.Measurement, out err);
#else
		audioSession.SetMode(AVAudioSession.ModeMeasurement, out err);
#endif
		err = audioSession.SetActive(true, AVAudioSessionSetActiveOptions.NotifyOthersOnDeactivation);

		// Configure request to get partial results
		_recognitionRequest = new SFSpeechAudioBufferRecognitionRequest
		{
			ShouldReportPartialResults = true,
			TaskHint = SFSpeechRecognitionTaskHint.Dictation
		};

		var inputNode = _audioEngine.InputNode;
		if (inputNode == null)
		{
			throw new InvalidProgramException("Audio engine has no input node");
		}

		var tcs = new TaskCompletionSource<SpeechRecognitionResult>();

		// Keep a reference to the task so that it can be cancelled.
		_recognitionTask = _speechRecognizer.GetRecognitionTask(_recognitionRequest, (result, error) =>
		{
			var isFinal = false;
			var bestMatch = default(SpeechRecognitionResult);

			if (result != null)
			{
				_initialSilenceTimeout.Stop();
				_endSilenceTimeout.Stop();
				_endSilenceTimeout.Start();

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
				_initialSilenceTimeout.Stop();
				_endSilenceTimeout.Stop();

				_audioEngine.Stop();

				inputNode.RemoveTapOnBus(0);
				inputNode.Reset();

				audioSession = AVAudioSession.SharedInstance();
				err = audioSession.SetCategory(AVAudioSessionCategory.Playback);
#if NET8_0_OR_GREATER
				audioSession.SetMode(AVAudioSessionMode.Default, out err);
#else
				audioSession.SetMode(AVAudioSession.ModeDefault, out err);
#endif
				err = audioSession.SetActive(false, AVAudioSessionSetActiveOptions.NotifyOthersOnDeactivation);

				_recognitionTask = null;

				OnStateChanged(SpeechRecognizerState.Idle);

				if (bestMatch != null)
				{
					tcs.TrySetResult(bestMatch);
				}
				else
				{
					tcs.TrySetException(new Exception($"Error during speech recognition: {error!.LocalizedDescription}"));
				}
			}
		});

		var recordingFormat = new AVAudioFormat(sampleRate: 44100, channels: 1);
		inputNode.InstallTapOnBus(0, 1024, recordingFormat, (buffer, when) =>
		{
			_recognitionRequest?.Append(buffer);
		});

		_initialSilenceTimeout.Start();

		_audioEngine.Prepare();
		_audioEngine.StartAndReturnError(out err);

		OnStateChanged(SpeechRecognizerState.Capturing);

		return tcs.Task.AsAsyncOperation();
	}

	private void OnTimeout(object? sender, ElapsedEventArgs e)
	{
		StopRecognition();
	}

	private void StopRecognition()
	{
		_recognitionRequest?.EndAudio();
		_recognitionRequest?.Dispose();
		_recognitionRequest = null;
	}

	public IAsyncAction StopRecognitionAsync()
	{
		StopRecognition();
		return Task.CompletedTask.AsAsyncAction();
	}

	public void Dispose()
	{
		_speechRecognizer?.Dispose();
		_audioEngine?.Dispose();
		_recognitionRequest?.Dispose();
		_recognitionTask?.Dispose();
		_initialSilenceTimeout?.Dispose();
		_endSilenceTimeout?.Dispose();
	}
}
