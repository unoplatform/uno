using System;
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls
{
	partial class DatePickerFlyout
	{
		protected TypedEventHandler<DatePickerFlyout, DatePickedEventArgs> _datePicked;

		public event TypedEventHandler<DatePickerFlyout, DatePickedEventArgs> DatePicked
		{
			add => _datePicked += value;
			remove => _datePicked -= value;
		}

		public DateTimeOffset Date
		{
			get => (DateTimeOffset)GetValue(DateProperty);
			set => SetValue(DateProperty, value);
		}

		public static DependencyProperty DateProperty { get; } =
			DependencyProperty.Register(
				"Date",
				typeof(DateTimeOffset),
				typeof(DatePickerFlyout),
				new FrameworkPropertyMetadata(
					defaultValue: GetDefaultDate(),
					options: FrameworkPropertyMetadataOptions.None
				)
			);

		public string DayFormat
		{
			get => (string)GetValue(DayFormatProperty);
			set => SetValue(DayFormatProperty, value);
		}

		public static DependencyProperty DayFormatProperty { get; } =
			DependencyProperty.Register(
				nameof(DayFormat), typeof(string),
				typeof(DatePickerFlyout),
				new FrameworkPropertyMetadata(
					defaultValue: GetDefaultDayFormat(),
					options: FrameworkPropertyMetadataOptions.None
				)
			);

		public string MonthFormat
		{
			get => (string)GetValue(MonthFormatProperty);
			set => SetValue(MonthFormatProperty, value);
		}

		public static DependencyProperty MonthFormatProperty { get; } =
			DependencyProperty.Register(
				nameof(MonthFormat), typeof(string),
				typeof(DatePickerFlyout),
				new FrameworkPropertyMetadata(
					defaultValue: GetDefaultMonthFormat(),
					options: FrameworkPropertyMetadataOptions.None
				)
			);

		public string YearFormat
		{
			get => (string)GetValue(YearFormatProperty);
			set => SetValue(YearFormatProperty, value);
		}

		public static DependencyProperty YearFormatProperty { get; } =
			DependencyProperty.Register(
				nameof(YearFormat), typeof(string),
				typeof(DatePickerFlyout),
				new FrameworkPropertyMetadata(
					defaultValue: GetDefaultYearFormat(),
					options: FrameworkPropertyMetadataOptions.None
				)
			);

		public string CalendarIdentifier
		{
			get => (string)GetValue(CalendarIdentifierProperty);
			set => SetValue(CalendarIdentifierProperty, value);
		}

		public static DependencyProperty CalendarIdentifierProperty { get; } =
			DependencyProperty.Register(
				nameof(CalendarIdentifier), typeof(string),
				typeof(DatePickerFlyout),
				new FrameworkPropertyMetadata(
					defaultValue: GetDefaultCalendarIdentifier(),
					options: FrameworkPropertyMetadataOptions.None
				)
			);

		public DateTimeOffset MinYear
		{
			get => (DateTimeOffset)GetValue(MinYearProperty);
			set => SetValue(MinYearProperty, value);
		}

		public static DependencyProperty MinYearProperty { get; } =
			DependencyProperty.Register(
				"MinYear",
				typeof(DateTimeOffset),
				typeof(DatePickerFlyout),
				new FrameworkPropertyMetadata(
					defaultValue: GetDefaultMinYear(),
					options: FrameworkPropertyMetadataOptions.None
				)
			);

		public DateTimeOffset MaxYear
		{
			get => (DateTimeOffset)GetValue(MaxYearProperty);
			set => SetValue(MaxYearProperty, value);
		}

		public static DependencyProperty MaxYearProperty { get; } =
			DependencyProperty.Register(
				"MaxYear",
				typeof(DateTimeOffset),
				typeof(DatePickerFlyout),
				new FrameworkPropertyMetadata(
					defaultValue: GetDefaultMaxYear(),
					options: FrameworkPropertyMetadataOptions.None
				)
			);

		public Style DatePickerFlyoutPresenterStyle
		{
			get { return (Style)this.GetValue(DatePickerFlyoutPresenterStyleProperty); }
			set { this.SetValue(DatePickerFlyoutPresenterStyleProperty, value); }
		}

		public static DependencyProperty DatePickerFlyoutPresenterStyleProperty { get; } =
			DependencyProperty.Register(
				nameof(DatePickerFlyoutPresenterStyle),
				typeof(Style),
				typeof(DatePickerFlyout),
				new FrameworkPropertyMetadata(
					default(Style),
					FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext
					));
	}
}
