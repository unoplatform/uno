#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Windows.Media.SpeechRecognition;

public partial class SpeechRecognizer
{
	private const float ClippingPeakDecibels = -0.5f;
	private const int ClippedSamplesForTooLoud = 3;
	private const float SilentSignalDecibels = -100f;
	private const int SilentSamplesForNoSignal = 20; // About 1 second of 50 ms samples.
	private const float NoisyFloorDecibels = -35f;
	private const float QuietSpeechDecibels = -50f;

	private readonly HashSet<SpeechRecognitionAudioProblem> _reportedAudioProblems = new();
	private SpeechContinuousRecognitionSession? _continuousSession;
	private SpeechContinuousRecognitionMode _continuousMode;
	private SpeechRecognitionResultStatus? _pendingContinuousStop;
	private bool _isPauseRequested;
	private bool _isPaused;
	private bool _isContinuousStarting;
	private int _continuousStartId;
	private ITimer? _autoStopTimer;
	private TaskCompletionSource? _continuousCompletion;
	private TaskCompletionSource? _pauseCompletion;
	private int _clippedSamples;
	private int _silentSamples;
	private float _loudestLevel;

	public SpeechContinuousRecognitionSession ContinuousRecognitionSession
	{
		get
		{
			lock (_gate)
			{
				return _continuousSession ??= new SpeechContinuousRecognitionSession(this);
			}
		}
	}

	public event TypedEventHandler<SpeechRecognizer, SpeechRecognitionQualityDegradingEventArgs>? RecognitionQualityDegrading;

	internal async Task StartContinuousRecognitionAsync(SpeechContinuousRecognitionMode mode)
	{
		var extension = _extension ?? throw new NotSupportedException("SpeechRecognizer is not available on this platform.");

		int startId;
		lock (_gate)
		{
			ThrowIfDisposedLocked();
			if (State != SpeechRecognizerState.Idle || _session != SessionKind.None)
			{
				throw new InvalidOperationException("Continuous recognition can only be started while the speech recognizer is idle.");
			}

			_session = SessionKind.Continuous;
			_continuousMode = mode;
			_pendingContinuousStop = null;
			_isPauseRequested = false;
			_isPaused = false;
			_isContinuousStarting = true;
			_eventContext = SynchronizationContext.Current;
			startId = ++_continuousStartId;
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
				if (IsStartingLocked(startId))
				{
					_isContinuousStarting = false;
					_session = SessionKind.None;
				}
			}

			throw;
		}

		lock (_gate)
		{
			// The session may have been stopped, canceled or disposed while waiting for permission.
			if (!IsStartingLocked(startId))
			{
				return;
			}

			_isContinuousStarting = false;
			StartSessionLocked();

			if (status != SpeechRecognitionResultStatus.Success)
			{
				FinishContinuousRecognitionLocked(status);
			}
			else if (!_isPaused)
			{
				BeginUtteranceLocked();
				RestartAutoStopTimerLocked();
			}
		}

		FlushEvents();
	}

	internal Task StopContinuousRecognitionAsync(bool discardPendingResults)
	{
		Task completion;
		lock (_gate)
		{
			if (_session != SessionKind.Continuous)
			{
				return Task.CompletedTask;
			}

			_continuousCompletion ??= new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			completion = _continuousCompletion.Task;

			var status = discardPendingResults ? SpeechRecognitionResultStatus.UserCanceled : SpeechRecognitionResultStatus.Success;
			if (_isUtteranceActive)
			{
				if (_pendingContinuousStop != SpeechRecognitionResultStatus.UserCanceled)
				{
					_pendingContinuousStop = status;
				}

				EndContinuousUtteranceLocked(discardPendingResults);
			}
			else
			{
				FinishContinuousRecognitionLocked(status);
			}
		}

		FlushEvents();
		return completion;
	}

	internal Task PauseContinuousRecognitionAsync()
	{
		Task completion;
		lock (_gate)
		{
			if (_session != SessionKind.Continuous || _isPaused)
			{
				return Task.CompletedTask;
			}

			_pauseCompletion ??= new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			completion = _pauseCompletion.Task;
			_isPauseRequested = true;

			if (_isUtteranceActive)
			{
				EndContinuousUtteranceLocked(discard: false);
			}
			else
			{
				PauseContinuousRecognitionLocked();
			}
		}

		FlushEvents();
		return completion;
	}

	internal void ResumeContinuousRecognition()
	{
		lock (_gate)
		{
			if (_session != SessionKind.Continuous || !_isPaused)
			{
				return;
			}

			if (_isContinuousStarting)
			{
				// Paused while waiting for permission: the startup begins recognizing.
				_isPaused = false;
				SetStateLocked(SpeechRecognizerState.Idle);
				return;
			}

			// Constraints can be changed while paused.
			_isPaused = false;
			_activeConstraints = _compiledConstraints ?? ConstraintPlan.Create(Constraints);
			var status = GetCompilationStatus(_activeConstraints);
			if (status == SpeechRecognitionResultStatus.Success)
			{
				BeginUtteranceLocked();
				RestartAutoStopTimerLocked();
			}
			else
			{
				FinishContinuousRecognitionLocked(status);
			}
		}

		FlushEvents();
	}

	// Ends the current phrase: its result is kept unless discarded, or when nothing was said yet.
	private void EndContinuousUtteranceLocked(bool discard)
	{
		if (!discard && _endReason != UtteranceEndReason.None)
		{
			return;
		}

		_endReason = discard ? UtteranceEndReason.Aborted : UtteranceEndReason.Stopped;
		if (!discard && _isSpeechDetected)
		{
			FinishUtteranceLocked();
		}
		else
		{
			_extension!.CancelUtterance();
		}
	}

	private void OnContinuousUtteranceCompletedLocked(SpeechUtteranceOutcome outcome, UtteranceEndReason reason)
	{
		if (_pendingContinuousStop == SpeechRecognitionResultStatus.UserCanceled)
		{
			FinishContinuousRecognitionLocked(SpeechRecognitionResultStatus.UserCanceled);
			return;
		}

		var result = CreateResult(outcome, reason);
		var hasResult = result.Status == SpeechRecognitionResultStatus.Success;
		if (hasResult)
		{
			var session = _continuousSession;
			EnqueueEventLocked(() => session?.RaiseResultGenerated(result));
		}

		if (_pendingContinuousStop is { } stopStatus)
		{
			FinishContinuousRecognitionLocked(stopStatus);
		}
		else if (outcome.Kind == SpeechUtteranceOutcomeKind.Error && outcome.ErrorStatus != SpeechRecognitionResultStatus.TimeoutExceeded)
		{
			FinishContinuousRecognitionLocked(outcome.ErrorStatus);
		}
		else if (_isPauseRequested || (hasResult && _continuousMode == SpeechContinuousRecognitionMode.PauseOnRecognition))
		{
			PauseContinuousRecognitionLocked();
		}
		else
		{
			BeginUtteranceLocked();
		}
	}

	private void PauseContinuousRecognitionLocked()
	{
		_isPauseRequested = false;
		_isPaused = true;
		StopTimer(ref _autoStopTimer);
		SetStateLocked(SpeechRecognizerState.Paused);

		var completion = _pauseCompletion;
		_pauseCompletion = null;
		EnqueueEventLocked(() => completion?.TrySetResult());
	}

	private void FinishContinuousRecognitionLocked(SpeechRecognitionResultStatus status)
	{
		if (_isUtteranceActive)
		{
			EndUtteranceLocked();
			_extension?.CancelUtterance();
		}

		_session = SessionKind.None;
		_pendingContinuousStop = null;
		_isPauseRequested = false;
		_isPaused = false;
		_isContinuousStarting = false;
		StopTimer(ref _autoStopTimer);
		_extension?.StopAudio();
		SetStateLocked(SpeechRecognizerState.Idle);

		var session = _continuousSession;
		var completion = _continuousCompletion;
		var pauseCompletion = _pauseCompletion;
		_continuousCompletion = null;
		_pauseCompletion = null;
		EnqueueEventLocked(() =>
		{
			session?.RaiseCompleted(status);
			completion?.TrySetResult();
			pauseCompletion?.TrySetResult();
		});
	}

	private void RestartAutoStopTimerLocked()
	{
		var timeout = _continuousSession?.AutoStopSilenceTimeout ?? TimeSpan.Zero;
		if (_session != SessionKind.Continuous || timeout <= TimeSpan.Zero)
		{
			return;
		}

		if (_autoStopTimer is not null)
		{
			_autoStopTimer.Change(timeout, Timeout.InfiniteTimeSpan);
			return;
		}

		var sessionId = _sessionId;
		_autoStopTimer = _timeProvider.CreateTimer(
			_ => RunOnEventContext(() =>
			{
				lock (_gate)
				{
					if (_sessionId != sessionId || _session != SessionKind.Continuous || _isPaused || _pendingContinuousStop is not null)
					{
						return;
					}

					if (_isUtteranceActive)
					{
						_pendingContinuousStop = SpeechRecognitionResultStatus.TimeoutExceeded;
						EndContinuousUtteranceLocked(discard: false);
					}
					else
					{
						FinishContinuousRecognitionLocked(SpeechRecognitionResultStatus.TimeoutExceeded);
					}
				}

				FlushEvents();
			}),
			null,
			timeout,
			Timeout.InfiniteTimeSpan);
	}

	private bool IsStartingLocked(int startId)
		=> _isContinuousStarting && _continuousStartId == startId && _session == SessionKind.Continuous;

	private void ResetAudioQualityLocked()
	{
		_reportedAudioProblems.Clear();
		_clippedSamples = 0;
		_silentSamples = 0;
		_loudestLevel = float.NegativeInfinity;
	}

	private void TrackAudioQualityLocked(SpeechAudioLevel level)
	{
		_loudestLevel = Math.Max(_loudestLevel, level.RmsDecibels);

		_clippedSamples = level.PeakDecibels >= ClippingPeakDecibels ? _clippedSamples + 1 : 0;
		if (_clippedSamples >= ClippedSamplesForTooLoud)
		{
			ReportAudioProblemLocked(SpeechRecognitionAudioProblem.TooLoud);
		}

		_silentSamples = level.RmsDecibels <= SilentSignalDecibels ? _silentSamples + 1 : 0;
		if (_silentSamples >= SilentSamplesForNoSignal)
		{
			ReportAudioProblemLocked(SpeechRecognitionAudioProblem.NoSignal);
		}

		if (_audioLevels.IsWarmedUp && _audioLevels.NoiseFloorDecibels > NoisyFloorDecibels)
		{
			ReportAudioProblemLocked(SpeechRecognitionAudioProblem.TooNoisy);
		}
	}

	// Checked once the phrase is over: speech is often detected before its loudest part was measured.
	private void CheckSpeechLevelLocked()
	{
		if (!float.IsNegativeInfinity(_loudestLevel) && _loudestLevel < QuietSpeechDecibels)
		{
			ReportAudioProblemLocked(SpeechRecognitionAudioProblem.TooQuiet);
		}
	}

	private void ReportAudioProblemLocked(SpeechRecognitionAudioProblem problem)
	{
		if (_reportedAudioProblems.Add(problem))
		{
			EnqueueEventLocked(() => RecognitionQualityDegrading?.Invoke(this, new SpeechRecognitionQualityDegradingEventArgs(problem)));
		}
	}
}
