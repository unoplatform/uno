using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System.Threading;

#if HAS_UNO_WINUI && IS_UNO_UI_PROJECT
namespace Microsoft.UI.Dispatching
#else
namespace Windows.System
#endif
{
	partial class DispatcherQueueTimer
	{
		private void StartNative(TimeSpan interval)
		{
		}

		private void StartNative(TimeSpan dueTime, TimeSpan interval)
		{
		}

		private void StopNative()
		{
		}
	}
}
