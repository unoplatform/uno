using System;
using System.Diagnostics;
using System.Threading;

using Uno.Extensions;
using Uno.Foundation.Logging;
using Windows.Foundation;
using DateTimeOffset = System.DateTimeOffset;

#if HAS_UNO_WINUI && IS_UNO_UI_DISPATCHING_PROJECT
namespace Microsoft.UI.Dispatching
#else
namespace Windows.System
#endif
{
	public partial class DispatcherQueueTimer 
	{
		private static class States
		{
			public const int Idle = 0;
			public const int Running = 1;
		}

		private readonly Stopwatch _elapsed = new Stopwatch();

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
		/// Gets the elapsed time on last <see cref="Tick"/> since the last time that this timer has been [re]-started.
		/// NOTE:
		///		This is updated **BEFORE** the event is being raised.
		///		It can safely be used to get the absolute time in the Tick event handler.
		/// </summary>
		internal TimeSpan LastTickElapsed { get; private set; }

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
			if (Interlocked.CompareExchange(ref _state, States.Running, States.Idle) == States.Idle)
			{
				_elapsed.Restart();
				StartNative(Interval);
			}
			else
			{
				// As of 2018-04-24: "If the timer has already started, then it is restarted."
				// https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.dispatchertimer.start#Windows_UI_Xaml_DispatcherTimer_Start
				StopNative();

				_elapsed.Restart();
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
			// First be sure to stop the pending timer
			StopNative();

			var elapsed = _elapsed.Elapsed - LastTickElapsed;
			if (elapsed >= interval)
			{
				RaiseTick(isTickForRestart: true);

				if (IsRunning) // be sure to not self restart if the timer was Stopped by the Tick event handler
				{
					_elapsed.Restart();
					StartNative(interval);
				}
			}
			else
			{
				_elapsed.Restart();
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
					LastTickElapsed = _elapsed.Elapsed;

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
