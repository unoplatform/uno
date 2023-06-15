using System;
using System.Diagnostics;
using System.Threading;
using Uno;
using Uno.Foundation.Logging;

namespace Windows.UI.Xaml;

/// <summary>
/// Provides a timer that is integrated into the Dispatcher queue, which is processed at a specified interval of time and at a specified priority.
/// </summary>
public partial class DispatcherTimer : IDispatcherTimer
{
	private static class States
	{
		public const int Idle = 0;
		public const int Running = 1;
	}

	private readonly Stopwatch _stopwatch = new();

	private int _state = States.Idle;
	private TimeSpan _interval;


	/// <summary>
	/// Initializes a new instance of the DispatcherTimer class. 
	/// </summary>
	public DispatcherTimer()
	{
		InitializePlatform();
	}

	partial void InitializePlatform();

	/// <summary>
	/// Occurs when the timer interval has elapsed.
	/// </summary>
	public event EventHandler<object> Tick;

	/// <summary>
	/// Gets or sets the amount of time between timer ticks.
	/// </summary>
	public TimeSpan Interval
	{
		get => _interval;
		set
		{
			if (_interval != value)
			{
				_interval = value;
				if (IsEnabled)
				{
					Restart(value);
				}
			}
		}
	}

	/// <summary>
	/// Gets a value that indicates whether the timer is running.
	/// </summary>
	public bool IsEnabled => _state == States.Running;

	internal TimeSpan LastTickElapsed { get; private set; }

	/// <summary>
	/// Starts the DispatcherTimer.
	/// </summary>
	public void Start()
	{
		LastTickElapsed = TimeSpan.Zero;
		if (Interlocked.CompareExchange(ref _state, States.Running, States.Idle) == States.Idle)
		{
			StartNative(Interval);
		}
		else
		{
			// As of 2018-04-24: "If the timer has already started, then it is restarted."
			// https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.dispatchertimer.start#Windows_UI_Xaml_DispatcherTimer_Start
			StopNative();
			StartNative(Interval);
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
			RaiseTick();

			if (IsEnabled) // be sure to not self restart if the timer was Stopped by the Tick event handler
			{
				StartNative(interval);
			}
		}
		else
		{
			StartNative(interval - elapsed, interval);
		}
	}

	private void RaiseTick()
	{
		var continueTicking = true;
		try
		{
			if (IsEnabled)
			{
				LastTickElapsed = _stopwatch.Elapsed;

				Tick?.Invoke(this, null);
			}
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"Unhandled exception in {nameof(DispatcherTimer)}.{nameof(Tick)}", ex);
			}
			continueTicking = false;
		}

		OnTickFinished(continueTicking);
	}

	partial void OnTickFinished(bool continueTicking);
}
