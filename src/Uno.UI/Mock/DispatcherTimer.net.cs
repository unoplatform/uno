using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.UI.Xaml
{
	partial class DispatcherTimer
	{
		private void StartNative(TimeSpan interval)
		{
			; // Not supported in unit tests
		}

		private void StartNative(TimeSpan dueTime, TimeSpan interval)
		{
			; // Not supported in unit tests
		}

		private void StopNative()
		{
			; // Not supported in unit tests
		}
	}
}
