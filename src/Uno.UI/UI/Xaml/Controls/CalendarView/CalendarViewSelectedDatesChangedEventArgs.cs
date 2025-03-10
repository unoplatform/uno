using System;
using System.Collections.Generic;

namespace Windows.UI.Xaml.Controls
{
	public partial class CalendarViewSelectedDatesChangedEventArgs
	{
		internal CalendarViewSelectedDatesChangedEventArgs()
		{
		}

		public IReadOnlyList<DateTimeOffset> AddedDates { get; internal set; }
		public IReadOnlyList<DateTimeOffset> RemovedDates { get; internal set; }
	}
}
