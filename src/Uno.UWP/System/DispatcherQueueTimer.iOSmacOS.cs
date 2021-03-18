using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Foundation;

namespace Windows.System
{
    partial class DispatcherQueueTimer
	{
		private NSTimer _timer;

		private void StartNative(TimeSpan interval)
		{
			Start(NSTimer.CreateScheduledTimer(interval.TotalSeconds, repeats: IsRepeating, block: OnRecurentTick));
		}

		private void StartNative(TimeSpan dueTime, TimeSpan interval)
		{
			Start(NSTimer.CreateScheduledTimer(dueTime.TotalSeconds, repeats: false, block: OnUniqueTick));
		}

		private void Start(NSTimer timer)
		{
			Interlocked.Exchange(ref _timer, timer)?.Invalidate();
			NSRunLoop.Main.AddTimer(timer, NSRunLoopMode.Common);
		}

		private void StopNative()
		{
			Interlocked.Exchange(ref _timer, null)?.Invalidate();
		}

		private void OnUniqueTick(NSTimer timer)
		{
			if (_timer == timer)
			{
				RaiseTick();

				if (_timer == timer) // be sure to not self restart if the timer was Stopped by the Tick event handler
				{
					StartNative(Interval);
				}
			}
		}

		private void OnRecurentTick(NSTimer timer)
		{
			if (_timer == timer)
			{
				RaiseTick();
			}
		}
	}
}
