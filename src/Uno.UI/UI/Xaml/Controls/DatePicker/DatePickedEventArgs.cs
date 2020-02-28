using System;

namespace Windows.UI.Xaml.Controls
{
	public partial class DatePickedEventArgs
	{
		public DatePickedEventArgs(DateTimeOffset newDate, DateTimeOffset oldDate)
		{
			NewDate = newDate;
			OldDate = oldDate;
		}

		public DateTimeOffset NewDate { get; }
		public DateTimeOffset OldDate { get; }
	}
}
