using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
using DateTime = Windows.Foundation.WindowsFoundationDateTime;

namespace Windows.UI.Xaml.Controls.Primitives
{
	public sealed partial class CalendarViewTemplateSettings : DependencyObject
	{
		/// <summary>Gets the minimum width of the view.</summary>
		/// <returns>The minimum width of the view.</returns>
		public double MinViewWidth
		{
			get => (double)GetValue(MinViewWidthProperty);
			internal set => SetValue(MinViewWidthProperty, value);
		}

		private static readonly DependencyProperty MinViewWidthProperty = DependencyProperty.Register(
			"MinViewWidth",
			typeof(double),
			typeof(CalendarViewTemplateSettings),
			new FrameworkPropertyMetadata(default(double)));

		/// <summary>Gets the text of the header.</summary>
		/// <returns>The text of the header.</returns>
		public string HeaderText
		{
			get => (string)GetValue(HeaderTextProperty);
			internal set => SetValue(HeaderTextProperty, value);
		}

		private static readonly DependencyProperty HeaderTextProperty = DependencyProperty.Register(
			"HeaderText",
			typeof(string),
			typeof(CalendarViewTemplateSettings),
			new FrameworkPropertyMetadata(string.Empty));

		/// <summary>Gets the first day of the week.</summary>
		/// <returns>The first day of the week.</returns>
		public string WeekDay1
		{
			get => (string)GetValue(WeekDay1Property);
			internal set => SetValue(WeekDay1Property, value);
		}

		private static readonly DependencyProperty WeekDay1Property = DependencyProperty.Register(
			"WeekDay1",
			typeof(string),
			typeof(CalendarViewTemplateSettings),
			new FrameworkPropertyMetadata(string.Empty));

		/// <summary>Gets the second day of the week.</summary>
		/// <returns>The second day of the week.</returns>
		public string WeekDay2
		{
			get => (string)GetValue(WeekDay2Property);
			internal set => SetValue(WeekDay2Property, value);
		}

		private static readonly DependencyProperty WeekDay2Property = DependencyProperty.Register(
			"WeekDay2",
			typeof(string),
			typeof(CalendarViewTemplateSettings),
			new FrameworkPropertyMetadata(string.Empty));

		/// <summary>Gets the third day of the week.</summary>
		/// <returns>The third day of the week.</returns>
		public string WeekDay3
		{
			get => (string)GetValue(WeekDay3Property);
			internal set => SetValue(WeekDay3Property, value);
		}

		private static readonly DependencyProperty WeekDay3Property = DependencyProperty.Register(
			"WeekDay3",
			typeof(string),
			typeof(CalendarViewTemplateSettings),
			new FrameworkPropertyMetadata(string.Empty));

		/// <summary>Gets the fourth day of the week.</summary>
		/// <returns>The fourth day of the week.</returns>
		public string WeekDay4
		{
			get => (string)GetValue(WeekDay4Property);
			internal set => SetValue(WeekDay4Property, value);
		}

		private static readonly DependencyProperty WeekDay4Property = DependencyProperty.Register(
			"WeekDay4",
			typeof(string),
			typeof(CalendarViewTemplateSettings),
			new FrameworkPropertyMetadata(string.Empty));

		/// <summary>Gets the fifth day of the week.</summary>
		/// <returns>The fifth day of the week.</returns>
		public string WeekDay5
		{
			get => (string)GetValue(WeekDay5Property);
			internal set => SetValue(WeekDay5Property, value);
		}

		private static readonly DependencyProperty WeekDay5Property = DependencyProperty.Register(
			"WeekDay5",
			typeof(string),
			typeof(CalendarViewTemplateSettings),
			new FrameworkPropertyMetadata(string.Empty));

		/// <summary>Gets the sixth day of the week.</summary>
		/// <returns>The sixth day of the week.</returns>
		public string WeekDay6
		{
			get => (string)GetValue(WeekDay6Property);
			internal set => SetValue(WeekDay6Property, value);
		}

		private static readonly DependencyProperty WeekDay6Property = DependencyProperty.Register(
			"WeekDay6",
			typeof(string),
			typeof(CalendarViewTemplateSettings),
			new FrameworkPropertyMetadata(string.Empty));

		/// <summary>Gets the seventh day of the week.</summary>
		/// <returns>The seventh day of the week.</returns>
		public string WeekDay7
		{
			get => (string)GetValue(WeekDay7Property);
			internal set => SetValue(WeekDay7Property, value);
		}

		private static readonly DependencyProperty WeekDay7Property = DependencyProperty.Register(
			"WeekDay7",
			typeof(string),
			typeof(CalendarViewTemplateSettings),
			new FrameworkPropertyMetadata(string.Empty));

		/// <summary>Gets a value that indicates whether the CalendarView has more content after the displayed content.</summary>
		/// <returns>**true** if the CalendarView has more content after the displayed content; otherwise, **false**.</returns>
		public bool HasMoreContentAfter
		{
			get => (bool)GetValue(HasMoreContentAfterProperty);
			internal set => SetValue(HasMoreContentAfterProperty, value);
		}

		private static readonly DependencyProperty HasMoreContentAfterProperty = DependencyProperty.Register(
			"HasMoreContentAfter",
			typeof(bool),
			typeof(CalendarViewTemplateSettings),
			new FrameworkPropertyMetadata(default(bool)));

		/// <summary>Gets a value that indicates whether the CalendarView has more content before the displayed content.</summary>
		/// <returns>**true** if the CalendarView has more content after the displayed content; otherwise, **false**.</returns>
		public bool HasMoreContentBefore
		{
			get => (bool)GetValue(HasMoreContentBeforeProperty);
			internal set => SetValue(HasMoreContentBeforeProperty, value);
		}

		private static readonly DependencyProperty HasMoreContentBeforeProperty = DependencyProperty.Register(
			"HasMoreContentBefore",
			typeof(bool),
			typeof(CalendarViewTemplateSettings),
			new FrameworkPropertyMetadata(default(bool)));

		/// <summary>Gets a value that indicates whether the CalendarView has more views (like year or decade) that can be shown.</summary>
		/// <returns>**true** if the CalendarView has more views (like year or decade) that can be shown; otherwise, **false**.</returns>
		public bool HasMoreViews
		{
			get => (bool)GetValue(HasMoreViewsProperty);
			internal set => SetValue(HasMoreViewsProperty, value);
		}

		private static readonly DependencyProperty HasMoreViewsProperty = DependencyProperty.Register(
			"HasMoreViews",
			typeof(bool),
			typeof(CalendarViewTemplateSettings),
			new FrameworkPropertyMetadata(default(bool)));

		/// <summary>Gets the rectangle used to clip the CalendarView.</summary>
		/// <returns>The rectangle used to clip the CalendarView.</returns>
		public Rect ClipRect
		{
			get => (Rect)GetValue(ClipRectProperty);
			internal set => SetValue(ClipRectProperty, value);
		}

		private static readonly DependencyProperty ClipRectProperty = DependencyProperty.Register(
			"ClipRect",
			typeof(Rect),
			typeof(CalendarViewTemplateSettings),
			new FrameworkPropertyMetadata(default(Rect)));

		/// <summary>Gets the X coordinate of the CalendarView 's center point.</summary>
		/// <returns>The X coordinate of the CalendarView 's center point.</returns>
		public double CenterX
		{
			get => (double)GetValue(CenterXProperty);
			internal set => SetValue(CenterXProperty, value);
		}

		private static readonly DependencyProperty CenterXProperty = DependencyProperty.Register(
			"CenterX",
			typeof(double),
			typeof(CalendarViewTemplateSettings),
			new FrameworkPropertyMetadata(default(double)));

		/// <summary>Gets the Y coordinate of the CalendarView 's center point.</summary>
		/// <returns>The Y coordinate of the CalendarView 's center point.</returns>
		public double CenterY
		{
			get => (double)GetValue(CenterYProperty);
			internal set => SetValue(CenterYProperty, value);
		}

		private static readonly DependencyProperty CenterYProperty = DependencyProperty.Register(
			"CenterY",
			typeof(double),
			typeof(CalendarViewTemplateSettings),
			new FrameworkPropertyMetadata(default(double)));
	}
}
