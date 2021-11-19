using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#if HAS_UNO_WINUI && IS_UNO_UI_DISPATCHING_PROJECT
namespace Microsoft.UI.Dispatching
#else
namespace Windows.System
#endif
{
	partial class DispatcherQueueTimer
	{
		Timer _timer;

		private void StartNative(TimeSpan interval)
		{
			if(_timer == null)
			{
				_timer = new Timer(_ => RaiseTick());
			}

			_timer.Change(interval, interval);
		}

		private void StartNative(TimeSpan dueTime, TimeSpan interval)
		{
			if (_timer == null)
			{
				_timer = new Timer(_ => RaiseTick());
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
