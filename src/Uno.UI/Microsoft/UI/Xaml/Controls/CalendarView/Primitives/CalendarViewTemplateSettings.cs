using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
using DateTime = System.DateTimeOffset;

namespace Windows.UI.Xaml.Controls.Primitives
{
	public sealed partial class CalendarViewTemplateSettings : DependencyObject
	{
		/// <summary>Gets the minimum width of the view.</summary>
		/// <returns>The minimum width of the view.</returns>
		public double MinViewWidth { get; internal set; }
		/// <summary>Gets the text of the header.</summary>
		/// <returns>The text of the header.</returns>
		public string HeaderText { get; internal set; }
		/// <summary>Gets the first day of the week.</summary>
		/// <returns>The first day of the week.</returns>
		public string WeekDay1 { get; internal set; }
		/// <summary>Gets the second day of the week.</summary>
		/// <returns>The second day of the week.</returns>
		public string WeekDay2 { get; internal set; }
		/// <summary>Gets the third day of the week.</summary>
		/// <returns>The third day of the week.</returns>
		public string WeekDay3 { get; internal set; }
		/// <summary>Gets the fourth day of the week.</summary>
		/// <returns>The fourth day of the week.</returns>
		public string WeekDay4 { get; internal set; }
		/// <summary>Gets the fifth day of the week.</summary>
		/// <returns>The fifth day of the week.</returns>
		public string WeekDay5 { get; internal set; }
		/// <summary>Gets the sixth day of the week.</summary>
		/// <returns>The sixth day of the week.</returns>
		public string WeekDay6 { get; internal set; }
		/// <summary>Gets the seventh day of the week.</summary>
		/// <returns>The seventh day of the week.</returns>
		public string WeekDay7 { get; internal set; }
		/// <summary>Gets a value that indicates whether the CalendarView has more content after the displayed content.</summary>
		/// <returns>**true** if the CalendarView has more content after the displayed content; otherwise, **false**.</returns>
		public bool HasMoreContentAfter { get; internal set; }
		/// <summary>Gets a value that indicates whether the CalendarView has more content before the displayed content.</summary>
		/// <returns>**true** if the CalendarView has more content after the displayed content; otherwise, **false**.</returns>
		public bool HasMoreContentBefore { get; internal set; }
		/// <summary>Gets a value that indicates whether the CalendarView has more views (like year or decade) that can be shown.</summary>
		/// <returns>**true** if the CalendarView has more views (like year or decade) that can be shown; otherwise, **false**.</returns>
		public bool HasMoreViews { get; internal set; }
		/// <summary>Gets the rectangle used to clip the CalendarView.</summary>
		/// <returns>The rectangle used to clip the CalendarView.</returns>
		public Rect ClipRect { get; internal set; }
		/// <summary>Gets the X coordinate of the CalendarView 's center point.</summary>
		/// <returns>The X coordinate of the CalendarView 's center point.</returns>
		public double CenterX { get; internal set; }
		/// <summary>Gets the Y coordinate of the CalendarView 's center point.</summary>
		/// <returns>The Y coordinate of the CalendarView 's center point.</returns>
		public double CenterY { get; internal set; }
	}
}
