using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace Uno.UI.RemoteControl.VS.AppLaunch;

/// <summary>
/// State machine that determines when an application is considered "launched"
/// based on a sequence of IDE-originating events (Play pressed, Build begins, Build completes).
/// </summary>
internal sealed class VsAppLaunchStateService<TStateDetails> : IDisposable
{
	/// <summary>
	/// Configuration options for the state machine.
	/// </summary>
	public sealed class Options
	{
		/// <summary>
		/// Time window during which a build is expected to start after Play is pressed.
		/// Default is 8 seconds.
		/// </summary>
		public TimeSpan BuildWaitWindow { get; init; } = TimeSpan.FromSeconds(8);

		/// <summary>
		/// How long to retain an "app launched" notification received before a Register/Start so it can
		/// be matched later. Default is 30 seconds.
		/// </summary>
		public TimeSpan LaunchedRetention { get; init; } = TimeSpan.FromSeconds(30);
	}

	/// <summary>
	/// High-level states for the launch tracking lifecycle.
	/// </summary>
	public enum LaunchState
	{
		/// <summary>
		/// No active cycle is in progress.
		/// </summary>
		Idle,

		/// <summary>
		/// The Play command was invoked and we are waiting briefly for a build to begin.
		/// </summary>
		PlayInvokedPendingBuild,

		/// <summary>
		/// A solution build is currently running for this cycle.
		/// </summary>
		BuildInProgress,

		/// <summary>
		/// The build completed successfully. In this service, this is a transient outcome that immediately returns to Idle.
		/// </summary>
		BuildSucceeded,

		/// <summary>
		/// The build completed with errors. Transient outcome that immediately returns to Idle.
		/// </summary>
		BuildFailed,

		/// <summary>
		/// No build began within the wait window after Play. Transient outcome that immediately returns to Idle.
		/// </summary>
		TimedOut
	}

	private readonly TimeProvider _timeProvider;
	private readonly Options _options;
	private ITimer? _timer;
	// Retained launched notifications for details that arrived before a Register/Start.
	// We keep an immutable collection of (details, launchedAt, timer) and update it atomically using
	// ImmutableInterlocked.Update to ensure thread-safety without locking. Expected size is small.
	private ImmutableArray<(TStateDetails? Details, DateTimeOffset LaunchedAt, ITimer? Timer)> _launchedEntries
		= ImmutableArray<(TStateDetails?, DateTimeOffset, ITimer?)>.Empty;
	private static readonly IEqualityComparer<TStateDetails?> _detailsComparer = EqualityComparer<TStateDetails?>.Default;

	/// <summary>
	/// Immutable snapshot of the current cycle and state.
	/// Contains the correlation StateDetails and current high-level state.
	/// </summary>
	private record struct Snapshot(TStateDetails? StateDetails, LaunchState State, DateTimeOffset? StartTimestampUtc);

	private Snapshot _snapshot = new Snapshot(default, LaunchState.Idle, null);

	/// <summary>
	/// Current state of the state machine.
	/// </summary>
	public LaunchState State => _snapshot.State;

	/// <summary>
	/// Single event raised on every state change. Includes the correlated StateDetails and basic context.
	/// </summary>
	public event EventHandler<StateChangedEventArgs<TStateDetails>>? StateChanged;

	public VsAppLaunchStateService(TimeProvider? timeProvider = null, Options? options = null)
	{
		_timeProvider = timeProvider ?? TimeProvider.System;
		_options = options ?? new Options();
	}

	/// <summary>
	/// Signal a Play (Run) intent with correlation details chosen by the caller.
	/// Starts a new cycle for these details and opens a short wait window for a build to begin.
	/// </summary>
	public void Start(TStateDetails details)
	{
		// Cancel any previous wait timer and initialize the new cycle for the provided details.
		try { _timer?.Dispose(); } catch { }
		_timer = null;

		// If we have a retained launched entry for these details, compute the register->launched delay
		// (launchedAt - registerTime). Note: if the launchedAt is earlier than registerTime this will be negative,
		// which is expected per requirements.
		TimeSpan? registerToLaunchedDelay = null;
		var registerTime = _timeProvider.GetUtcNow();
		// Atomically remove a retained entry for these details (if any) and capture the removed value.
		(bool removed, DateTimeOffset LaunchedAt, ITimer? Timer) removedEntry = (false, default, null);
		ImmutableInterlocked.Update(ref _launchedEntries, current =>
		{
			var idx = -1;
			for (var i = 0; i < current.Length; i++)
			{
				if (_detailsComparer.Equals(current[i].Details, details))
				{
					idx = i;
					break;
				}
			}
			if (idx < 0) return current;
			removedEntry = (true, current[idx].LaunchedAt, current[idx].Timer);
			return current.RemoveAt(idx);
		});
		if (removedEntry.removed)
		{
			registerToLaunchedDelay = removedEntry.LaunchedAt - registerTime;
			try { removedEntry.Timer?.Dispose(); } catch { }
		}

		SetState(LaunchState.PlayInvokedPendingBuild, details, startTimestampUtc: registerTime, registerToLaunchedDelay: registerToLaunchedDelay);
		ResetTimer(startNew: true);
	}

	/// <summary>
	/// Notify that the application has launched (e.g., the runtime reported back) at the given timestamp.
	/// This may arrive before or after a Register/Start. If it arrives earlier it will be retained for
	/// <see cref="Options.LaunchedRetention"/> and matched when a Register/Start occurs. If it arrives while a
	/// cycle is active, an event will be emitted that includes the computed delay.
	/// </summary>
	public void NotifyLaunched(TStateDetails details, DateTimeOffset launchedAtUtc)
	{
		// If there is an active snapshot for the same details, compute delay vs the snapshot's start timestamp
		// and emit an event immediately so listeners can correlate the two.
		if (_detailsComparer.Equals(details, _snapshot.StateDetails) && _snapshot.StartTimestampUtc is { } startTs)
		{
			var delay = launchedAtUtc - startTs;
			// Raise an informational StateChanged event preserving the current state but including the delay.
			StateChanged?.Invoke(this, new StateChangedEventArgs<TStateDetails>(_timeProvider.GetUtcNow(), _snapshot.State, _snapshot.State, _snapshot.StateDetails, delay));
			return;
		}

		// Otherwise retain it until a Register/Start or until scavenging.
		// Replace any existing retained entry for these details.
		// Atomically remove any existing entry for these details and dispose its timer.
		ImmutableInterlocked.Update(ref _launchedEntries, current =>
		{
			var idx = -1;
			for (var i = 0; i < current.Length; i++)
			{
				if (_detailsComparer.Equals(current[i].Details, details))
				{
					idx = i;
					break;
				}
			}
			if (idx < 0) return current;
			try { current[idx].Timer?.Dispose(); } catch { }
			return current.RemoveAt(idx);
		});

		// Create a scavenging timer to remove the entry after the retention window.
		ITimer? thisTimer = null;
		thisTimer = _timeProvider.CreateTimer(_ =>
		{
			try
			{
				// Remove the entry matching details (if still present) and dispose its timer.
				ImmutableInterlocked.Update(ref _launchedEntries, current =>
					{
						var idx = -1;
						for (var i = 0; i < current.Length; i++)
						{
							if (_detailsComparer.Equals(current[i].Details, details))
							{
								idx = i;
								break;
							}
						}
						if (idx < 0) return current;
						try { current[idx].Timer?.Dispose(); } catch { }
						return current.RemoveAt(idx);
					});
			}
			finally
			{
				try { thisTimer?.Dispose(); } catch { }
			}
		}, null, dueTime: _options.LaunchedRetention, period: Timeout.InfiniteTimeSpan);

		ImmutableInterlocked.Update(ref _launchedEntries, current => current.Add((details, launchedAtUtc, thisTimer)));
	}

	/// <summary>
	/// Notify a build-related event for the given StateDetails.
	/// Events for unrelated details are ignored.
	/// </summary>
	public void NotifyBuild(TStateDetails details, BuildNotification notification)
	{
		if (!_detailsComparer.Equals(details, _snapshot.StateDetails))
		{
			return; // ignore events for other details
		}

		switch (notification)
		{
			case BuildNotification.Began:
				if (State == LaunchState.PlayInvokedPendingBuild)
				{
					// Preserve the original correlated details instance when transitioning to BuildInProgress
					SetState(LaunchState.BuildInProgress, _snapshot.StateDetails, startTimestampUtc: _snapshot.StartTimestampUtc);
					ResetTimer(startNew: false);
				}
				break;

			case BuildNotification.Canceled:
				Reset();
				break;

			case BuildNotification.CompletedSuccess:
				// Ensure the BuildSucceeded event carries the same correlated details
				SetState(LaunchState.BuildSucceeded, _snapshot.StateDetails, startTimestampUtc: _snapshot.StartTimestampUtc);
				Reset();
				break;

			case BuildNotification.CompletedFailure:
				// Ensure the BuildFailed event carries the same correlated details
				SetState(LaunchState.BuildFailed, _snapshot.StateDetails, startTimestampUtc: _snapshot.StartTimestampUtc);
				Reset();
				break;
		}
	}


	/// <summary>
	/// Resets the state machine back to Idle and cancels any outstanding timers.
	/// </summary>
	public void Reset()
	{
		try { _timer?.Dispose(); } catch { }
		_timer = null;
		// Clear active StateDetails and emit Idle with no correlation details.
		SetState(LaunchState.Idle, default, startTimestampUtc: null);
	}

	public void Dispose() => Reset();

	private void SetState(LaunchState state, TStateDetails? details = default, DateTimeOffset? startTimestampUtc = null, TimeSpan? registerToLaunchedDelay = null)
	{
		var prev = _snapshot.State;
		var prevDetails = _snapshot.StateDetails;
		var prevStartTs = _snapshot.StartTimestampUtc;
		if (prev == state && _detailsComparer.Equals(details, prevDetails) && Nullable.Equals(startTimestampUtc, prevStartTs))
		{
			return;
		}

		if (state == LaunchState.Idle)
		{
			// When transitioning back to Idle, set the snapshot to the provided details so listeners
			// receive the correlated StateDetails for the cycle that just ended. Callers (Reset)
			// may clear the snapshot afterwards.
			_snapshot = new Snapshot(details, LaunchState.Idle, startTimestampUtc);
		}
		else
		{
			_snapshot = new Snapshot(details ?? prevDetails, state, startTimestampUtc ?? prevStartTs);
		}

		StateChanged?.Invoke(this, new StateChangedEventArgs<TStateDetails>(_timeProvider.GetUtcNow(), prev, state, _snapshot.StateDetails, registerToLaunchedDelay));
	}

	private void ResetTimer(bool startNew)
	{
		// Cancel any existing timer
		try { _timer?.Dispose(); } catch { }
		_timer = null;

		if (!startNew)
		{
			return;
		}

		var capturedDetails = _snapshot.StateDetails;

		// Create a timer whose callback executes the timeout logic directly.
		// Using the TimeProvider's CreateTimer allows FakeTimeProvider.Advance(...) in tests to
		// synchronously trigger the callback, avoiding Task.Run/await boundaries and
		// making tests deterministic.
		ITimer? thisTimer = null;
		thisTimer = _timeProvider.CreateTimer(state =>
		{
			try
			{
				if (State == LaunchState.PlayInvokedPendingBuild && _detailsComparer.Equals(capturedDetails, _snapshot.StateDetails))
				{
					SetState(LaunchState.TimedOut, capturedDetails);
					Reset();
				}
			}
			finally
			{
				// Dispose only the timer instance that scheduled this callback.
				try { thisTimer?.Dispose(); } catch { }
				// Clear the field only if it still references this timer to avoid disposing a newer one.
				if (object.ReferenceEquals(_timer, thisTimer))
				{
					_timer = null;
				}
			}
		}, null, dueTime: _options.BuildWaitWindow, period: Timeout.InfiniteTimeSpan);
		_timer = thisTimer;
	}
}

internal enum BuildNotification
{
	Began,
	Canceled,
	CompletedSuccess,
	CompletedFailure
}

internal sealed class StateChangedEventArgs<TStateDetails>(
	DateTimeOffset timestampUtc,
	VsAppLaunchStateService<TStateDetails>.LaunchState previous,
	VsAppLaunchStateService<TStateDetails>.LaunchState current,
	TStateDetails? details,
	TimeSpan? registerToLaunchedDelay = null)
	: EventArgs
{
	public DateTimeOffset TimestampUtc { get; } = timestampUtc;
	public VsAppLaunchStateService<TStateDetails>.LaunchState Previous { get; } = previous;
	public VsAppLaunchStateService<TStateDetails>.LaunchState Current { get; } = current;
	public TStateDetails? StateDetails { get; } = details;

	/// <summary>
	/// If not null, indicates a delay computed between register/start and a previously received launched notification.
	/// **May be negative** if the launched event arrived earlier than register time.
	/// </summary>
	public TimeSpan? RegisterToLaunchedDelay { get; } = registerToLaunchedDelay;
	public bool BuildSucceeded => Current == VsAppLaunchStateService<TStateDetails>.LaunchState.BuildSucceeded;
}
