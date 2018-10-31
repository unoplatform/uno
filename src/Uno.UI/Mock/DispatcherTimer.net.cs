using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.UI.Xaml
{
	partial class DispatcherTimer
	{
		private void StartNative(TimeSpan interval)
		{
			throw new NotSupportedException();
		}

		private void StartNative(TimeSpan dueTime, TimeSpan interval)
		{
			throw new NotSupportedException();
		}

		private void StopNative()
		{
			throw new NotSupportedException();
		}
	}
}
