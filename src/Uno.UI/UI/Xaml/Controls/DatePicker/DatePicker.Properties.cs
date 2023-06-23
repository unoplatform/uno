using System;

namespace Windows.UI.Xaml.Controls
{
	public partial class DatePicker : Control
	{
		#region DateProperty

		public DateTimeOffset Date
		{
			get => (DateTimeOffset)GetValue(DateProperty);
			set => SetValue(DateProperty, value);
		}

		//#18331 If the Date property of DatePickerFlyout is two way binded, the ViewModel receives the control's default value while the ViewModel sends its default value which desynchronizes the values
		//Set initial value of DatePicker to DateTimeOffset.MinValue to avoid 2 way binding issue where the DatePicker reset Date(DateTimeOffset.MinValue) after the initial binding value.
		//We assume that this is the view model who will set the initial value just the time to fix #18331
		public static DependencyProperty DateProperty { get; } =
			DependencyProperty.Register(
				name: "Date",
				propertyType: typeof(DateTimeOffset),
				ownerType: typeof(DatePicker),
				typeMetadata: new FrameworkPropertyMetadata(
					defaultValue: DateTimeOffset.MinValue,
					propertyChangedCallback: (s, e) => ((DatePicker)s).OnDatePropertyChanged((DateTimeOffset)e.NewValue, (DateTimeOffset)e.OldValue)));

		private void OnDatePropertyChanged(DateTimeOffset newValue, DateTimeOffset oldValue)
		{
			OnDateChangedPartial();
		}

		partial void OnDateChangedPartial();
		#endregion

		#region DayVisibleProperty
		public bool DayVisible
		{
			get => (bool)GetValue(DayVisibleProperty);
			set => SetValue(DayVisibleProperty, value);
		}

		public static DependencyProperty DayVisibleProperty { get; } =
			DependencyProperty.Register(
				name: "DayVisible",
				propertyType: typeof(bool),
				ownerType: typeof(DatePicker),
				typeMetadata: new FrameworkPropertyMetadata(
					defaultValue: true,
					propertyChangedCallback: (s, e) => ((DatePicker)s).OnDayVisibleChangedPartial()));

		partial void OnDayVisibleChangedPartial();
		#endregion

		#region MonthVisibleProperty
		public bool MonthVisible
		{
			get => (bool)GetValue(MonthVisibleProperty);
			set => SetValue(MonthVisibleProperty, value);
		}

		public static DependencyProperty MonthVisibleProperty { get; } =
			DependencyProperty.Register(
				name: "MonthVisible",
				propertyType: typeof(bool),
				ownerType: typeof(DatePicker),
				typeMetadata: new FrameworkPropertyMetadata(
					defaultValue: true,
					propertyChangedCallback: (s, e) => ((DatePicker)s).OnMonthVisibleChangedPartial()));

		partial void OnMonthVisibleChangedPartial();
		#endregion

		#region YearVisibleProperty
		public bool YearVisible
		{
			get => (bool)GetValue(YearVisibleProperty);
			set => SetValue(YearVisibleProperty, value);
		}

		public static DependencyProperty YearVisibleProperty { get; } =
			DependencyProperty.Register(
				name: "YearVisible",
				propertyType: typeof(bool),
				ownerType: typeof(DatePicker),
				typeMetadata: new FrameworkPropertyMetadata(
					defaultValue: true,
					propertyChangedCallback: (s, e) => ((DatePicker)s).OnYearVisibleChangedPartial()));

		partial void OnYearVisibleChangedPartial();
		#endregion

		#region MaxYearProperty
		public DateTimeOffset MaxYear
		{
			get => (DateTimeOffset)GetValue(MaxYearProperty);
			set => SetValue(MaxYearProperty, value);
		}

		public static DependencyProperty MaxYearProperty { get; } =
			DependencyProperty.Register(
				name: "MaxYear",
				propertyType: typeof(DateTimeOffset),
				ownerType: typeof(DatePicker),
				typeMetadata: new FrameworkPropertyMetadata(
					defaultValue: DateTimeOffset.MaxValue,
					propertyChangedCallback: (s, e) => ((DatePicker)s).OnMaxYearChangedPartial()));

		partial void OnMaxYearChangedPartial();
		#endregion

		#region MinYearProperty
		public DateTimeOffset MinYear
		{
			get => (DateTimeOffset)GetValue(MinYearProperty);
			set => SetValue(MinYearProperty, value);
		}

		public static DependencyProperty MinYearProperty { get; } =
			DependencyProperty.Register(
				name: "MinYear",
				propertyType: typeof(DateTimeOffset),
				ownerType: typeof(DatePicker),
				typeMetadata: new FrameworkPropertyMetadata(
					defaultValue: DateTimeOffset.MinValue,
					propertyChangedCallback: (s, e) => ((DatePicker)s).OnMinYearChangedPartial()));

		partial void OnMinYearChangedPartial();
		#endregion

		public string YearFormat
		{
			get => (string)GetValue(YearFormatProperty);
			set => SetValue(YearFormatProperty, value);
		}

		public static DependencyProperty YearFormatProperty { get; } =
		DependencyProperty.Register(
			name: "YearFormat",
			propertyType: typeof(string),
			ownerType: typeof(DatePicker),
			typeMetadata: new FrameworkPropertyMetadata("year.full"));

		public Orientation Orientation
		{
			get => (Orientation)GetValue(OrientationProperty);
			set => SetValue(OrientationProperty, value);
		}

		public static DependencyProperty OrientationProperty { get; } =
		DependencyProperty.Register(
			name: "Orientation",
			propertyType: typeof(Orientation),
			ownerType: typeof(DatePicker),
			typeMetadata: new FrameworkPropertyMetadata(default(Orientation)));

		public string MonthFormat
		{
			get => (string)GetValue(MonthFormatProperty);
			set => SetValue(MonthFormatProperty, value);
		}

		public static DependencyProperty MonthFormatProperty { get; } =
		DependencyProperty.Register(
			name: "MonthFormat",
			propertyType: typeof(string),
			ownerType: typeof(DatePicker),
			typeMetadata: new FrameworkPropertyMetadata("{month.full}"));

		public DataTemplate HeaderTemplate
		{
			get => (DataTemplate)GetValue(HeaderTemplateProperty);
			set => SetValue(HeaderTemplateProperty, value);
		}

		public static DependencyProperty HeaderTemplateProperty { get; } =
		DependencyProperty.Register(
			name: "HeaderTemplate",
			propertyType: typeof(DataTemplate),
			ownerType: typeof(DatePicker),
			typeMetadata: new FrameworkPropertyMetadata(default(DataTemplate), FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext));

		public object Header
		{
			get => (object)GetValue(HeaderProperty);
			set => SetValue(HeaderProperty, value);
		}

		public static DependencyProperty HeaderProperty { get; } =
		DependencyProperty.Register(
			name: "Header",
			propertyType: typeof(object),
			ownerType: typeof(DatePicker),
			typeMetadata: new FrameworkPropertyMetadata(default(object)));

		public string DayFormat
		{
			get => (string)GetValue(DayFormatProperty);
			set => SetValue(DayFormatProperty, value);
		}

		public static DependencyProperty DayFormatProperty { get; } =
		DependencyProperty.Register(
			name: "DayFormat",
			propertyType: typeof(string),
			ownerType: typeof(DatePicker),
			typeMetadata: new FrameworkPropertyMetadata("day"));

		public string CalendarIdentifier
		{
			get => (string)GetValue(CalendarIdentifierProperty);
			set => SetValue(CalendarIdentifierProperty, value);
		}
		public static DependencyProperty CalendarIdentifierProperty { get; } =
		DependencyProperty.Register(
			name: "CalendarIdentifier",
			propertyType: typeof(string),
			ownerType: typeof(DatePicker),
			typeMetadata: new FrameworkPropertyMetadata("GregorianCalendar"));

		public DateTimeOffset? SelectedDate
		{
			get => (DateTimeOffset?)GetValue(SelectedDateProperty);
			set => SetValue(SelectedDateProperty, value);
		}

		public static DependencyProperty SelectedDateProperty { get; } =
		DependencyProperty.Register(
			name: "SelectedDate",
			propertyType: typeof(DateTimeOffset?),
			ownerType: typeof(DatePicker),
			typeMetadata: new FrameworkPropertyMetadata(default(DateTimeOffset?)));

		public LightDismissOverlayMode LightDismissOverlayMode
		{
			get => (LightDismissOverlayMode)GetValue(LightDismissOverlayModeProperty);
			set => SetValue(LightDismissOverlayModeProperty, value);
		}

		public static DependencyProperty LightDismissOverlayModeProperty { get; } =
		DependencyProperty.Register(
			name: "LightDismissOverlayMode",
			propertyType: typeof(LightDismissOverlayMode),
			ownerType: typeof(DatePicker),
			typeMetadata: new FrameworkPropertyMetadata(default(LightDismissOverlayMode)));
	}
}
