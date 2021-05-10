using System;
using Windows.Foundation;
using SelectedDatesChangedEventSourceType = Windows.Foundation.TypedEventHandler<Windows.UI.Xaml.Controls.CalendarView, Windows.UI.Xaml.Controls.CalendarViewSelectedDatesChangedEventArgs>;

namespace Windows.UI.Xaml.Controls
{
	partial class CalendarView
	{
		void GetSelectedDatesChangedEventSourceNoRef(out SelectedDatesChangedEventSourceType ppEventSource)
		{
			throw new NotImplementedException("UNO-TODO");
		}

		public void GetCalendarViewDayItemChangingEventSourceNoRef(out TypedEventHandler<CalendarView, CalendarViewDayItemChangingEventArgs> pEventSource)
		{
			throw new NotImplementedException("UNO-TODO");
		}
	}
}
