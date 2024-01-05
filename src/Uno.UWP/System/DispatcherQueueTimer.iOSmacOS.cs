using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Foundation;

#if HAS_UNO_WINUI && IS_UNO_UI_DISPATCHING_PROJECT
namespace Microsoft.UI.Dispatching;
#else
namespace Windows.System;
#endif

partial class DispatcherQueueTimer
{
	private NSTimer? _timer;

	private void ScheduleTickNative(TimeSpan interval)
	{
		Start(NSTimer.CreateScheduledTimer(interval.TotalSeconds, repeats: false, block: OnRecurentTick));
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

	private void OnRecurentTick(NSTimer timer)
	{
		if (_timer == timer)
		{
			RaiseTick();
		}
	}
}
