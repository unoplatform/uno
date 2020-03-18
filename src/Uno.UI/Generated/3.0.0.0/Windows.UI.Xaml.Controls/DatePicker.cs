#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class DatePicker : global::Windows.UI.Xaml.Controls.Control
	{
		// Skipping already declared property YearVisible
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string YearFormat
		{
			get
			{
				return (string)this.GetValue(YearFormatProperty);
			}
			set
			{
				this.SetValue(YearFormatProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.Orientation Orientation
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.Orientation)this.GetValue(OrientationProperty);
			}
			set
			{
				this.SetValue(OrientationProperty, value);
			}
		}
		#endif
		// Skipping already declared property MonthVisible
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string MonthFormat
		{
			get
			{
				return (string)this.GetValue(MonthFormatProperty);
			}
			set
			{
				this.SetValue(MonthFormatProperty, value);
			}
		}
		#endif
		// Skipping already declared property MinYear
		// Skipping already declared property MaxYear
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.DataTemplate HeaderTemplate
		{
			get
			{
				return (global::Windows.UI.Xaml.DataTemplate)this.GetValue(HeaderTemplateProperty);
			}
			set
			{
				this.SetValue(HeaderTemplateProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  object Header
		{
			get
			{
				return (object)this.GetValue(HeaderProperty);
			}
			set
			{
				this.SetValue(HeaderProperty, value);
			}
		}
		#endif
		// Skipping already declared property DayVisible
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string DayFormat
		{
			get
			{
				return (string)this.GetValue(DayFormatProperty);
			}
			set
			{
				this.SetValue(DayFormatProperty, value);
			}
		}
		#endif
		// Skipping already declared property Date
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string CalendarIdentifier
		{
			get
			{
				return (string)this.GetValue(CalendarIdentifierProperty);
			}
			set
			{
				this.SetValue(CalendarIdentifierProperty, value);
			}
		}
		#endif
		// Skipping already declared property SelectedDate
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty CalendarIdentifierProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"CalendarIdentifier", typeof(string), 
			typeof(global::Windows.UI.Xaml.Controls.DatePicker), 
			new FrameworkPropertyMetadata(default(string)));
		#endif
		// Skipping already declared property DateProperty
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty DayFormatProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"DayFormat", typeof(string), 
			typeof(global::Windows.UI.Xaml.Controls.DatePicker), 
			new FrameworkPropertyMetadata(default(string)));
		#endif
		// Skipping already declared property DayVisibleProperty
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty HeaderProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Header", typeof(object), 
			typeof(global::Windows.UI.Xaml.Controls.DatePicker), 
			new FrameworkPropertyMetadata(default(object)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty HeaderTemplateProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"HeaderTemplate", typeof(global::Windows.UI.Xaml.DataTemplate), 
			typeof(global::Windows.UI.Xaml.Controls.DatePicker), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.DataTemplate)));
		#endif
		// Skipping already declared property MaxYearProperty
		// Skipping already declared property MinYearProperty
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty MonthFormatProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"MonthFormat", typeof(string), 
			typeof(global::Windows.UI.Xaml.Controls.DatePicker), 
			new FrameworkPropertyMetadata(default(string)));
		#endif
		// Skipping already declared property MonthVisibleProperty
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty OrientationProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Orientation", typeof(global::Windows.UI.Xaml.Controls.Orientation), 
			typeof(global::Windows.UI.Xaml.Controls.DatePicker), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.Orientation)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty YearFormatProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"YearFormat", typeof(string), 
			typeof(global::Windows.UI.Xaml.Controls.DatePicker), 
			new FrameworkPropertyMetadata(default(string)));
#endif
		// Skipping already declared property YearVisibleProperty
		// Skipping already declared property SelectedDateProperty
		// Skipping already declared method Windows.UI.Xaml.Controls.DatePicker.DatePicker()
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.DatePicker()
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.Header.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.Header.set
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.HeaderTemplate.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.HeaderTemplate.set
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.CalendarIdentifier.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.CalendarIdentifier.set
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.Date.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.Date.set
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.DayVisible.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.DayVisible.set
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.MonthVisible.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.MonthVisible.set
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.YearVisible.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.YearVisible.set
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.DayFormat.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.DayFormat.set
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.MonthFormat.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.MonthFormat.set
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.YearFormat.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.YearFormat.set
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.MinYear.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.MinYear.set
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.MaxYear.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.MaxYear.set
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.Orientation.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.Orientation.set
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.DateChanged.add
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.DateChanged.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.LightDismissOverlayMode.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.LightDismissOverlayMode.set
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.SelectedDate.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.SelectedDate.set
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.SelectedDateChanged.add
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.SelectedDateChanged.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.SelectedDateProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.LightDismissOverlayModeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.HeaderProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.HeaderTemplateProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.CalendarIdentifierProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.DateProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.DayVisibleProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.MonthVisibleProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.YearVisibleProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.DayFormatProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.MonthFormatProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.YearFormatProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.MinYearProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.MaxYearProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePicker.OrientationProperty.get
		// Skipping already declared event Windows.UI.Xaml.Controls.DatePicker.DateChanged
		// Skipping already declared event Windows.UI.Xaml.Controls.DatePicker.SelectedDateChanged
	}
}
