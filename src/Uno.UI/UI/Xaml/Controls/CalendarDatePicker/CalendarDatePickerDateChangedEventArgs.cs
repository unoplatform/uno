using System;

namespace Windows.UI.Xaml.Controls
{
	public sealed partial class CalendarDatePickerDateChangedEventArgs
	{
		internal CalendarDatePickerDateChangedEventArgs(DateTimeOffset? newDate, DateTimeOffset? oldDate)
		{
			NewDate = newDate;
			OldDate = oldDate;
		}

		public DateTimeOffset? NewDate { get; }
		public DateTimeOffset? OldDate { get; }
	}
}
