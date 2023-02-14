using System;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class TimePickerSelectedValueChangedEventArgs
	{
		public TimeSpan? OldTime { get; }
		public TimeSpan? NewTime { get; }

		internal TimePickerSelectedValueChangedEventArgs(TimeSpan? oldTime, TimeSpan? newTime)
		{
			OldTime = oldTime;
			NewTime = newTime;
		}
	}
}
