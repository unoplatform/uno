using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
	public sealed partial class DatePickerSelectedValueChangedEventArgs
	{
		public DatePickerSelectedValueChangedEventArgs(DateTimeOffset? newDate, DateTimeOffset? oldDate)
		{
			NewDate = newDate;
			OldDate = oldDate;
		}

		public DateTimeOffset? NewDate { get; }
		public DateTimeOffset? OldDate { get; }
	}
}
