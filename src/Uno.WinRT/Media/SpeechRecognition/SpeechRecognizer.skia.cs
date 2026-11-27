#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Windows.Foundation;
using Windows.Globalization;

namespace Windows.Media.SpeechRecognition;

public partial class SpeechRecognizer : IDisposable
{
	// WinRT reports a declined speech privacy policy with this HRESULT.
	internal const int SpeechPrivacyPolicyNotAcceptedHResult = unchecked((int)0x80045509);

	// How long the audio must stay quiet before SoundEnded is reported.
	private static readonly TimeSpan SoundEndedDelay = TimeSpan.FromMilliseconds(150);

	private readonly object _gate = new();
	private readonly Queue<Action> _pendingEvents = new();
	private readonly AudioLevelTracker _audioLevels = new();
	private TimeProvider _timeProvider = TimeProvider.System;
	private ISpeechRecognizerExtension? _extension;
	private bool _isLanguageSupported;
	private bool _isFlushingEvents;
	private bool _isDisposed;

	private SessionKind _session;
	private int _utteranceId;
	private bool _isUtteranceActive;
	private UtteranceEndReason _endReason;
	private bool _isSpeechDetected;
	private bool _isSoundActive;
	private DateTimeOffset? _quietSince;
	private ITimer? _initialSilenceTimer;
	private ITimer? _endSilenceTimer;
	private ITimer? _babbleTimer;
	private TaskCompletionSource<SpeechRecognitionResult>? _recognitionCompletion;
	private TaskCompletionSource? _idleCompletion;

	internal SpeechRecognizer(Language language, ISpeechRecognizerExtension extension, TimeProvider timeProvider)
	{
		CurrentLanguage = language;
		_extension = extension;
		_timeProvider = timeProvider;
		InitializeSpeechRecognizer();
	}

	private void InitializeSpeechRecognizer()
	{
		if (_extension is null && ApiExtensibility.CreateInstance<ISpeechRecognizerExtension>(this, out var extension))
		{
			_extension = extension;
		}

		if (_extension is null)
		{
			return;
		}

		_extension.HypothesisGenerated += OnExtensionHypothesisGenerated;
		_extension.SpeechDetected += OnExtensionSpeechDetected;
		_extension.AudioCaptureEnded += OnExtensionAudioCaptureEnded;
		_extension.AudioLevelChanged += OnExtensionAudioLevelChanged;
		_extension.UtteranceCompleted += OnExtensionUtteranceCompleted;
		_isLanguageSupported = _extension.Initialize(CurrentLanguage);
	}

	public IAsyncOperation<SpeechRecognitionCompilationResult> CompileConstraintsAsync()
		=> AsyncOperation.FromTask(_ => Task.FromResult(new SpeechRecognitionCompilationResult { Status = CompileConstraints() }));

	public IAsyncOperation<SpeechRecognitionResult> RecognizeAsync()
		=> AsyncOperation.FromTask(RecognizeCoreAsync);

	public IAsyncAction StopRecognitionAsync()
		=> AsyncAction.FromTask(_ => StopRecognitionCoreAsync());

	public void Dispose()
	{
		ISpeechRecognizerExtension? extension;
		lock (_gate)
		{
			if (_isDisposed)
			{
				return;
			}

			_isDisposed = true;
			if (_isUtteranceActive)
			{
				EndUtteranceLocked();
			}

			if (_session == SessionKind.Single)
			{
				CompleteRecognitionLocked(null);
			}

			_session = SessionKind.None;
			extension = _extension;
			_extension = null;
		}

		FlushEvents();

		if (extension is not null)
		{
			extension.HypothesisGenerated -= OnExtensionHypothesisGenerated;
			extension.SpeechDetected -= OnExtensionSpeechDetected;
			extension.AudioCaptureEnded -= OnExtensionAudioCaptureEnded;
			extension.AudioLevelChanged -= OnExtensionAudioLevelChanged;
			extension.UtteranceCompleted -= OnExtensionUtteranceCompleted;
			extension.Dispose();
		}
	}

	private SpeechRecognitionResultStatus CompileConstraints()
	{
		lock (_gate)
		{
			ThrowIfDisposedLocked();
			if (State is not (SpeechRecognizerState.Idle or SpeechRecognizerState.Paused))
			{
				throw new InvalidOperationException("Constraints can only be compiled while the speech recognizer is idle or paused.");
			}

			return _extension is null || _isLanguageSupported
				? SpeechRecognitionResultStatus.Success
				: SpeechRecognitionResultStatus.TopicLanguageNotSupported;
		}
	}

	private async Task<SpeechRecognitionResult> RecognizeCoreAsync(CancellationToken ct)
	{
		var extension = _extension ?? throw new NotSupportedException("SpeechRecognizer is not available on this platform.");

		lock (_gate)
		{
			ThrowIfDisposedLocked();
			if (State != SpeechRecognizerState.Idle || _session != SessionKind.None)
			{
				throw new InvalidOperationException("RecognizeAsync can only be called while the speech recognizer is idle.");
			}

			_session = SessionKind.Single;
		}

		SpeechRecognitionResultStatus status;
		try
		{
			status = await PrepareRecognitionAsync(extension);
		}
		catch
		{
			lock (_gate)
			{
				_session = SessionKind.None;
			}

			throw;
		}

		var completion = new TaskCompletionSource<SpeechRecognitionResult>(TaskCreationOptions.RunContinuationsAsynchronously);
		lock (_gate)
		{
			ThrowIfDisposedLocked();
			if (status != SpeechRecognitionResultStatus.Success)
			{
				_session = SessionKind.None;
				return CreateEmptyResult(status);
			}

			_recognitionCompletion = completion;
			BeginUtteranceLocked();
		}

		FlushEvents();

		using var registration = ct.Register(CancelRecognition);
		return await completion.Task;
	}

	private async Task<SpeechRecognitionResultStatus> PrepareRecognitionAsync(ISpeechRecognizerExtension extension)
	{
		if (!_isLanguageSupported)
		{
			return SpeechRecognitionResultStatus.TopicLanguageNotSupported;
		}

		switch (await extension.RequestAuthorizationAsync())
		{
			case SpeechRecognizerAuthorization.Denied:
				throw new COMException(
					"The speech privacy policy was not accepted: speech recognition or microphone access was denied.",
					SpeechPrivacyPolicyNotAcceptedHResult);

			case SpeechRecognizerAuthorization.MissingUsageDescription:
				throw new COMException(
					"Speech recognition cannot request permission because the app does not declare why it needs it. " +
					"On macOS, add NSSpeechRecognitionUsageDescription and NSMicrophoneUsageDescription to the app bundle's Info.plist.",
					SpeechPrivacyPolicyNotAcceptedHResult);
		}

		return SpeechRecognitionResultStatus.Success;
	}

	private Task StopRecognitionCoreAsync()
	{
		Task idle;
		lock (_gate)
		{
			if (_session != SessionKind.Single || !_isUtteranceActive)
			{
				return Task.CompletedTask;
			}

			_idleCompletion ??= new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			idle = _idleCompletion.Task;

			if (_endReason == UtteranceEndReason.None)
			{
				_endReason = UtteranceEndReason.Stopped;
				if (_isSpeechDetected)
				{
					FinishUtteranceLocked();
				}
				else
				{
					_extension!.CancelUtterance();
				}
			}
		}

		FlushEvents();
		return idle;
	}

	private void CancelRecognition()
	{
		lock (_gate)
		{
			if (_session != SessionKind.Single || !_isUtteranceActive)
			{
				return;
			}

			_endReason = UtteranceEndReason.Aborted;
			_extension!.CancelUtterance();
		}
	}

	private void BeginUtteranceLocked()
	{
		_utteranceId++;
		_isUtteranceActive = true;
		_endReason = UtteranceEndReason.None;
		_isSpeechDetected = false;
		_isSoundActive = false;
		_quietSince = null;
		_audioLevels.Reset();

		SetStateLocked(SpeechRecognizerState.Capturing);
		_extension!.BeginUtterance(new SpeechUtteranceOptions(SpeechTaskHint.Dictation, Array.Empty<string>()));
		_initialSilenceTimer = StartTimerLocked(Timeouts.InitialSilenceTimeout, OnInitialSilenceTimeoutLocked);
	}

	// Asks the engine for the final result of the audio captured so far.
	private void FinishUtteranceLocked()
	{
		if (_isSoundActive)
		{
			_isSoundActive = false;
			SetStateLocked(SpeechRecognizerState.SoundEnded);
		}

		SetStateLocked(SpeechRecognizerState.Processing);
		_extension!.EndUtterance();
	}

	private void EndUtteranceLocked()
	{
		_isUtteranceActive = false;
		StopTimer(ref _initialSilenceTimer);
		StopTimer(ref _endSilenceTimer);
		StopTimer(ref _babbleTimer);
	}

	private void CompleteRecognitionLocked(SpeechRecognitionResult? result)
	{
		_session = SessionKind.None;
		_extension?.StopAudio();
		SetStateLocked(SpeechRecognizerState.Idle);

		var completion = _recognitionCompletion;
		var idle = _idleCompletion;
		_recognitionCompletion = null;
		_idleCompletion = null;

		EnqueueEventLocked(() =>
		{
			if (result is null)
			{
				completion?.TrySetCanceled();
			}
			else
			{
				completion?.TrySetResult(result);
			}

			idle?.TrySetResult();
		});
	}

	private void OnInitialSilenceTimeoutLocked()
	{
		_endReason = UtteranceEndReason.InitialSilenceTimeout;
		_extension!.CancelUtterance();
	}

	private void OnBabbleTimeoutLocked()
	{
		_endReason = UtteranceEndReason.BabbleTimeout;
		_extension!.CancelUtterance();
	}

	private void OnEndSilenceTimeoutLocked()
	{
		_endReason = UtteranceEndReason.EndSilenceTimeout;
		FinishUtteranceLocked();
	}

	private void OnSpeechDetectedLocked()
	{
		if (_isSpeechDetected)
		{
			return;
		}

		_isSpeechDetected = true;
		if (!_isSoundActive)
		{
			_isSoundActive = true;
			SetStateLocked(SpeechRecognizerState.SoundStarted);
		}

		SetStateLocked(SpeechRecognizerState.SpeechDetected);
		StopTimer(ref _initialSilenceTimer);
		StopTimer(ref _babbleTimer);
		RestartEndSilenceTimerLocked();
	}

	private void RestartEndSilenceTimerLocked()
	{
		var timeout = Timeouts.EndSilenceTimeout;
		if (timeout <= TimeSpan.Zero)
		{
			return;
		}

		if (_endSilenceTimer is null)
		{
			_endSilenceTimer = StartTimerLocked(timeout, OnEndSilenceTimeoutLocked);
		}
		else
		{
			_endSilenceTimer.Change(timeout, Timeout.InfiniteTimeSpan);
		}
	}

	private void OnExtensionHypothesisGenerated(object? sender, string text)
	{
		lock (_gate)
		{
			if (!_isUtteranceActive)
			{
				return;
			}

			if (!string.IsNullOrEmpty(text) && _endReason == UtteranceEndReason.None)
			{
				OnSpeechDetectedLocked();
				RestartEndSilenceTimerLocked();
			}

			EnqueueEventLocked(() => OnHypothesisGenerated(text));
		}

		FlushEvents();
	}

	private void OnExtensionSpeechDetected(object? sender, EventArgs e)
	{
		lock (_gate)
		{
			if (_isUtteranceActive && _endReason == UtteranceEndReason.None)
			{
				OnSpeechDetectedLocked();
			}
		}

		FlushEvents();
	}

	private void OnExtensionAudioCaptureEnded(object? sender, EventArgs e)
	{
		lock (_gate)
		{
			if (_isUtteranceActive && _session == SessionKind.Single && _endReason == UtteranceEndReason.None)
			{
				_endReason = UtteranceEndReason.EngineEnded;
				SetStateLocked(SpeechRecognizerState.Processing);
			}
		}

		FlushEvents();
	}

	private void OnExtensionAudioLevelChanged(object? sender, SpeechAudioLevel level)
	{
		lock (_gate)
		{
			if (!_isUtteranceActive || _endReason != UtteranceEndReason.None)
			{
				return;
			}

			if (_audioLevels.IsSound(level.RmsDecibels))
			{
				_quietSince = null;
				if (!_isSoundActive)
				{
					_isSoundActive = true;
					SetStateLocked(SpeechRecognizerState.SoundStarted);
					if (!_isSpeechDetected && _babbleTimer is null)
					{
						_babbleTimer = StartTimerLocked(Timeouts.BabbleTimeout, OnBabbleTimeoutLocked);
					}
				}

				if (_isSpeechDetected)
				{
					RestartEndSilenceTimerLocked();
				}
			}
			else if (_isSoundActive)
			{
				var now = _timeProvider.GetUtcNow();
				_quietSince ??= now;
				if (now - _quietSince.Value >= SoundEndedDelay)
				{
					_isSoundActive = false;
					SetStateLocked(SpeechRecognizerState.SoundEnded);
				}
			}
		}

		FlushEvents();
	}

	private void OnExtensionUtteranceCompleted(object? sender, SpeechUtteranceOutcome outcome)
	{
		lock (_gate)
		{
			if (!_isUtteranceActive)
			{
				return;
			}

			var reason = _endReason;
			EndUtteranceLocked();

			if (_session == SessionKind.Single)
			{
				CompleteRecognitionLocked(reason == UtteranceEndReason.Aborted ? null : CreateResult(outcome, reason));
			}
		}

		FlushEvents();
	}

	private SpeechRecognitionResult CreateResult(SpeechUtteranceOutcome outcome, UtteranceEndReason reason)
	{
		if (reason is UtteranceEndReason.InitialSilenceTimeout or UtteranceEndReason.BabbleTimeout)
		{
			return CreateEmptyResult(SpeechRecognitionResultStatus.TimeoutExceeded);
		}

		if (outcome.Kind == SpeechUtteranceOutcomeKind.Success
			&& outcome.Transcriptions.Count > 0
			&& !string.IsNullOrEmpty(outcome.Transcriptions[0].Text))
		{
			return CreateRecognizedResult(outcome);
		}

		if (reason == UtteranceEndReason.Stopped)
		{
			return CreateEmptyResult(SpeechRecognitionResultStatus.UserCanceled);
		}

		return outcome.Kind switch
		{
			SpeechUtteranceOutcomeKind.Error => CreateEmptyResult(outcome.ErrorStatus),
			SpeechUtteranceOutcomeKind.Canceled => CreateEmptyResult(SpeechRecognitionResultStatus.UserCanceled),
			_ => CreateEmptyResult(SpeechRecognitionResultStatus.Success),
		};
	}

	private static SpeechRecognitionResult CreateRecognizedResult(SpeechUtteranceOutcome outcome)
	{
		var alternates = outcome.Transcriptions
			.Select(transcription => new SpeechRecognitionResult
			{
				Text = transcription.Text,
				RawConfidence = transcription.Confidence,
				Status = SpeechRecognitionResultStatus.Success,
			})
			.ToList();

		foreach (var alternate in alternates)
		{
			alternate.Alternates = alternates;
		}

		return alternates[0];
	}

	private static SpeechRecognitionResult CreateEmptyResult(SpeechRecognitionResultStatus status)
	{
		var result = new SpeechRecognitionResult
		{
			Text = string.Empty,
			Status = status,
		};
		result.Alternates = new[] { result };
		return result;
	}

	private ITimer? StartTimerLocked(TimeSpan dueTime, Action onElapsedLocked)
	{
		if (dueTime <= TimeSpan.Zero)
		{
			return null;
		}

		var utteranceId = _utteranceId;
		return _timeProvider.CreateTimer(
			_ =>
			{
				lock (_gate)
				{
					if (_utteranceId != utteranceId || !_isUtteranceActive || _endReason != UtteranceEndReason.None)
					{
						return;
					}

					onElapsedLocked();
				}

				FlushEvents();
			},
			null,
			dueTime,
			Timeout.InfiniteTimeSpan);
	}

	private static void StopTimer(ref ITimer? timer)
	{
		timer?.Dispose();
		timer = null;
	}

	private void SetStateLocked(SpeechRecognizerState state)
	{
		if (State == state)
		{
			return;
		}

		State = state;
		EnqueueEventLocked(() => OnStateChanged(state));
	}

	private void EnqueueEventLocked(Action raise) => _pendingEvents.Enqueue(raise);

	// Raises the queued events outside of the lock, in order, from a single thread at a time.
	private void FlushEvents()
	{
		lock (_gate)
		{
			if (_isFlushingEvents)
			{
				return;
			}

			_isFlushingEvents = true;
		}

		while (true)
		{
			Action? raise;
			lock (_gate)
			{
				if (!_pendingEvents.TryDequeue(out raise))
				{
					_isFlushingEvents = false;
					return;
				}
			}

			try
			{
				raise();
			}
			catch (Exception ex)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error("A SpeechRecognizer event handler failed.", ex);
				}
			}
		}
	}

	private void ThrowIfDisposedLocked()
	{
		if (_isDisposed)
		{
			throw new ObjectDisposedException(nameof(SpeechRecognizer));
		}
	}

	private enum SessionKind
	{
		None,
		Single,
	}

	private enum UtteranceEndReason
	{
		None,
		EndSilenceTimeout,
		InitialSilenceTimeout,
		BabbleTimeout,
		EngineEnded,
		Stopped,
		Aborted,
	}

	/// <summary>
	/// Tells sound from background noise by comparing the level to the quietest level of the last few seconds.
	/// </summary>
	private sealed class AudioLevelTracker
	{
		private const int WindowLength = 60; // About 3 seconds of 50 ms samples.
		private const int WarmUpLength = 10;
		private const float InitialNoiseFloorDecibels = -55f;
		private const float SoundMarginDecibels = 12f;
		private const float MinimumSoundDecibels = -60f;

		private readonly float[] _window = new float[WindowLength];
		private int _count;
		private int _next;

		public void Reset()
		{
			_count = 0;
			_next = 0;
		}

		public bool IsSound(float rmsDecibels)
		{
			var isSound = rmsDecibels > Math.Max(GetNoiseFloor() + SoundMarginDecibels, MinimumSoundDecibels);

			_window[_next] = rmsDecibels;
			_next = (_next + 1) % WindowLength;
			_count = Math.Min(_count + 1, WindowLength);

			return isSound;
		}

		private float GetNoiseFloor()
		{
			var floor = _count < WarmUpLength ? InitialNoiseFloorDecibels : float.MaxValue;
			for (var i = 0; i < _count; i++)
			{
				floor = Math.Min(floor, _window[i]);
			}

			return floor;
		}
	}
}
