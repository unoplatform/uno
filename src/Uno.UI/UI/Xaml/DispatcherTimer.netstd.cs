using System;
using System.Diagnostics;
using System.Threading;
using Windows.UI.Core;

namespace Windows.UI.Xaml;

partial class DispatcherTimer
{
	private readonly TimeSpan _minInterval = TimeSpan.FromMilliseconds(1);

	private Timer _timer;

	private void StartNative(TimeSpan interval)
	{
		_timer ??= new Timer(_ => DispatchRaiseTick());
		_timer.Change(ClampInterval(interval), ClampInterval(interval));
	}

	private void StartNative(TimeSpan dueTime, TimeSpan interval)
	{
		_timer ??= new Timer(_ => DispatchRaiseTick());
		_timer.Change(ClampInterval(dueTime), ClampInterval(interval));
	}

	private void DispatchRaiseTick()
	{
#if __WASM__
		if (!dispatcher.IsThreadingSupported)
		{
			RaiseTick();
			return;
		}
#endif
		CoreDispatcher.Main.RunAsync(CoreDispatcherPriority.Normal, RaiseTick);
	}

	private void StopNative()
	{
		_timer.Dispose();
		_timer = null;
	}

	/// <summary>
	/// Timer class does not support zero interval.
	/// This clamps it to at least single tick.
	/// </summary>
	/// <param name="interval">Interval.</param>
	/// <returns>Clamped interval.</returns>
	private TimeSpan ClampInterval(TimeSpan interval) =>
		interval <= _minInterval ?
			_minInterval :
			interval;
}
