#nullable disable

using System;

namespace Windows.UI.Xaml.Controls
{
	public partial class TimePickerValueChangedEventArgs
	{
		public TimeSpan OldTime { get; }
		public TimeSpan NewTime { get; }

		internal TimePickerValueChangedEventArgs(TimeSpan oldTime, TimeSpan newTime)
		{
			OldTime = oldTime;
			NewTime = newTime;
		}
	}
}
