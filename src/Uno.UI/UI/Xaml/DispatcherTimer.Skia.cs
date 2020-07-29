using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Core;

namespace Windows.UI.Xaml
{
	partial class DispatcherTimer
	{
		Timer _timer;

		private void StartNative(TimeSpan interval)
		{
			if (_timer == null)
			{
				_timer = new Timer(_ => DispatchRaiseTick());
			}

			_timer.Change(interval, interval);
		}

		private void DispatchRaiseTick()
		{
			CoreDispatcher.Main.RunAsync(CoreDispatcherPriority.Normal, _ => RaiseTick());
		}

		private void StartNative(TimeSpan dueTime, TimeSpan interval)
		{
			if (_timer == null)
			{
				_timer = new Timer(_ => DispatchRaiseTick());
			}

			_timer.Change(dueTime, interval);
		}

		private void StopNative()
		{
			_timer.Dispose();
			_timer = null;
		}
	}
}
