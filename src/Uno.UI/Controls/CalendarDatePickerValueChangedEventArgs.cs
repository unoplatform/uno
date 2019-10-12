using System;

namespace Windows.UI.Xaml.Controls
{
	public partial class CalendarDatePickerDateChangedEventArgs
	{
		public CalendarDatePickerDateChangedEventArgs(Nullable<DateTimeOffset> newDate, Nullable<DateTimeOffset> oldDate)
		{
			NewDate = newDate;
			OldDate = oldDate;
		}

		public Nullable<DateTimeOffset> NewDate { get; }
		public Nullable<DateTimeOffset> OldDate { get; }
	}
}
