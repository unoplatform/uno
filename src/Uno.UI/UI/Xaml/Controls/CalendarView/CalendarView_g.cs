using System;
using Windows.Foundation;
using SelectedDatesChangedEventSourceType = Windows.Foundation.TypedEventHandler<Windows.UI.Xaml.Controls.CalendarView, Windows.UI.Xaml.Controls.CalendarViewSelectedDatesChangedEventArgs>;

namespace Windows.UI.Xaml.Controls
{
	partial class CalendarView
	{
		// UNO: Most parts of that files are in the uno's generated CalendarView.Properties.cs

		internal void GetCalendarViewDayItemChangingEventSourceNoRef(out TypedEventHandler<CalendarView, CalendarViewDayItemChangingEventArgs> pEventSource)
		{
			pEventSource = CalendarViewDayItemChanging;
		}

		public event TypedEventHandler<CalendarView, CalendarViewDayItemChangingEventArgs> CalendarViewDayItemChanging;

		private void GetSelectedDatesChangedEventSourceNoRef(out SelectedDatesChangedEventSourceType ppEventSource)
		{
			ppEventSource = SelectedDatesChanged;
		}

		public event SelectedDatesChangedEventSourceType SelectedDatesChanged;

		// UNO: The correspondent internal/impl method has been exposed publicly
		//public void SetDisplayDate( global::System.DateTimeOffset date)
		//{
		//	CheckThread();
		//	SetDisplayDateImpl();
		//}

		// UNO: The correspondent internal/impl method has been exposed publicly
		//public void SetYearDecadeDisplayDimensions( int columns,  int rows)
		//{
		//	CheckThread();
		//	SetYearDecadeDisplayDimensionsImpl();
		//}
	}
}
