using System;
using System.Collections.Generic;
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
		/// Time window during which a build is expected after Play is pressed.
		/// Default is 5 seconds.
		/// </summary>
		public TimeSpan BuildWaitWindow { get; init; } = TimeSpan.FromSeconds(5);
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
	private static readonly IEqualityComparer<TStateDetails?> _detailsComparer = EqualityComparer<TStateDetails?>.Default;

	/// <summary>
	/// Immutable snapshot of the current cycle and state.
	/// Contains the correlation StateDetails and current high-level state.
	/// </summary>
	private record struct Snapshot(TStateDetails? StateDetails, LaunchState State);

	private Snapshot _snapshot = new Snapshot(default, LaunchState.Idle);

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
		SetState(LaunchState.PlayInvokedPendingBuild, details);
		ResetTimer(startNew: true);
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
					SetState(LaunchState.BuildInProgress, details);
					ResetTimer(startNew: false);
				}
				break;

			case BuildNotification.Canceled:
				Reset();
				break;

			case BuildNotification.CompletedSuccess:
				SetState(LaunchState.BuildSucceeded, details);
				Reset();
				break;

			case BuildNotification.CompletedFailure:
				SetState(LaunchState.BuildFailed, details);
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
		SetState(LaunchState.Idle, default);
	}

	public void Dispose() => Reset();

	private void SetState(LaunchState state, TStateDetails? details)
	{
		var prev = _snapshot.State;
		var prevDetails = _snapshot.StateDetails;
		if (prev == state && _detailsComparer.Equals(details, prevDetails))
		{
			return;
		}

		if (state == LaunchState.Idle)
		{
			// When transitioning back to Idle, set the snapshot to the provided details so listeners
			// receive the correlated StateDetails for the cycle that just ended. Callers (Reset)
			// may clear the snapshot afterwards.
			_snapshot = new Snapshot(details, LaunchState.Idle);
		}
		else
		{
			_snapshot = new Snapshot(details, state);
		}

		StateChanged?.Invoke(this, new StateChangedEventArgs<TStateDetails>(_timeProvider.GetUtcNow(), prev, state, _snapshot.StateDetails));
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
	TStateDetails? details)
	: EventArgs
{
	public DateTimeOffset TimestampUtc { get; } = timestampUtc;
	public VsAppLaunchStateService<TStateDetails>.LaunchState Previous { get; } = previous;
	public VsAppLaunchStateService<TStateDetails>.LaunchState Current { get; } = current;
	public TStateDetails? StateDetails { get; } = details;
	public bool Succeeded => Current == VsAppLaunchStateService<TStateDetails>.LaunchState.BuildSucceeded;
}
