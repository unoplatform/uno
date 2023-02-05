using System;
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls
{
	partial class CalendarDatePicker
	{
		public event EventHandler<object> Opened;

		public event EventHandler<object> Closed;

		public event TypedEventHandler<CalendarDatePicker, CalendarDatePickerDateChangedEventArgs> DateChanged;

		private protected override void OnUnloaded()
		{
			// Ensure flyout is closed when the control is unloaded
			IsCalendarOpen = false;

			base.OnUnloaded();
		}
	}
}
