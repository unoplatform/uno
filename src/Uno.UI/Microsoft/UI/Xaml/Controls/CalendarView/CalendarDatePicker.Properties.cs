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
			get => (string)GetValue(CalendarIdentifierProperty);
			set => SetValue(CalendarIdentifierProperty, value);
		}

		public static readonly DependencyProperty CalendarViewStyleProperty = DependencyProperty.Register(
			"CalendarViewStyle", typeof(Style), typeof(CalendarDatePicker), new PropertyMetadata(default(Style)));

		public Style CalendarViewStyle
		{
			get => (Style)GetValue(CalendarViewStyleProperty);
			set => SetValue(CalendarViewStyleProperty, value);
		}

		public static readonly DependencyProperty DateProperty = DependencyProperty.Register(
			"Date", typeof(DateTimeOffset?), typeof(CalendarDatePicker), new PropertyMetadata(default(DateTimeOffset)));

		public DateTimeOffset? Date
		{
			get => (DateTimeOffset?)GetValue(DateProperty);
			set => SetValue(DateProperty, value);
		}

		public static readonly DependencyProperty DateFormatProperty = DependencyProperty.Register(
			"DateFormat", typeof(string), typeof(CalendarDatePicker), new PropertyMetadata(default(string)));

		public string DateFormat
		{
			get => (string)GetValue(DateFormatProperty);
			set => SetValue(DateFormatProperty, value);
		}

		public static readonly DependencyProperty DayOfWeekFormatProperty = DependencyProperty.Register(
			"DayOfWeekFormat", typeof(string), typeof(CalendarDatePicker), new PropertyMetadata(default(string)));

		public string DayOfWeekFormat
		{
			get => (string)GetValue(DayOfWeekFormatProperty);
			set => SetValue(DayOfWeekFormatProperty, value);
		}

		public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
			"Description", typeof(string), typeof(CalendarDatePicker), new PropertyMetadata(default(string)));

		public string Description
		{
			get => (string)GetValue(DescriptionProperty);
			set => SetValue(DescriptionProperty, value);
		}

		public static readonly DependencyProperty DisplayModeProperty = DependencyProperty.Register(
			"DisplayMode", typeof(CalendarViewDisplayMode), typeof(CalendarDatePicker), new PropertyMetadata(default(CalendarViewDisplayMode)));

		public CalendarViewDisplayMode DisplayMode
		{
			get => (CalendarViewDisplayMode)GetValue(DisplayModeProperty);
			set => SetValue(DisplayModeProperty, value);
		}

		public static readonly DependencyProperty FirstDayOfWeekProperty = DependencyProperty.Register(
			"FirstDayOfWeek", typeof(DayOfWeek), typeof(CalendarDatePicker), new PropertyMetadata(default(DayOfWeek)));

		public DayOfWeek FirstDayOfWeek
		{
			get => (DayOfWeek)GetValue(FirstDayOfWeekProperty);
			set => SetValue(FirstDayOfWeekProperty, value);
		}

		public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
			"Header", typeof(object), typeof(CalendarDatePicker), new PropertyMetadata(default(object)));

		public object Header
		{
			get => (object)GetValue(HeaderProperty);
			set => SetValue(HeaderProperty, value);
		}

		public static readonly DependencyProperty HeaderTemplateProperty = DependencyProperty.Register(
			"HeaderTemplate", typeof(DataTemplate), typeof(CalendarDatePicker), new PropertyMetadata(default(DataTemplate)));

		public DataTemplate HeaderTemplate
		{
			get => (DataTemplate)GetValue(HeaderTemplateProperty);
			set => SetValue(HeaderTemplateProperty, value);
		}

		public static readonly DependencyProperty IsCalendarOpenProperty = DependencyProperty.Register(
			"IsCalendarOpen", typeof(bool), typeof(CalendarDatePicker), new PropertyMetadata(default(bool)));

		public bool IsCalendarOpen
		{
			get => (bool)GetValue(IsCalendarOpenProperty);
			set => SetValue(IsCalendarOpenProperty, value);
		}

		public static readonly DependencyProperty IsGroupLabelVisibleProperty = DependencyProperty.Register(
			"IsGroupLabelVisible", typeof(bool), typeof(CalendarDatePicker), new PropertyMetadata(default(bool)));

		public bool IsGroupLabelVisible
		{
			get => (bool)GetValue(IsGroupLabelVisibleProperty);
			set => SetValue(IsGroupLabelVisibleProperty, value);
		}

		public static readonly DependencyProperty IsOutOfScopeEnabledProperty = DependencyProperty.Register(
			"IsOutOfScopeEnabled", typeof(bool), typeof(CalendarDatePicker), new PropertyMetadata(default(bool)));

		public bool IsOutOfScopeEnabled
		{
			get => (bool)GetValue(IsOutOfScopeEnabledProperty);
			set => SetValue(IsOutOfScopeEnabledProperty, value);
		}

		public static readonly DependencyProperty IsTodayHighlightedProperty = DependencyProperty.Register(
			"IsTodayHighlighted", typeof(bool), typeof(CalendarDatePicker), new PropertyMetadata(default(bool)));

		public bool IsTodayHighlighted
		{
			get => (bool)GetValue(IsTodayHighlightedProperty);
			set => SetValue(IsTodayHighlightedProperty, value);
		}

		public static readonly DependencyProperty LightDismissOverlayModeProperty = DependencyProperty.Register(
			"LightDismissOverlayMode", typeof(LightDismissOverlayMode), typeof(CalendarDatePicker), new PropertyMetadata(default(LightDismissOverlayMode)));

		public LightDismissOverlayMode LightDismissOverlayMode
		{
			get => (LightDismissOverlayMode)GetValue(LightDismissOverlayModeProperty);
			set => SetValue(LightDismissOverlayModeProperty, value);
		}

		public static readonly DependencyProperty MaxDateProperty = DependencyProperty.Register(
			"MaxDate", typeof(DateTimeOffset), typeof(CalendarDatePicker), new PropertyMetadata(default(DateTimeOffset)));

		public DateTimeOffset MaxDate
		{
			get => (DateTimeOffset)GetValue(MaxDateProperty);
			set => SetValue(MaxDateProperty, value);
		}

		public static readonly DependencyProperty MinDateProperty = DependencyProperty.Register(
			"MinDate", typeof(DateTimeOffset), typeof(CalendarDatePicker), new PropertyMetadata(default(DateTimeOffset)));

		public DateTimeOffset MinDate
		{
			get => (DateTimeOffset)GetValue(MinDateProperty);
			set => SetValue(MinDateProperty, value);
		}

		public static readonly DependencyProperty PlaceholderTextProperty = DependencyProperty.Register(
			"PlaceholderText", typeof(string), typeof(CalendarDatePicker), new PropertyMetadata(default(string)));

		public string PlaceholderText
		{
			get => (string)GetValue(PlaceholderTextProperty);
			set => SetValue(PlaceholderTextProperty, value);
		}
	}
}
