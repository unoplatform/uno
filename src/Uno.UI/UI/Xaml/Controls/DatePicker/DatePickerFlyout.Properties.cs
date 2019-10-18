using Windows.Foundation;

namespace Windows.UI.Xaml.Controls
{
	partial class DatePickerFlyout
	{

		
		public static DependencyProperty DayFormatProperty { get; } =
			DependencyProperty.Register(
				nameof(DayFormat), typeof(string),
				typeof(DatePickerFlyout),
				new FrameworkPropertyMetadata("day"));


		
		public static DependencyProperty MonthFormatProperty { get; } =
			DependencyProperty.Register(
				nameof(MonthFormat), typeof(string),
				typeof(DatePickerFlyout),
				new FrameworkPropertyMetadata("{month.full}"));


		
		public static DependencyProperty YearFormatProperty { get; } =
			DependencyProperty.Register(
				nameof(YearFormat), typeof(string),
				typeof(DatePickerFlyout),
				new FrameworkPropertyMetadata("year.full"));


		
		public string CalendarIdentifier
		{
			get => (string)GetValue(CalendarIdentifierProperty);
			set => SetValue(CalendarIdentifierProperty, value);
		}


		
		public string YearFormat
		{
			get => (string)GetValue(YearFormatProperty);
			set => SetValue(YearFormatProperty, value);
		}


		
		public string MonthFormat
		{
			get => (string)GetValue(MonthFormatProperty);
			set => SetValue(MonthFormatProperty, value);
		}


		
		public string DayFormat
		{
			get => (string)GetValue(DayFormatProperty);
			set => SetValue(DayFormatProperty, value);
		}


		
		public static DependencyProperty CalendarIdentifierProperty { get; } =
		DependencyProperty.Register(
			nameof(CalendarIdentifier), typeof(string),
			typeof(DatePickerFlyout),
			new FrameworkPropertyMetadata(GetDefaultCalendarIdentifier()));

		public TypedEventHandler<DatePickerFlyout, DatePickedEventArgs> DatePicked;
	}
}
