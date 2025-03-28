using System;

using DateTime = Windows.Foundation.WindowsFoundationDateTime;
using Calendar = Windows.Globalization.Calendar;
using DayOfWeek = Windows.Globalization.DayOfWeek;

namespace Windows.UI.Xaml.Controls
{
	partial class CalendarDatePicker
	{
		public static DependencyProperty CalendarIdentifierProperty { get; } = DependencyProperty.Register(
			"CalendarIdentifier", typeof(string), typeof(CalendarDatePicker), new FrameworkPropertyMetadata("GregorianCalendar"));

		public string CalendarIdentifier
		{
			get => (string)GetValue(CalendarIdentifierProperty);
			set => SetValue(CalendarIdentifierProperty, value);
		}

		public static DependencyProperty CalendarViewStyleProperty { get; } = DependencyProperty.Register(
			"CalendarViewStyle", typeof(Style), typeof(CalendarDatePicker), new FrameworkPropertyMetadata(default(Style), FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext));

		public Style CalendarViewStyle
		{
			get => (Style)GetValue(CalendarViewStyleProperty);
			set => SetValue(CalendarViewStyleProperty, value);
		}

		public static DependencyProperty DateProperty { get; } = DependencyProperty.Register(
			"Date", typeof(DateTimeOffset?), typeof(CalendarDatePicker), new FrameworkPropertyMetadata(default(DateTimeOffset?)));

		public DateTimeOffset? Date
		{
			get => (DateTimeOffset?)GetValue(DateProperty);
			set => SetValue(DateProperty, value);
		}

		public static DependencyProperty DateFormatProperty { get; } = DependencyProperty.Register(
			"DateFormat", typeof(string), typeof(CalendarDatePicker), new FrameworkPropertyMetadata(default(string)));

		public string DateFormat
		{
			get => (string)GetValue(DateFormatProperty) ?? "";
			set => SetValue(DateFormatProperty, value);
		}

		public static DependencyProperty DayOfWeekFormatProperty { get; } = DependencyProperty.Register(
			"DayOfWeekFormat", typeof(string), typeof(CalendarDatePicker), new FrameworkPropertyMetadata(default(string)));

		public string DayOfWeekFormat
		{
			get => (string)GetValue(DayOfWeekFormatProperty) ?? "";
			set => SetValue(DayOfWeekFormatProperty, value);
		}

		public static DependencyProperty DescriptionProperty { get; } = DependencyProperty.Register(
			"Description", typeof(object), typeof(CalendarDatePicker), new FrameworkPropertyMetadata(default(object), propertyChangedCallback: (s, e) => (s as CalendarDatePicker)?.UpdateDescriptionVisibility(false)));

#if __IOS__ || __MACOS__
		public new // .Description already exists on NSObject (both macOS & iOS)
#else
		public
#endif
			object Description
		{
			get => GetValue(DescriptionProperty);
			set => SetValue(DescriptionProperty, value);
		}

		public static DependencyProperty DisplayModeProperty { get; } = DependencyProperty.Register(
			"DisplayMode", typeof(CalendarViewDisplayMode), typeof(CalendarDatePicker), new FrameworkPropertyMetadata(default(CalendarViewDisplayMode)));

		public CalendarViewDisplayMode DisplayMode
		{
			get => (CalendarViewDisplayMode)GetValue(DisplayModeProperty);
			set => SetValue(DisplayModeProperty, value);
		}

		public static DependencyProperty FirstDayOfWeekProperty { get; } = DependencyProperty.Register(
			"FirstDayOfWeek", typeof(DayOfWeek), typeof(CalendarDatePicker), new FrameworkPropertyMetadata(default(DayOfWeek)));

		public DayOfWeek FirstDayOfWeek
		{
			get => (DayOfWeek)GetValue(FirstDayOfWeekProperty);
			set => SetValue(FirstDayOfWeekProperty, value);
		}

		public static DependencyProperty HeaderProperty { get; } = DependencyProperty.Register(
			"Header", typeof(object), typeof(CalendarDatePicker), new FrameworkPropertyMetadata(default(object)));

		public object Header
		{
			get => (object)GetValue(HeaderProperty);
			set => SetValue(HeaderProperty, value);
		}

		public static DependencyProperty HeaderTemplateProperty { get; } = DependencyProperty.Register(
			"HeaderTemplate", typeof(DataTemplate), typeof(CalendarDatePicker), new FrameworkPropertyMetadata(default(DataTemplate), FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext));

		public DataTemplate HeaderTemplate
		{
			get => (DataTemplate)GetValue(HeaderTemplateProperty);
			set => SetValue(HeaderTemplateProperty, value);
		}

		public static DependencyProperty IsCalendarOpenProperty { get; } = DependencyProperty.Register(
			"IsCalendarOpen", typeof(bool), typeof(CalendarDatePicker), new FrameworkPropertyMetadata(default(bool)));

		public bool IsCalendarOpen
		{
			get => (bool)GetValue(IsCalendarOpenProperty);
			set => SetValue(IsCalendarOpenProperty, value);
		}

		public static DependencyProperty IsGroupLabelVisibleProperty { get; } = DependencyProperty.Register(
			"IsGroupLabelVisible", typeof(bool), typeof(CalendarDatePicker), new FrameworkPropertyMetadata(default(bool)));

		public bool IsGroupLabelVisible
		{
			get => (bool)GetValue(IsGroupLabelVisibleProperty);
			set => SetValue(IsGroupLabelVisibleProperty, value);
		}

		public static DependencyProperty IsOutOfScopeEnabledProperty { get; } = DependencyProperty.Register(
			"IsOutOfScopeEnabled", typeof(bool), typeof(CalendarDatePicker), new FrameworkPropertyMetadata(defaultValue: true));

		public bool IsOutOfScopeEnabled
		{
			get => (bool)GetValue(IsOutOfScopeEnabledProperty);
			set => SetValue(IsOutOfScopeEnabledProperty, value);
		}

		public static DependencyProperty IsTodayHighlightedProperty { get; } = DependencyProperty.Register(
			"IsTodayHighlighted", typeof(bool), typeof(CalendarDatePicker), new FrameworkPropertyMetadata(defaultValue: true));

		public bool IsTodayHighlighted
		{
			get => (bool)GetValue(IsTodayHighlightedProperty);
			set => SetValue(IsTodayHighlightedProperty, value);
		}

		public static DependencyProperty LightDismissOverlayModeProperty { get; } = DependencyProperty.Register(
			"LightDismissOverlayMode", typeof(LightDismissOverlayMode), typeof(CalendarDatePicker), new FrameworkPropertyMetadata(default(LightDismissOverlayMode)));

		public LightDismissOverlayMode LightDismissOverlayMode
		{
			get => (LightDismissOverlayMode)GetValue(LightDismissOverlayModeProperty);
			set => SetValue(LightDismissOverlayModeProperty, value);
		}

		public static DependencyProperty MaxDateProperty { get; } = DependencyProperty.Register(
			"MaxDate", typeof(DateTimeOffset), typeof(CalendarDatePicker), new FrameworkPropertyMetadata(default(DateTimeOffset)));

		public DateTimeOffset MaxDate
		{
			get => (DateTimeOffset)GetValue(MaxDateProperty);
			set => SetValue(MaxDateProperty, value);
		}

		public static DependencyProperty MinDateProperty { get; } = DependencyProperty.Register(
			"MinDate", typeof(DateTimeOffset), typeof(CalendarDatePicker), new FrameworkPropertyMetadata(default(DateTimeOffset)));

		public DateTimeOffset MinDate
		{
			get => (DateTimeOffset)GetValue(MinDateProperty);
			set => SetValue(MinDateProperty, value);
		}

		public static DependencyProperty PlaceholderTextProperty { get; } = DependencyProperty.Register(
			"PlaceholderText", typeof(string), typeof(CalendarDatePicker), new FrameworkPropertyMetadata("select a date")); // TODO: Localize?

		public string PlaceholderText
		{
			get => (string)GetValue(PlaceholderTextProperty);
			set => SetValue(PlaceholderTextProperty, value);
		}

		private static Calendar _gregorianCalendar;

		private const int DEFAULT_MIN_MAX_DATE_YEAR_OFFSET = 100;

		internal override bool GetDefaultValue2(DependencyProperty property, out object value)
		{
			Calendar GetOrCreateGregorianCalendar()
			{
				if (_gregorianCalendar is null)
				{
					var tempCalendar = new Calendar();
					_gregorianCalendar = new Calendar(
							tempCalendar.Languages,
							"GregorianCalendar",
							tempCalendar.GetClock());
				}

				return _gregorianCalendar;
			}

			DateTimeOffset ClampDate(
				DateTime date,
				DateTime minDate,
				DateTime maxDate)
			{
				return date.UniversalTime < minDate.UniversalTime ? minDate : date.UniversalTime > maxDate.UniversalTime ? maxDate : date;
			}

			if (property == CalendarDatePicker.MinDateProperty)
			{
				var calendar = GetOrCreateGregorianCalendar();
				calendar.SetToMin();
				var minCalendarDate = calendar.GetDateTime();
				calendar.SetToMax();
				var maxCalendarDate = calendar.GetDateTime();

				//Default value is today's date minus 100 Gregorian years.
				calendar.SetToday();
				calendar.AddYears(-DEFAULT_MIN_MAX_DATE_YEAR_OFFSET);
				calendar.Month = calendar.FirstMonthInThisYear;
				calendar.Day = calendar.FirstDayInThisMonth;
				var minDate = calendar.GetDateTime();

				value = ClampDate(minDate, minCalendarDate, maxCalendarDate);
				return true;
			}

			if (property == CalendarDatePicker.MaxDateProperty)
			{
				var calendar = GetOrCreateGregorianCalendar();
				calendar.SetToMin();
				var minCalendarDate = calendar.GetDateTime();
				calendar.SetToMax();
				var maxCalendarDate = calendar.GetDateTime();

				//Default value is today's date plus 100 Gregorian years.
				calendar.SetToday();
				calendar.AddYears(DEFAULT_MIN_MAX_DATE_YEAR_OFFSET);
				calendar.Month = calendar.LastMonthInThisYear;
				calendar.Day = calendar.LastDayInThisMonth;
				var maxDate = calendar.GetDateTime();

				value = ClampDate(maxDate, minCalendarDate, maxCalendarDate);
				return true;
			}

			return base.GetDefaultValue2(property, out value);
		}
	}
}
