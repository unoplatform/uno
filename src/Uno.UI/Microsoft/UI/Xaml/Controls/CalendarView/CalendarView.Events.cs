using Windows.Foundation;

namespace Windows.UI.Xaml.Controls
{
	partial class CalendarView
	{
		private TypedEventHandler<CalendarView, CalendarViewDayItemChangingEventArgs> _calendarViewDayItemChanging;
		public event TypedEventHandler<CalendarView, CalendarViewDayItemChangingEventArgs> CalendarViewDayItemChanging
		{
			add => _calendarViewDayItemChanging += value;
			remove => _calendarViewDayItemChanging -= value;
		}

		private event TypedEventHandler<CalendarView, CalendarViewSelectedDatesChangedEventArgs> _selectedDatesChanged;

		public event TypedEventHandler<CalendarView, CalendarViewSelectedDatesChangedEventArgs> SelectedDatesChanged
		{
			add => _selectedDatesChanged += value;
			remove => _selectedDatesChanged -= value;
		}
	}
}
