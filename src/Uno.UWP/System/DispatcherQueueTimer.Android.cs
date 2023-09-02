using System;
using Android.OS;
using Java.Lang;

#if HAS_UNO_WINUI && IS_UNO_UI_DISPATCHING_PROJECT
namespace Microsoft.UI.Dispatching;
#else
namespace Windows.System;
#endif

partial class DispatcherQueueTimer
{
	private Handler _handler;
	private TickRunnable _runnable;

	partial void InitializePlatform()
	{
		_handler = new Handler(Looper.MainLooper);
		_runnable = new TickRunnable(this);
	}

	private void ScheduleTickNative(TimeSpan dueTime) => _handler.PostDelayed(_runnable, (long)dueTime.TotalMilliseconds);

	private void StopNative() => _handler.RemoveCallbacks(_runnable);

	private class TickRunnable : Java.Lang.Object, IRunnable
	{
		private readonly DispatcherQueueTimer _timer;

		public TickRunnable(DispatcherQueueTimer timer)
		{
			_timer = timer;
		}

		public void Run()
		{
			if (_timer.IsRunning)
			{
				_timer.RaiseTick();
			}
		}
	}
}
