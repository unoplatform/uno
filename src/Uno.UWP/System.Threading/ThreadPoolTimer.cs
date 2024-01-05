using System;
using System.Threading;

namespace Windows.System.Threading
{
	public partial class ThreadPoolTimer
	{
		private readonly Timer _timer;
		private readonly TimerDestroyedHandler? _destroyed;
		private CancellationTokenSource _cts = new CancellationTokenSource();

		public TimeSpan Delay { get; }

		public TimeSpan Period { get; }

		public void Cancel()
		{
			_cts.Cancel();
			_timer.Dispose();
			_destroyed?.Invoke(this);
		}

		internal ThreadPoolTimer(TimerElapsedHandler handler, TimeSpan? period = null, TimeSpan? delay = null, TimerDestroyedHandler? destroyed = null)
		{
			_timer = new Timer(_ =>
			{
				if (!_cts.IsCancellationRequested)
				{
					handler.Invoke(this);
				}
			});

			_destroyed = destroyed;

			_timer.Change(
				period: period ?? global::System.Threading.Timeout.InfiniteTimeSpan,
				dueTime: delay ?? TimeSpan.Zero
			);
		}

		public static ThreadPoolTimer CreatePeriodicTimer(TimerElapsedHandler handler, TimeSpan period)
			=> new ThreadPoolTimer(handler, period: period);

		public static ThreadPoolTimer CreateTimer(TimerElapsedHandler handler, TimeSpan delay)
			=> new ThreadPoolTimer(handler, delay: delay);

		public static ThreadPoolTimer CreatePeriodicTimer(TimerElapsedHandler handler, TimeSpan period, TimerDestroyedHandler destroyed)
			=> new ThreadPoolTimer(handler, period: period, destroyed: destroyed);

		public static ThreadPoolTimer CreateTimer(TimerElapsedHandler handler, TimeSpan delay, TimerDestroyedHandler destroyed)
			=> new ThreadPoolTimer(handler, delay: delay, destroyed: destroyed);
	}
}
