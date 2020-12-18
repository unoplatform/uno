using System;
using System.Linq;
using DateTime = System.DateTimeOffset;

namespace Windows.UI.Xaml.Controls
{
	partial class CalendarDatePicker
	{
		public static readonly DependencyProperty CalendarIdentifierProperty = DependencyProperty.Register(
			"CalendarIdentifier", typeof(string), typeof(CalendarDatePicker), new PropertyMetadata(default(string)));

		public string CalendarIdentifier
		{
			get { return (string)GetValue(CalendarIdentifierProperty); }
			set { SetValue(CalendarIdentifierProperty, value); }
		}

		public static readonly DependencyProperty CalendarViewStyleProperty = DependencyProperty.Register(
			"CalendarViewStyle", typeof(Style), typeof(CalendarDatePicker), new PropertyMetadata(default(Style)));

		public Style CalendarViewStyle
		{
			get { return (Style)GetValue(CalendarViewStyleProperty); }
			set { SetValue(CalendarViewStyleProperty, value); }
		}

		public static readonly DependencyProperty DateProperty = DependencyProperty.Register(
			"Date", typeof(DateTimeOffset?), typeof(CalendarDatePicker), new PropertyMetadata(default(DateTimeOffset)));

		public DateTimeOffset? Date
		{
			get { return (DateTimeOffset?)GetValue(DateProperty); }
			set { SetValue(DateProperty, value); }
		}

		public static readonly DependencyProperty DateFormatProperty = DependencyProperty.Register(
			"DateFormat", typeof(string), typeof(CalendarDatePicker), new PropertyMetadata(default(string)));

		public string DateFormat
		{
			get { return (string)GetValue(DateFormatProperty); }
			set { SetValue(DateFormatProperty, value); }
		}

		public static readonly DependencyProperty DayOfWeekFormatProperty = DependencyProperty.Register(
			"DayOfWeekFormat", typeof(string), typeof(CalendarDatePicker), new PropertyMetadata(default(string)));

		public string DayOfWeekFormat
		{
			get { return (string)GetValue(DayOfWeekFormatProperty); }
			set { SetValue(DayOfWeekFormatProperty, value); }
		}

		public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
			"Description", typeof(string), typeof(CalendarDatePicker), new PropertyMetadata(default(string)));

		public string Description
		{
			get { return (string)GetValue(DescriptionProperty); }
			set { SetValue(DescriptionProperty, value); }
		}

		public static readonly DependencyProperty DisplayModeProperty = DependencyProperty.Register(
			"DisplayMode", typeof(CalendarViewDisplayMode), typeof(CalendarDatePicker), new PropertyMetadata(default(CalendarViewDisplayMode)));

		public CalendarViewDisplayMode DisplayMode
		{
			get { return (CalendarViewDisplayMode)GetValue(DisplayModeProperty); }
			set { SetValue(DisplayModeProperty, value); }
		}

		public static readonly DependencyProperty FirstDayOfWeekProperty = DependencyProperty.Register(
			"FirstDayOfWeek", typeof(DayOfWeek), typeof(CalendarDatePicker), new PropertyMetadata(default(DayOfWeek)));

		public DayOfWeek FirstDayOfWeek
		{
			get { return (DayOfWeek)GetValue(FirstDayOfWeekProperty); }
			set { SetValue(FirstDayOfWeekProperty, value); }
		}

		public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
			"Header", typeof(object), typeof(CalendarDatePicker), new PropertyMetadata(default(object)));

		public object Header
		{
			get { return (object)GetValue(HeaderProperty); }
			set { SetValue(HeaderProperty, value); }
		}

		public static readonly DependencyProperty HeaderTemplateProperty = DependencyProperty.Register(
			"HeaderTemplate", typeof(DataTemplate), typeof(CalendarDatePicker), new PropertyMetadata(default(DataTemplate)));

		public DataTemplate HeaderTemplate
		{
			get { return (DataTemplate)GetValue(HeaderTemplateProperty); }
			set { SetValue(HeaderTemplateProperty, value); }
		}

		public static readonly DependencyProperty IsCalendarOpenProperty = DependencyProperty.Register(
			"IsCalendarOpen", typeof(bool), typeof(CalendarDatePicker), new PropertyMetadata(default(bool)));

		public bool IsCalendarOpen
		{
			get { return (bool)GetValue(IsCalendarOpenProperty); }
			set { SetValue(IsCalendarOpenProperty, value); }
		}

		public static readonly DependencyProperty IsGroupLabelVisibleProperty = DependencyProperty.Register(
			"IsGroupLabelVisible", typeof(bool), typeof(CalendarDatePicker), new PropertyMetadata(default(bool)));

		public bool IsGroupLabelVisible
		{
			get { return (bool)GetValue(IsGroupLabelVisibleProperty); }
			set { SetValue(IsGroupLabelVisibleProperty, value); }
		}

		public static readonly DependencyProperty IsOutOfScopeEnabledProperty = DependencyProperty.Register(
			"IsOutOfScopeEnabled", typeof(bool), typeof(CalendarDatePicker), new PropertyMetadata(default(bool)));

		public bool IsOutOfScopeEnabled
		{
			get { return (bool)GetValue(IsOutOfScopeEnabledProperty); }
			set { SetValue(IsOutOfScopeEnabledProperty, value); }
		}

		public static readonly DependencyProperty IsTodayHighlightedProperty = DependencyProperty.Register(
			"IsTodayHighlighted", typeof(bool), typeof(CalendarDatePicker), new PropertyMetadata(default(bool)));

		public bool IsTodayHighlighted
		{
			get { return (bool)GetValue(IsTodayHighlightedProperty); }
			set { SetValue(IsTodayHighlightedProperty, value); }
		}

		public static readonly DependencyProperty LightDismissOverlayModeProperty = DependencyProperty.Register(
			"LightDismissOverlayMode", typeof(LightDismissOverlayMode), typeof(CalendarDatePicker), new PropertyMetadata(default(LightDismissOverlayMode)));

		public LightDismissOverlayMode LightDismissOverlayMode
		{
			get { return (LightDismissOverlayMode)GetValue(LightDismissOverlayModeProperty); }
			set { SetValue(LightDismissOverlayModeProperty, value); }
		}

		public static readonly DependencyProperty MaxDateProperty = DependencyProperty.Register(
			"MaxDate", typeof(DateTimeOffset), typeof(CalendarDatePicker), new PropertyMetadata(default(DateTimeOffset)));

		public DateTimeOffset MaxDate
		{
			get { return (DateTimeOffset)GetValue(MaxDateProperty); }
			set { SetValue(MaxDateProperty, value); }
		}

		public static readonly DependencyProperty MinDateProperty = DependencyProperty.Register(
			"MinDate", typeof(DateTimeOffset), typeof(CalendarDatePicker), new PropertyMetadata(default(DateTimeOffset)));

		public DateTimeOffset MinDate
		{
			get { return (DateTimeOffset)GetValue(MinDateProperty); }
			set { SetValue(MinDateProperty, value); }
		}

		public static readonly DependencyProperty PlaceholderTextProperty = DependencyProperty.Register(
			"PlaceholderText", typeof(string), typeof(CalendarDatePicker), new PropertyMetadata(default(string)));

		public string PlaceholderText
		{
			get { return (string)GetValue(PlaceholderTextProperty); }
			set { SetValue(PlaceholderTextProperty, value); }
		}
	}
}
