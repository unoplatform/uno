using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.Logging;
using Windows.Foundation;
using DateTimeOffset = System.DateTimeOffset;

namespace Windows.System
{
	public partial class DispatcherQueueTimer 
	{
		private static class States
		{
			public const int Idle = 0;
			public const int Running = 1;
		}

		private int _state = States.Idle;
		private TimeSpan _interval;

		public event TypedEventHandler<DispatcherQueueTimer, object> Tick;

		public TimeSpan Interval
		{
			get => _interval;
			set
			{
				if (value < TimeSpan.Zero)
				{
					throw new ArgumentException(nameof(value)); // WinUI throws a ArgumentException, not and ArgumentOutOfRangeException
				}

				if (_interval != value)
				{
					_interval = value;
					if (IsRunning)
					{
						Restart(value);
					}
				}
			}
		}

		public bool IsRunning => _state == States.Running;

		public bool IsRepeating { get; set; } = true;

		/// <summary>
		/// Gets a timestamp of the last start (or restart) of this timer.
		/// </summary>
		internal DateTimeOffset StartTimestamp { get; private set; }

		/// <summary>
		/// The absolute date at which the last tick event have been raised by this timer.
		/// NOTE:
		///		This is updated **BEFORE** the event is being raised.
		///		It can safely be used to get the absolute time in the Tick event handler.
		/// </summary>
		internal DateTimeOffset LastTickTimestamp { get; private set; }

		/// <summary>
		/// Gets the difference between <see cref="LastTickTimestamp"/> and <see cref="StartTimestamp"/>.
		/// WARNING: This is not protected from overflow! You must not access it before first Tick!
		/// </summary>
		internal TimeSpan LastTickElapsed
		{
			get
			{
				var elapsed = LastTickTimestamp - StartTimestamp;

				return elapsed > TimeSpan.Zero
					? elapsed
					: TimeSpan.Zero;
			}
		}

		/// <summary>
		/// An internal state that can be used to store a value in order to prevent a closure in the click handler.
		/// </summary>
		internal object State { get; set; }

		internal DispatcherQueueTimer()
		{
			NativeCtor();
		}

		partial void NativeCtor();

		public void Start()
		{
			StartTimestamp = DateTimeOffset.UtcNow;

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
			StartTimestamp = DateTimeOffset.UtcNow;

			// First be sure to stop the pending timer
			StopNative();

			var elapsed = StartTimestamp - LastTickTimestamp;
			if (elapsed >= interval)
			{
				RaiseTick(isTickForRestart: true);

				if (IsRunning) // be sure to not self restart if the timer was Stopped by the Tick event handler
				{
					StartNative(interval);
				}
			}
			else
			{
				StartNative(interval - elapsed, interval);
			}
		}

		private void RaiseTick(bool isTickForRestart = false)
		{
			try
			{
				var isRunning = IsRunning;

				if (isRunning && !isTickForRestart && !IsRepeating)
				{
					Stop();
				}

				if (isRunning)
				{
					LastTickTimestamp = DateTimeOffset.UtcNow;

					Tick?.Invoke(this, null);
				}
			}
			catch (Exception e)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error("Raising tick failed", e);
				}
			}
		}

		~DispatcherQueueTimer()
		{
			Stop();
		}
	}
}
