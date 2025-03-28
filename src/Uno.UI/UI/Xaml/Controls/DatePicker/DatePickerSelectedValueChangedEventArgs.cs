using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
	public sealed partial class DatePickerSelectedValueChangedEventArgs
	{
		internal DatePickerSelectedValueChangedEventArgs(
			DateTimeOffset? newDate = null,
			DateTimeOffset? oldDate = null)
		{
			NewDate = newDate;
			OldDate = oldDate;
		}

		public DateTimeOffset? NewDate { get; internal set; }
		public DateTimeOffset? OldDate { get; internal set; }
	}
}
