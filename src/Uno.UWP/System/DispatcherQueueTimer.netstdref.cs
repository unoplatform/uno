using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System.Threading;

namespace Windows.System
{
	partial class DispatcherQueueTimer
	{
		private void StartNative(TimeSpan interval)
		{
			throw new NotImplementedException("DispatcherQueueTimer not supported");
		}

		private void StartNative(TimeSpan dueTime, TimeSpan interval)
		{
			throw new NotImplementedException("DispatcherQueueTimer not supported");
		}

		private void StopNative()
		{
			throw new NotImplementedException("DispatcherQueueTimer not supported");
		}
	}
}
