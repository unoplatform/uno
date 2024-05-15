using System;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class DatePickedEventArgs
	{
		public DatePickedEventArgs()
		{
		}

		internal DatePickedEventArgs(DateTimeOffset newDate, DateTimeOffset oldDate)
		{
			NewDate = newDate;
			OldDate = oldDate;
		}

		public DateTimeOffset NewDate { get; }
		public DateTimeOffset OldDate { get; }
	}
}
