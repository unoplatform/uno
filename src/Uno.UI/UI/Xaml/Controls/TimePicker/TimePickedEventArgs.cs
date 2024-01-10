using System;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class TimePickedEventArgs
	{
		public TimePickedEventArgs()
		{
		}

		public TimePickedEventArgs(TimeSpan oldTime, TimeSpan newTime)
		{
			OldTime = oldTime;
			NewTime = newTime;
		}

		public TimeSpan OldTime { get; }
		public TimeSpan NewTime { get; }
	}
}
