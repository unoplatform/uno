using System;
using System.Diagnostics;
using System.Threading;
using Uno.Foundation.Logging;
using Windows.Foundation;

#if HAS_UNO_WINUI && IS_UNO_UI_DISPATCHING_PROJECT
namespace Microsoft.UI.Dispatching;
#else
namespace Windows.System;
#endif

/// <summary>
/// Periodically executes a task on a DispatcherQueue thread after a time interval has elapsed.
/// </summary>
public partial class DispatcherQueueTimer
{
	private static class States
	{
		public const int Idle = 0;
		public const int Running = 1;
	}

	private readonly Stopwatch _stopwatch = new();
	private readonly bool _simulateDispatcherTimer;
	private int _state = States.Idle;
	private TimeSpan _interval;

	/// <summary>
	/// Creates a new instance of DispatcherQueueTimer.
	/// </summary>
	/// <param name="simulateDispatcherTimer">Indicates whether the timer simulates DispatcherTimer behavior. In such case exceptions break the timer and interval is reset when it changes.</param>
	internal DispatcherQueueTimer(bool simulateDispatcherTimer = false)
	{
		InitializePlatform();
		_simulateDispatcherTimer = simulateDispatcherTimer;
	}

	partial void InitializePlatform();

	/// <summary>
	/// Occurs when the timer interval has elapsed.
	/// </summary>
	public event TypedEventHandler<DispatcherQueueTimer, object?>? Tick;

	/// <summary>
	/// Gets and sets the interval for the timer.
	/// </summary>
	public TimeSpan Interval
	{
		get => _interval;
		set
		{
			if (value < TimeSpan.Zero)
			{
				// WinUI throws a ArgumentException, not and ArgumentOutOfRangeException
				throw new ArgumentException("Interval must not be negative", nameof(value));
			}

			_interval = value;
			if (_simulateDispatcherTimer && IsRunning)
			{
				Restart(value);
			}
		}
	}

	/// <summary>
	/// Gets a value that indicates whether the timer is running.
	/// </summary>
	public bool IsRunning => _state == States.Running;

	/// <summary>
	/// Gets or sets a value indicating whether the timer is repeating.
	/// </summary>
	public bool IsRepeating { get; set; } = true;

	/// <summary>
	/// Gets the elapsed time on last <see cref="Tick"/> since the last time that this timer has been [re]-started.
	/// NOTE:
	///		This is updated **BEFORE** the event is being raised.
	///		It can safely be used to get the absolute time in the Tick event handler.
	/// </summary>
	internal TimeSpan LastTickElapsed { get; private set; }

	/// <summary>
	/// An internal state that can be used to store a value in order to prevent a closure in the click handler.
	/// </summary>
	internal object? State { get; set; }

	public void Start()
	{
		LastTickElapsed = TimeSpan.Zero;
		if (Interlocked.CompareExchange(ref _state, States.Running, States.Idle) == States.Idle)
		{
			ScheduleTickNative(Interval);
		}
		else
		{
			// As of 2018-04-24: "If the timer has already started, then it is restarted."
			// https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.dispatchertimer.start#Windows_UI_Xaml_DispatcherTimer_Start
			StopNative();

			ScheduleTickNative(Interval);
		}

		_stopwatch.Restart();
	}

	/// <summary>
	/// Stops the DispatcherTimer.
	/// </summary>
	public void Stop()
	{
		if (Interlocked.Exchange(ref _state, States.Idle) == States.Running)
		{
			StopNative();
		}

		_stopwatch.Reset();
	}

	/*
	 * Note: When we change the Interval on a timer while it's running, the new delay is applied immediately.
	 * This means that :
	 *  - if the new Interval is lower that delay since the last Tick, a Tick will occurs immediately
	 *  - if the new Interval is greater, next Tick will occur after the end of the new Interval.
	 */
	private void Restart(TimeSpan interval)
	{
		// First be sure to stop the pending timer
		StopNative();

		var elapsed = _stopwatch.Elapsed - LastTickElapsed;
		if (elapsed >= interval)
		{
			RaiseTick(isTickForRestart: true);

			if (IsRunning) // be sure to not self restart if the timer was Stopped by the Tick event handler
			{
				ScheduleTickNative(interval);
			}
		}
		else
		{
			ScheduleTickNative(interval - elapsed);
		}
	}

	private void RaiseTick(bool isTickForRestart = false)
	{
		var isRunning = IsRunning;

		if (isRunning && !isTickForRestart && !IsRepeating)
		{
			Stop();
		}

		bool exceptionOcurred = false;
		try
		{
			if (isRunning)
			{
				LastTickElapsed = _stopwatch.Elapsed;

				Tick?.Invoke(this, null);
			}
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"Unhandled exception in {nameof(DispatcherQueueTimer)}.{nameof(Tick)}", ex);
			}
			exceptionOcurred = true;
		}

		var ignoreExceptions = !_simulateDispatcherTimer;
		var shouldContinueTicking =
			(ignoreExceptions || !exceptionOcurred) &&
			isRunning &&
			IsRepeating;

		if (IsRunning && IsRepeating && shouldContinueTicking)
		{
			ScheduleTickNative(Interval);
		}
	}

	partial void ScheduleNextTick();

	~DispatcherQueueTimer()
	{
		Stop();
	}
}
