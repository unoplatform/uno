using System;
using System.Collections.Generic;
using System.Text;
using Android.OS;
using Java.Lang;

namespace Microsoft.UI.Xaml
{
	partial class DispatcherTimer
	{
		private Handler _handler;
		private TickRunnable _runnable;

		partial void NativeCtor()
		{
			_handler = new Handler(Looper.MainLooper);
			_runnable = new TickRunnable(this);
		}

		private void StartNative(TimeSpan interval)
		{
			var longInterval = (long)interval.TotalMilliseconds;

			_runnable.Interval = longInterval;
			_handler.PostDelayed(_runnable, longInterval);
		}

		private void StartNative(TimeSpan dueTime, TimeSpan interval)
		{
			_runnable.Interval = (long)interval.TotalMilliseconds;
			_handler.PostDelayed(_runnable, (long)dueTime.TotalMilliseconds);
		}

		private void StopNative()
		{
			_runnable.Interval = -1;
			_handler.RemoveCallbacks(_runnable);
		}

		private class TickRunnable : Java.Lang.Object, IRunnable
		{
			private readonly DispatcherTimer _timer;

			public long Interval { get; set; }

			public TickRunnable(DispatcherTimer timer)
			{
				_timer = timer;
			}

			public void Run()
			{
				var interval = Interval;
				if (interval >= 0)
				{
					_timer.RaiseTick();

					interval = Interval;
					if (interval >= 0) // be sure to not self restart if the timer was Stopped by the Tick event handler
					{
						_timer._handler.PostDelayed(this, interval);
					}
				}
			}
		}
	}
}
