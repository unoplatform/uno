using System;
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls
{
	partial class CalendarDatePicker
	{
		private event EventHandler<object> _opened;

		public event EventHandler<object> Opened
		{
			add => _opened += value;
			remove => _opened -= value;
		}

		private event EventHandler<object> _closed;

		public event EventHandler<object> Closed
		{
			add => _closed += value;
			remove => _closed -= value;
		}

		public event TypedEventHandler<CalendarDatePicker, CalendarDatePickerDateChangedEventArgs> _dateChanged;

		public event TypedEventHandler<CalendarDatePicker, CalendarDatePickerDateChangedEventArgs> DateChanged
		{
			add => _dateChanged += value;
			remove => _dateChanged -= value;
		}
	}
}
